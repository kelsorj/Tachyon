using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;

// TODO look up LINQ + select -- does the select portion result in the creation of an entirely new collection?

namespace BioNex.BumblebeePlugin.Scheduler.DualChannelScheduler
{
    public class ServiceSharedMemory : IReportsStatus
    {
        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// WorkingTransfer is simply a wrapper around a Transfer that is in the master
        /// transfer list, and it allows us to mark a transfer with attributes that
        /// the tip service uses to determine if it should be queued for transfer or not
        /// </summary>
        public enum TransferStatus
        {
            NotStarted,
            Aspirating,
            Dispensing,
            Done,
        }
        private class WorkingTransfer
        {
            public Transfer Transfer { get; set; }
            public TransferStatus Status { get; set; }
        }

        //-----------------------------------------------------------------------------------------
        // properties.
        //-----------------------------------------------------------------------------------------
        private HashSet< WorkingTransfer> AllWorkingTransfers { get; set; }
        private HashSet< WorkingTransfer> WorkingTransfers { get; set; }
        private List< Stage> Stages { get; set; }

        //-----------------------------------------------------------------------------------------
        // members.
        //-----------------------------------------------------------------------------------------
        private readonly ReaderWriterLockSlim lock_ = new ReaderWriterLockSlim();

        //-----------------------------------------------------------------------------------------
        // constructors.
        //-----------------------------------------------------------------------------------------
        public ServiceSharedMemory( BBHardware hardware, BumblebeeConfiguration config)
        {
            AllWorkingTransfers = new HashSet< WorkingTransfer>();
            WorkingTransfers = new HashSet< WorkingTransfer>();
            Stages = new List< Stage>( hardware.Stages);
        }

        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------
        // WORKING TRANSFERS
        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------
        public void SetTransferStatus( Transfer transfer, TransferStatus status)
        {
            lock_.EnterWriteLock();
            try {
                WorkingTransfers.First( wt => wt.Transfer == transfer).Status = status;
            } catch( Exception){
            } finally{
                lock_.ExitWriteLock();
            }
        }
        //-----------------------------------------------------------------------------------------
        public void AddTransfers( List< Transfer> transfers)
        {
            lock_.EnterWriteLock();
            try {
                AllWorkingTransfers.UnionWith( from t in transfers
                                               select new WorkingTransfer{ Transfer = t, Status = TransferStatus.NotStarted});
                var src_barcodes = ( from wt in AllWorkingTransfers
                                    select wt.Transfer.SrcPlate.Barcode).Distinct().ToList();
                var dst_barcodes = ( from wt in AllWorkingTransfers
                                    select wt.Transfer.DstPlate.Barcode).Distinct().ToList();
                Debug.Assert( src_barcodes.Intersect( dst_barcodes).Count() == 0);
            } catch( Exception){
            } finally{
                lock_.ExitWriteLock();
            }
        }
        //-----------------------------------------------------------------------------------------
        public void RemoveTransfer( Transfer transfer)
        {
            lock_.EnterWriteLock();
            try{
                WorkingTransfers.RemoveWhere( wt => wt.Transfer == transfer);
            } catch( Exception){
            } finally{
                lock_.ExitWriteLock();
            }
        }
        //-----------------------------------------------------------------------------------------
        public IList< Transfer> GetUnstartedTransfersOnDeck()
        {
            lock_.EnterReadLock();
            try {
                var barcodes_on_deck = Stages.Where( s => s.Plate != null).Select( s => s.Plate.Barcode);
                return ( from wt in WorkingTransfers 
                         where wt.Status == TransferStatus.NotStarted && barcodes_on_deck.Contains( wt.Transfer.SrcPlate.Barcode) && barcodes_on_deck.Contains( wt.Transfer.DstPlate.Barcode)
                         select wt.Transfer).ToList();
            } catch( Exception){
            } finally{
                lock_.ExitReadLock();
            }
            return null;
        }
        //-----------------------------------------------------------------------------------------
        public IList< Transfer> GetUnstartedTransfersBySrcAndDstBarcode( string src_barcode, string dst_barcode)
        {
            lock_.EnterReadLock();
            try {
                return ( from wt in WorkingTransfers 
                         where wt.Transfer.SrcPlate.Barcode == src_barcode && wt.Transfer.DstPlate.Barcode == dst_barcode && wt.Status == TransferStatus.NotStarted
                         select wt.Transfer).ToList();
            } catch( Exception){
            } finally{
                lock_.ExitReadLock();
            }
            return null;
        }
        //-----------------------------------------------------------------------------------------
        public bool PlateDone( Plate plate)
        {
            lock_.EnterReadLock();
            try{
                int num_transfers_involved = 0;
                if( plate is SourcePlate){
                    num_transfers_involved = WorkingTransfers.Count( wt => ( wt.Transfer.SrcPlate.Barcode == plate.Barcode) && ( wt.Status != TransferStatus.Done && wt.Status != TransferStatus.Dispensing));
                } else if( plate is DestinationPlate){
                    num_transfers_involved = WorkingTransfers.Count( wt => ( wt.Transfer.DstPlate.Barcode == plate.Barcode) && wt.Status != TransferStatus.Done);
                } else{
                    Debug.Assert( false, "um, what type of plate is this?");
                }
                return num_transfers_involved == 0;
            } catch( Exception){
            } finally{
                lock_.ExitReadLock();
            }
            return false;
        }
        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------
        // STAGE USAGE
        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------
        public void SetStagePlate( Stage stage, Plate plate)
        {
            lock_.EnterWriteLock();
            try {
                WorkingTransfers.UnionWith( from wt in AllWorkingTransfers
                                            where wt.Transfer.SrcPlate == plate || wt.Transfer.DstPlate == plate
                                            select wt);
                AllWorkingTransfers.RemoveWhere( wt => wt.Transfer.SrcPlate == plate || wt.Transfer.DstPlate == plate);
                stage.Plate = plate;
            } catch( Exception){
            } finally{
                lock_.ExitWriteLock();
            }
        }
        //-----------------------------------------------------------------------------------------
        #region IReportsStatus Members
        public string GetStatus()
        {
            StringBuilder sb = new StringBuilder();

            // AllWorkingTransfers
            sb.AppendLine( "AllWorkingTransfers:");
            foreach( var transfer in AllWorkingTransfers) {
                sb.AppendLine( String.Format( "Transfer {0}.{1}->{2}.{3}, {4}", transfer.Transfer.SrcPlate.Barcode.Value, transfer.Transfer.SrcWell.WellName, transfer.Transfer.DstPlate.Barcode.Value, transfer.Transfer.DstWell.WellName, transfer.Status.ToString()));
            }

            // WorkingTransfers
            sb.AppendLine( "WorkingTransfers:");
            foreach( var transfer in WorkingTransfers) {
                sb.AppendLine( String.Format( "Transfer {0}.{1}->{2}.{3}, {4}", transfer.Transfer.SrcPlate.Barcode.Value, transfer.Transfer.SrcWell.WellName, transfer.Transfer.DstPlate.Barcode.Value, transfer.Transfer.DstWell.WellName, transfer.Status.ToString()));
            }

            return sb.ToString();
        }
        #endregion
    }
}
