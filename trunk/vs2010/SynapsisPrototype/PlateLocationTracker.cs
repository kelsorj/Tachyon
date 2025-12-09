using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.DeviceInterfaces;
using System.ComponentModel.Composition;

namespace BioNex.SynapsisPrototype
{
    [Export(typeof(IPlateLocationTracker))]
    public class PlateLocationTracker : IPlateLocationTracker
    {
        private Dictionary<string, Tuple<DeviceInterface,string>> _locations { get; set; }

        public PlateLocationTracker()
        {
            _locations = new Dictionary<string,Tuple<DeviceInterface,string>>();
        }

        #region IPlateLocationTracker Members

        public void RecordPlateLocation( string barcode, DeviceInterface device, string location_name)
        {
            lock( this) {
                ClearPlateLocation( barcode);
                // set the new plate location
                _locations.Add( barcode, new Tuple<DeviceInterface,string>( device, location_name));
            }
        }

        public void ClearPlateLocation( string barcode)
        {
            _locations.Remove( barcode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="barcode"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">
        /// barcode not found in locator
        /// </exception>
        public Tuple<DeviceInterface, string> GetPlateLocation( string barcode)
        {
            return _locations[barcode];
        }

        public string GetPlateAtLocation(DeviceInterface device, string location_name)
        {
            return _locations.FirstOrDefault( x => (x.Value.Item1 == device && x.Value.Item2 == location_name)).Key;
        }

        #endregion
    }
}
