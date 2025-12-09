using System;
using System.Collections.Generic;
using CookComputing.XmlRpc;

// Implementation class for JEMSServer, this is the class that actually does the work defined by the remoting interface

namespace BioNex.GemsRpc
{
    public class JEMSDataService : SystemMethodsBase, IGemsData
    {
        private Dictionary<string,string> _inventory { get; set; }
        private JemsServer.RefreshInventoryViewDelegate _refresh_view { get; set; }
        
        public JEMSDataService( ref Dictionary<string,string> inventory, JemsServer.RefreshInventoryViewDelegate refresh_view)
        {
            _inventory = inventory;
            _refresh_view = refresh_view;
        }

        public override object InitializeLifetimeService() { return null; } // The lease to this object shall never expire

        #region IJEMSData Members

        public void ReinventoryComplete(string hive_name, string cart_name, string[] barcodes)
        {
            // add only the barcodes that aren't already present in the list
            foreach (string x in barcodes)
                if (!_inventory.ContainsKey(x))
                    _inventory[x] = "Unknown Fate";
            if (_refresh_view != null)
                _refresh_view(hive_name);
        }

        public void Ping()
        {
            // doesn't need to do anything, we just need the function to be callable.
        }

        #endregion
    }
}
