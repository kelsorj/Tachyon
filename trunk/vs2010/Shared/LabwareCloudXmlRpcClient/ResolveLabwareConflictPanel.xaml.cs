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
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;

namespace BioNex.Shared.LabwareCloudXmlRpcClient
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ResolveLabwareConflict : Window
    {
        public string ResolutionLabel { get; set; }
        public RelayCommand DiscardCommand { get; set; }
        public RelayCommand AcceptCommand { get; set; }

        public class Conflict
        {
            public string Name { get; set; }
            public string LocalValue { get; set; }
            public string RemoteValue { get; set; }
        }
        public List<Conflict> Conflicts { get; set; }
        
        public ResolveLabwareConflict(string labware_name, List<Conflict> conflicts)
        {
            ResolutionLabel = string.Format("Labware '{0}' is present in both the local and remote database,\nbut is not identical in both places.\n\nDo you want to accept or discard the remote version?", labware_name);
            DiscardCommand = new RelayCommand(() => { DialogResult = false; Close(); });
            AcceptCommand = new RelayCommand(() => { DialogResult = true; Close(); });

            Conflicts = conflicts;
            
            DataContext = this;

            InitializeComponent();
        }
    }
}
