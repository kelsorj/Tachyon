using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Bumblebee2DSimulatorControl;
using System.Windows.Threading;
using BioNex.Shared.LabwareDatabase;
using BioNex.Bumblebee2DSimulatorControl;
using BioNex.Shared.LibraryInterfaces;

namespace SimulatorTestContainer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private DispatcherTimer _timer = new DispatcherTimer();
        List<ILabware> _labware = new List<ILabware>();
        int _plate_counter;

        // container should own the viewmodels
        PlateViewModel p1vm = new PlateViewModel( null);
        PlateViewModel p2vm = new PlateViewModel( null);
        PlateViewModel p3vm = new PlateViewModel( null);
        TipViewModel t1vm = new TipViewModel();
        TipViewModel t2vm = new TipViewModel();
        TipViewModel t3vm = new TipViewModel();
        TipViewModel t4vm = new TipViewModel();

        public Window1()
        {
            InitializeComponent();
            LabwareDatabase ldb = new LabwareDatabase( BioNex.Shared.Utils.FileSystem.GetAppPath() +  "\\labware.s3db");
            _labware.Add( ldb["NUNC 96 clear round well flat bottom"]);
            _labware.Add( ldb["NUNC 96 clear round well flat bottom"]);
            _labware.Add( ldb["NUNC 384 clear drafted square well"]);

            // set up viewmodels
            sim.Plates[0].ViewModel = p1vm;
            sim.Plates[1].ViewModel = p2vm;
            sim.Plates[2].ViewModel = p3vm;
            sim.Tips[0].ViewModel = t1vm;
            sim.Tips[1].ViewModel = t2vm;
            sim.Tips[2].ViewModel = t3vm;
            sim.Tips[3].ViewModel = t4vm;

            _timer.Interval = new TimeSpan( 0, 0, 0, 0, 50);
            _timer.Tick += new EventHandler( _timer_Tick);
            _timer.Start();
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            // stages
            p1vm.Y = (p1vm.Y + 1) % 400;
            p1vm.R = (p1vm.R + 1) % 360;
            if( p1vm.R == 359)
                p1vm = sim.LoadPlate( 0, _labware[(_plate_counter++ % 3)]);
            p2vm.Y = (p2vm.Y + 2) % 400;
            p2vm.R = (p2vm.R + 2) % 360;
            if( p2vm.R == 358)
                p2vm = sim.LoadPlate( 1, _labware[(_plate_counter++ % 3)]);
            p3vm.Y = (p3vm.Y + 3) % 400;
            p3vm.R = (p3vm.R + 3) % 360;
            if( p3vm.R == 357)
                p3vm = sim.LoadPlate( 2, _labware[(_plate_counter++ % 3)]);
            // tips
            t1vm.X = (t1vm.X + 1) % 600;
            t2vm.X = (t2vm.X + 2) % 600;
            t3vm.X = (t3vm.X + 3) % 600;
            t4vm.X = (t4vm.X + 4) % 600;
        }
    }
}