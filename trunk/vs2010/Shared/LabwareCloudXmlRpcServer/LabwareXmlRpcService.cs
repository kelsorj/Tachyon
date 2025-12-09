using System;
using System.Threading;
using BioNex.Shared.LabwareDatabase;
using BioNex.Shared.LibraryInterfaces;
using CookComputing.XmlRpc;

namespace BioNex.Shared.LabwareCloudXmlRpcServer
{
    public class LabwareXmlRpcService : SystemMethodsBase, ILabwareCloudXmlRpcService
    {
        readonly ILabwareDatabase _labwareDb;
        string _current_transaction_id;

        Thread _transactionThread;
        readonly AutoResetEvent _transactionEvent;
        const int DefaultTransactionPeriod = 30000; // 30 seconds in milliseconds
        int _transactionPeriod;

        public LabwareXmlRpcService(ILabwareDatabase db)
        {
            _labwareDb = db;
            _current_transaction_id = null;
            _transactionEvent = new AutoResetEvent(false);
        }
        public override object InitializeLifetimeService() { return null; } // The lease to this object shall never expire

        public XmlRpcLabware[] Sync(DateTime lastSyncTime)
        {
            // if we haven't been modified since last sync, don't return anything
            var lastModifiedTime = _labwareDb.GetLastModifiedTime();
            if( lastSyncTime > lastModifiedTime)
                return new XmlRpcLabware[0];

            var names = _labwareDb.GetLabwareNames();
            var xmlrpc_labware = new XmlRpcLabware[names.Count];
            for( int i=0; i<names.Count; ++i)
                xmlrpc_labware[i] = new XmlRpcLabware(_labwareDb.GetLabware(names[i]));
            return xmlrpc_labware;
        }

        public string BeginPublish()
        {
            if (_current_transaction_id != null)
                throw new LabwareXmlRpcServiceException("Cannot begin a new transaction since another transaction is pending");

            BeginTransaction();
            return _current_transaction_id;
        }

        public void EndPublish(string guid)
        {
            if (_current_transaction_id == null || guid != _current_transaction_id)
                throw new LabwareXmlRpcServiceException("Transaction id is invalid, either the transaction timed out or you've already closed this transaction");

            EndTransaction();
        }

        public void Publish(string guid, XmlRpcLabware[] labwares)
        {
            if (_current_transaction_id == null || guid != _current_transaction_id)
                throw new LabwareXmlRpcServiceException("Transaction id is invalid, either the transaction timed out or you've already closed this transaction");

            ResetTransaction();
            // race condition -- there's a slight chance that the transaction has ended already here, so be sure not to assume it hasn't
            // in other words, the following addlabware is still safe since the transaction has just timed-out within a millisecond
            // but another call to Publish might fail with a timed-out transaction

            foreach (var x in labwares)
                _labwareDb.AddLabware((Labware)x);
        }

        void BeginTransaction()
        {
            // generate a transaction id
            _current_transaction_id = Guid.NewGuid().ToString();

            // TODO -- LOCK LABWARE DB / UI

            // start timeout timer, close transaction if it expires
            _transactionPeriod = DefaultTransactionPeriod;
            _transactionThread = new Thread(TransactionTimer);
            _transactionThread.Name = "Labware RPC server transaction thread";
            _transactionThread.Start();
        }

        void EndTransaction()
        {
            // stop timeout timer
            _transactionPeriod = 0;
            _transactionEvent.Set();

            _transactionThread.Join();
        }

        void ResetTransaction()
        {
            // reset the transaction timer to prevent timeout for another transaction period
            _transactionEvent.Set();

        }

        void TransactionTimer()
        {
            while (_transactionEvent.WaitOne(_transactionPeriod))
                Thread.Sleep(0); // we're here if we received a set event, which effectively restarts the transaction timer

            // we're here if we timed out 
            
            // TODO -- UNLOCK LABWARE DB / UI

            _current_transaction_id = null; // end the current transaction

        }
    }


    public class LabwareXmlRpcServiceException : ApplicationException
    {
        public LabwareXmlRpcServiceException() : base() { }
        public LabwareXmlRpcServiceException(string msg) : base(msg) { }
        public LabwareXmlRpcServiceException(string msg, Exception innerEx) : base(msg, innerEx) { }
    }
}
