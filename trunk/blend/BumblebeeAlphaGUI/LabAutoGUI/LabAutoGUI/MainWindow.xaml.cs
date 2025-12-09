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
using System.Windows.Shapes;
using BumblebeeAlphaGUI.ViewModel;

namespace LabAutoGUI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
        private MainViewModel _vm;
		private BumblebeeAlphaGUI.HardwareVisualizerViewModel _hvvm = new BumblebeeAlphaGUI.HardwareVisualizerViewModel();

		public MainWindow()
		{
			this.InitializeComponent();

			// Insert code required on object creation below this point.
            _vm = new MainViewModel( _hvvm);
			SetUpVisualization();
            this.DataContext = _vm;
			_vm.Initialize();
			
			this.protocol_page.ViewModel = _vm;
			this.diagnostics_page.ViewModel = _vm;
		}
		
		private void SetUpVisualization()
		{
            _hvvm.PlateViewModels[0] = visualizer.LoadPlate( 0, null);
            _hvvm.PlateViewModels[1] = visualizer.LoadPlate( 1, null);
            _hvvm.PlateViewModels[2] = visualizer.LoadPlate( 2, null);
            for( int i=0; i<4; i++)
                visualizer.Tips[i].ViewModel = _hvvm.TipViewModels[i];
		}
	}
}