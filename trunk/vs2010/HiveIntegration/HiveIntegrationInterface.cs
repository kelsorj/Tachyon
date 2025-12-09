using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.HiveIntegration
{
    public interface HiveIntegrationInterface
    {
        /// <summary>
        /// Initializes the Hive with the parameters specified within the XML string.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns>true on success, false on failure</returns>
        bool Initialize( string xml);
        /// <summary>
        /// Closes the connection to the Hive.
        /// </summary>
        /// <returns>true on success, false on failure</returns>
        bool Close();
        /// <summary>
        /// Unloads the plate with the specified barcode from static storage.
        /// </summary>
        /// <param name="expected_barcode">The barcode to unload</param>
        /// <param name="labware_name">The name of the labware to unload.  Allows the plate handling robot to know how to properly grip the plate.</param>
        /// <returns>true on success, false on failure</returns>
        bool UnloadPlate( string expected_barcode, string labware_name);
        /// <summary>
        /// Verifies that plate dropped off has the expected barcode, and then moves the
        /// plate into static storage.
        /// </summary>
        /// <param name="expected_barcode">The barcode of the plate that was dropped off</param>
        /// <param name="labware_name">The labware type of the plate that was dropped off.  Allows the plate handling robot to know how to properly grip the plate.</param>
        /// <returns>true on success, false on failure</returns>
        bool LoadPlate( string expected_barcode, string labware_name);
        /// <summary>
        /// Reports whether or not the specified barcode exists in the Hive.
        /// </summary>
        /// <param name="barcode">The barcode to look for</param>
        /// <param name="found">Whether or not the Hive has the specified barcode</param>
        /// <returns>true on success, false on failure</returns>
        bool HasBarcode( string barcode, out bool found);
        /// <summary>
        /// Returns the contents of the Hive's static storage and carts, if any are docked.
        /// </summary>
        /// <example>
        /// The following is an example of the XML that will be returned by the GetInventory method.  Note that this is still subject to change!
        /**
        \verbatim
        <HiveInventory>
        <!-- use the cart barcode instead of the dock name -->
        <!-- the following could be Dock #1 -->
          <Device name="Cart1BarcodeGoesHere">
            <Barcodes>
              <Barcode>A1234</Barcode>
              <Barcode>A2345</Barcode>
            </Barcodes>
          </Device>
          <!-- the following could be Dock #2 -->
          <Device name="Cart2BarcodeGoesHere">
            <Barcodes>
              <Barcode>B1234</Barcode>
            </Barcodes>
          </Device>
          <!-- "Static" refers to static storage -->
          <Device name="Static">
            <Barcodes>
              <Barcode>C1234</Barcode>
              <Barcode>C2345</Barcode>
              <Barcode>C3456</Barcode>
            </Barcodes>
          </Device>
        </HiveInventory> 
        \endverbatim
         */
        /// </example>
        /// <param name="xml">All plate locations are stored in an XML string</param>
        /// <returns>true on success, false on failure</returns>
        bool GetInventory( out string xml);
        /// <summary>
        /// Scans all storage locations (static storage and carts) for plates.
        /// </summary>
        /// <returns>true on success, false on failure</returns>
        bool ScanInventory();
        /// <summary>
        /// Retrieves the last error reported by the Hive hardware or any communication errors.  This method
        /// does not fail.
        /// </summary>
        /// <returns>The last error reported by the Hive</returns>
        string GetLastError();
        /// <summary>
        /// Moves the specified barcoded plate from its current location to the destination group.
        /// </summary>
        /// <param name="barcode">The plate to move within the Hive</param>
        /// <param name="destination_group">
        /// Group names are case-insensitive.
        /// "storage" and "" indicate that plate should be moved to static storage.  
        /// "trash" indicates that the plate should be dropped off at the trash location.
        /// Any other string will move the plate to the first available cart, and
        /// that cart will be reserved for future plates belonging to the same group.
        /// </param>
        /// <returns>true on success, false on failure</returns>
        bool MovePlate( string barcode, string labware_name, string destination_group);
        /// <summary>
        /// Returns information about the Hive's state of operation.
        /// </summary>
        /// <returns>true on success, false on failure</returns>
        bool GetStatus( out HiveStatus status);
        /// <summary>
        /// Moves the Hive stage to its external robotically-accessible teachpoint.
        /// </summary>
        /// <returns>true on success, false on failure</returns>
        bool PresentStage();
    }

    /// <example>
    /// It is very important to write code to the HiveIntegrationInterface, and not the concrete Hive class.  This will protect
    /// you from internal changes that will break your code if you directly use the Hive class.  The following is a code
    /// sample:
    /// <code>
    /// HiveIntegrationInterface hive = new Hive();
    /// // let's say that we want to connect to port 7777 on localhost
    /// string xml = BioNex.HiveIntegration.HiveXmlHelper.InitializeParamsToXml( "localhost", 7777);
    /// bool ret = _hive_client.Initialize( xml);
    /// // ...it is up to you to handle the return code in your application!
    /// </code>
    /// </example>
    public class Hive : HiveIntegrationInterface
    {
        private HiveClient _remote_client;
        private string _last_error;

        #region HiveIntegrationInterface Members

        public bool Initialize(string xml)
        {
            try {
                Close();
                HiveXmlHelper.InitializeParams init = HiveXmlHelper.XmlToInitializeParams( xml);
                // define the connection
                _remote_client = (HiveClient)CookComputing.XmlRpc.XmlRpcProxyGen.Create( typeof(HiveClient));
                _remote_client.Url = String.Format("http://{0}:{1}/hiverpc", init.IpAddress, init.Port);
                _remote_client.Timeout = 30000;
                // test the connection
                _remote_client.Ping();
                // home the device
                _remote_client.Initialize( xml);
                return true;
            } catch( Exception ex) {
                _last_error = ex.Message;
                return false;
            }
        }

        public bool Close()
        {
            try {
                _remote_client.Close();
                return true;
            } catch( Exception ex) {
                _last_error = ex.Message;
                return false;
            }
        }

        private void CheckRemoteClientConnection()
        {
            if( _remote_client == null)
                throw new Exception( "Not connected to remote Hive server.  Please call Initialize first with the appropriate connection parameters.");
        }

        public bool UnloadPlate(string expected_barcode, string labware_name)
        {
            try {
                CheckRemoteClientConnection();
                _last_error = "";
                _remote_client.UnloadPlate( expected_barcode, labware_name);
            } catch( Exception ex) {
                _last_error = ex.Message;
                return false;
            }
            return true;
        }

        public bool LoadPlate(string expected_barcode, string labware_name)
        {
            try {
                CheckRemoteClientConnection();
                _last_error = "";
                _remote_client.LoadPlate( expected_barcode, labware_name);
            } catch( Exception ex) {
                _last_error = ex.Message;
                return false;
            }
            return true;
        }

        public bool HasBarcode(string barcode, out bool found)
        {
            found = false;
            try {
                CheckRemoteClientConnection();
                _last_error = "";
                found = _remote_client.HasBarcode( barcode);
            } catch( Exception ex) {
                _last_error = ex.Message;
                return false;
            }
            return true;
        }

        public bool GetInventory(out string xml)
        {
            xml = "";
            try {
                CheckRemoteClientConnection();
                _last_error = "";
                xml = _remote_client.GetInventory();
            } catch( Exception ex) {
                _last_error = ex.Message;
                return false;
            }
            return true;
        }

        public bool ScanInventory()
        {
            try {
                CheckRemoteClientConnection();
                _last_error = "";
                _remote_client.ScanInventory();
            } catch( Exception ex) {
                _last_error = ex.Message;
                return false;
            }
            return true;
        }

        public string GetLastError()
        {
            return _last_error;
        }

        public bool MovePlate(string barcode, string labware_name, string destination_group)
        {
            try {
                CheckRemoteClientConnection();
                _last_error = "";
                _remote_client.MovePlate( barcode, labware_name, destination_group);
            } catch( Exception ex) {
                _last_error = ex.Message;
                return false;
            }
            return true;
        }

        public bool GetStatus( out HiveStatus status)
        {
            status = new HiveStatus();
            try {
                CheckRemoteClientConnection();
                _last_error = "";
                int status_code = _remote_client.GetStatus();
                // DKM 2012-05-14 TODO implement int-to-HiveStatus conversion
                return true;
            } catch( Exception ex) {
                _last_error = ex.Message;
                return false;
            }
        }

        public bool PresentStage()
        {
            try {
                CheckRemoteClientConnection();
                _last_error = "";
                _remote_client.PresentStage();
                // DKM 2012-05-14 TODO implement int-to-HiveStatus conversion
                return true;
            } catch( Exception ex) {
                _last_error = ex.Message;
                return false;
            }
        }

        #endregion
    }
}
