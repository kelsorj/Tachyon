using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using BioNex.Shared.LibraryInterfaces;

namespace BioNex.Bumblebee2DSimulatorControl
{
    public class Well
    {
        int _index;
        
        public string WellName { get; set; }
        /// <remark>
        /// I know this isn't the proper way to deal with the well diameter and boundary values.  Normally,
        /// I would have the Well class implement BaseViewModel and have the properties here.  But I noticed
        /// that it's not necessary to do this because the request for the value can somehow be linked to
        /// the parent class.  I don't know why this works, but I left it this way so I can investigate it later.
        /// </remark>
        public double WellDiameter { get; set; }
        public double WellBoundary { get; set; }

        public Well( int index, int number_of_wells, double diameter, double boundary)
        {
            _index = index;
            WellName = BioNex.Shared.Utils.Wells.IndexToWellName( index, number_of_wells);
            WellDiameter = diameter;
            WellBoundary = boundary;
        }
    }

    public class TipViewModel : MVVM.BaseViewModel
    {
        private double _x;
        public double X
        {
            get { return _x; }
            set {
                _x = value;
                RaisePropertyChanged( "X");
            }
        }
        private double _z;
        public double Z
        {
            get { return _z; }
            set {
                _z = value;
                RaisePropertyChanged( "Z");
            }
        }
        private double _w;
        public double W
        {
            get { return _w; }
            set {
                _w = value;
                RaisePropertyChanged( "W");
            }
        }
    }

    public class PlateViewModel : MVVM.BaseViewModel
    {
        private ILabware _labware;

        private double _y;
        public double Y
        {
            get { return _y; }
            set {
                _y = value;
                RaisePropertyChanged( "Y");
            }
        }
        private double _r;
        public double R
        {
            get { return _r; }
            set {
                _r = value;
                RaisePropertyChanged( "R");
            }
        }

        private List<Well> _wells = new List<Well>();
        public List<Well> Wells
        { 
            get {
                return _wells;
            }
            set {
                _wells = value;
                RaisePropertyChanged( "Wells");
            }
        }

        private double _well_diameter;
        public double WellDiameter
        {
            get { return _well_diameter; }
            set {
                _well_diameter = value;
                RaisePropertyChanged( "WellDiameter");
            }
        }

        private double _well_boundary;
        public double WellBoundary
        {
            get { return _well_boundary; }
            set {
                _well_boundary = value;
                RaisePropertyChanged( "WellBoundary");
            }
        }
        public PlateViewModel( ILabware labware)
        {
            _labware = labware;
            if( labware != null) {
                for( int i=0; i<_labware.NumberOfWells; i++) {
                    if( _labware.NumberOfWells == 96) {
                        WellDiameter = 7;
                        WellBoundary = 10;
                    } else {
                        WellDiameter = 3;
                        WellBoundary = 5;
                    }
                    _wells.Add( new Well( i, _labware.NumberOfWells, WellDiameter, WellBoundary));
                }
            } else {
                _wells.Clear();
            }
            RaisePropertyChanged( "Wells");
        }
    }

    public class ViewModel : MVVM.BaseViewModel
    {
        private double _workspace_width;
        public double WorkspaceWidth
        {
            get { return _workspace_width; }
            set {
                _workspace_width = value;
                RaisePropertyChanged( "WorkspaceWidth");
            }
        }

        private double _workspace_height;
        public double WorkspaceHeight
        {
            get { return _workspace_height; }
            set {
                _workspace_height = value;
                RaisePropertyChanged( "WorkspaceHeight");
            }
        }

        public ViewModel()
        {
            WorkspaceHeight = 480;
            WorkspaceWidth = 640;
        }
    }
}
