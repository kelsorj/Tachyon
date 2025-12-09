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
        private HiveServer.AddWorkSetDelegate _add_work_set;

        public HiveRemoteInterface(HiveServer.UpdatePlateFateDelegate update=null, HiveServer.EndBatchDelegate end_batch=null, HiveServer.AddWorkSetDelegate add_work_set=null)
        {
            _update = update;
            _end_batch = end_batch;
            _add_work_set = add_work_set;
        }

        public override object InitializeLifetimeService() { return null; } // The lease to this object shall never expire

        #region IHiveData Members

        public void AddWorkSet(string workset_xml)
        {
            if (_add_work_set != null)
                _add_work_set(workset_xml);
        }

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
