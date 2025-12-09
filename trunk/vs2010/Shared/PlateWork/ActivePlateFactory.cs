using System;
using System.Linq;

namespace BioNex.Shared.PlateWork
{
    public abstract class ActivePlateFactory
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected Worklist Worklist { get; set; }
        public int NumberOfPlatesToCreate { get; set; }
        public int PlateInstanceIndex { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ActivePlateFactory( Worklist worklist)
        {
            Worklist = worklist;
            SetNumberOfPlatesToCreate();
            PlateInstanceIndex = 0;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public abstract Type GetActivePlateType();

        public ActivePlate CreateActivePlate()
        {
            --NumberOfPlatesToCreate;
            // DKM 2011-10-05 I think whether or not using MutableString to solve the barcode-reassignment problem hinges
            //                on the creation of ActivePlate.  Worklist has a reference to Plates which have the MutableString.
            //                So the question is, when we create the ActivePlate object, are the Plate bits a copy?  Inside
            //                of the ActiveDestinationPlate constructor, it looks like the internal destination plate object
            //                is a new object.  Bummer.  Let's see what we can do in there.
            // DKM 2011-10-09 need to check if there are any more destination plates to create.  If not, bail, otherwise
            //                we'll go beyond the bounds of the dest plate array when calling CreateInstance.
            if (NumberOfPlatesToCreate < 0) {
                // DKM 2012-01-18 prevent possible OverflowException when CreatePlate is called over and over again
                NumberOfPlatesToCreate = 0;
                return null;
            }
            ActivePlate active_plate = ( ActivePlate)( Activator.CreateInstance( GetActivePlateType(), Worklist, PlateInstanceIndex));
            PlateInstanceIndex++;
            return active_plate;
        }

        public abstract void SetNumberOfPlatesToCreate();

        public abstract int NumberOfSimultaneousPlates();

        public virtual bool ReleaseActivePlate()
        {
            int active_plates_of_active_plate_type = ActivePlate.ActivePlates.Count( active_plate => active_plate.GetType() == GetActivePlateType());
            return active_plates_of_active_plate_type < NumberOfSimultaneousPlates();
        }

        public ActivePlate TryReleaseActivePlate()
        {
            return ReleaseActivePlate() ? CreateActivePlate() : null;
        }
    }

    public class ActiveSourcePlateFactory : ActivePlateFactory
    {
        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ActiveSourcePlateFactory( Worklist worklist)
            : base( worklist)
        {
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public override Type GetActivePlateType()
        {
            return typeof( ActiveSourcePlate);
        }

        public override void SetNumberOfPlatesToCreate()
        {
            NumberOfPlatesToCreate = Worklist.TransferOverview.Transfers.Select( t => t.SrcPlate).Distinct().Count();
        }

        public override int NumberOfSimultaneousPlates()
        {
            return 3;
        }

        public override bool ReleaseActivePlate()
        {
            return base.ReleaseActivePlate();
        }
    }

    public class ActiveDestinationPlateFactory : ActivePlateFactory
    {
        // ----------------------------------------------------------------------
        // members
        // ----------------------------------------------------------------------

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ActiveDestinationPlateFactory( Worklist worklist)
            : base( worklist)
        {
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public override Type GetActivePlateType()
        {
            return typeof( ActiveDestinationPlate);
        }

        public override void SetNumberOfPlatesToCreate()
        {
            NumberOfPlatesToCreate = Worklist.TransferOverview.Transfers.Select( t => t.DstPlate).Distinct().Count();
        }

        public override int NumberOfSimultaneousPlates()
        {
            return 2;
        }

        public override bool ReleaseActivePlate()
        {
            return base.ReleaseActivePlate();
        }
    }
}
