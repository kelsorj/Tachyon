using System;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// Interaction logic for GraphWindow.xaml
    /// </summary>
    public partial class VolumeMapWizard : Window
    {
        public VolumeMapWizard(Window owner, int columns)
        {
            Owner = owner;
            for (int i = 0; i < columns; ++i)
                _details.Add(new MapDetails() { Column = i + 1, Volume = 0.0 });
            Details = CollectionViewSource.GetDefaultView(_details);

            CommandCancel = new RelayCommand(ExecuteCommandCancel);
            CommandBeginScan = new RelayCommand(ExecuteCommandBeginScan);

            InitializeComponent();
            DataContext = this;
        }

        public RelayCommand CommandCancel { get; private set; }
        public RelayCommand CommandBeginScan { get; private set; }

        public ICollectionView Details { get; private set; }
        readonly ObservableCollection<MapDetails> _details = new ObservableCollection<MapDetails>();

        public MapDetails[] Map { get { return _details.ToArray(); } }

        public bool Cancelled { get; private set; }

        void ExecuteCommandCancel()
        {
            Cancelled = true;
            Close();
        }

        void ExecuteCommandBeginScan()
        {
            Cancelled = false;
            Close();
        }
    }
}
