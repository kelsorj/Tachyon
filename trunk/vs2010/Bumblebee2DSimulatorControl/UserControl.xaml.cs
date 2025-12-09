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
using System.Windows.Threading;
using BioNex.Shared.LibraryInterfaces;

namespace BioNex.Bumblebee2DSimulatorControl
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Simulation : UserControl
    {
        public List<Plate> Plates { get; private set; }
        public List<Tip> Tips { get; private set; }

        public Simulation()
        {
            InitializeComponent();
            Plates = new List<Plate>();
            Tips = new List<Tip>();

            Plates.Add( Plate1);
            Plates.Add( Plate2);
            Plates.Add( Plate3);
            Tips.Add( Tip1);
            Tips.Add( Tip2);
            Tips.Add( Tip3);
            Tips.Add( Tip4);
        }

        public PlateViewModel LoadPlate( uint stage_id_0_based, ILabware labware)
        {
            double y = 0;
            double r = 0;
            PlateViewModel new_model;
            switch( stage_id_0_based)
            {
                case 0:
                    if( Plate1.ViewModel != null) {
                        y = Plate1.ViewModel.Y;
                        r = Plate1.ViewModel.R;
                    }
                    new_model = new PlateViewModel( labware);
                    new_model.Y = y;
                    new_model.R = r;
                    Plate1.ViewModel = new_model;
                    return new_model;
                case 1:
                    if( Plate2.ViewModel != null) {
                        y = Plate2.ViewModel.Y;
                        r = Plate2.ViewModel.R;
                    }
                    new_model = new PlateViewModel( labware);
                    new_model.Y = y;
                    new_model.R = r;
                    Plate2.ViewModel = new_model;
                    return new_model;
                case 2:
                    if( Plate3.ViewModel != null) {
                        y = Plate3.ViewModel.Y;
                        r = Plate3.ViewModel.R;
                    }
                    new_model = new PlateViewModel( labware);
                    new_model.Y = y;
                    new_model.R = r;
                    Plate3.ViewModel = new_model;
                    return new_model;
            }
            return null;
        }

        public void AddTip( TipViewModel vm)
        {
            switch( Tips.Count)
            {
                case 1:
                    Tips.Add( Tip1);
                    Tip1.ViewModel = vm;
                    break;
                case 2:
                    Tips.Add( Tip2);
                    Tip2.ViewModel = vm;
                    break;
                case 3:
                    Tips.Add( Tip3);
                    Tip3.ViewModel = vm;
                    break;
                case 4:
                    Tips.Add( Tip4);
                    Tip4.ViewModel = vm;
                    break;
            }
        }
    }
}
