using System;
using System.Collections.Generic;
using System.Linq;
using CookComputing.XmlRpc;

// Implementation class for HiveServer, this is the class that actually does the work definde by the remoting interface

namespace BioNex.HiveRpc
{
    public class HiveRemoteInterface : SystemMethodsBase, IHiveData
    {
        private HiveServer.UpdatePlateFateDelegate _update;
        private HiveServer.EndBatchDelegate _end_batch;

        public HiveRemoteInterface(HiveServer.UpdatePlateFateDelegate update, HiveServer.EndBatchDelegate end_batch)
        {
            _update = update;
            _end_batch = end_batch;
        }

        public override object InitializeLifetimeService() { return null; } // The lease to this object shall never expire

        #region IHiveData Members

        public void SetPlateFate(string barcode, string new_fate)
        {
            if (_update != null)
                _update(barcode, new_fate);
        }

        public void EndBatch( string finished_fate)
        {
            if (_end_batch != null)
                _end_batch(finished_fate);
        }

        public void Ping()
        {
            // doesn't need to do anything, we just need the function to be callable.
        }
        #endregion
    }
}
