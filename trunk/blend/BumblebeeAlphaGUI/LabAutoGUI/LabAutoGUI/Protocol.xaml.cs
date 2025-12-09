using System;
using System.Collections.Generic;
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
using BumblebeeAlphaGUI.ViewModel;

namespace LabAutoGUI
{
	/// <summary>
	/// Interaction logic for Protocol.xaml
	/// </summary>
	public partial class Protocol : UserControl
	{
		private MainViewModel _vm;
		public MainViewModel ViewModel
		{
			get { return _vm; }
			set {
				this.DataContext = value;
				_vm = value;
			}
		}
		
		public Protocol()
		{
			this.InitializeComponent();
		}
		
		public void button_hitpick_file_Click( object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
			dlg.Filter = "XML files (*.xml)|*.xml";
			dlg.ShowDialog();
			ViewModel.HitpickFilepath = dlg.FileName;
		}
	}
}