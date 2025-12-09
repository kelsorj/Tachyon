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
using System.ComponentModel;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using BioNex.Shared.DeviceInterfaces;

namespace BioNex.KeyenceSZ01SPlugin
{
    /// <summary>
    /// Interaction logic for DiagnosticsPanel.xaml
    /// </summary>
    public partial class DiagnosticsPanel : UserControl, INotifyPropertyChanged
    {
        private SZ01S Controller { get; set; }
        private DispatcherTimer Timer { get; set; }

        private Brush warning_color_;
        public Brush WarningColor 
        {
            get { return warning_color_; }
            set {
                warning_color_ = value;
                OnPropertyChanged( "WarningColor");
            }
        }

        private Brush interlock_reset_ready_color_;
        public Brush InterlockResetReadyColor
        {
            get { return interlock_reset_ready_color_; }
            set {
                interlock_reset_ready_color_ = value;
                OnPropertyChanged( "InterlockResetReadyColor");
            }
        }

        private int _reset_interlocks_button_glow_size;
        public int ResetInterlocksButtonGlowSize
        {
            get { return _reset_interlocks_button_glow_size; }
            set {
                _reset_interlocks_button_glow_size = value;
                OnPropertyChanged( "ResetInterlocksButtonGlowSize");
            }
        }

        private int _reset_interlocks_shadow_depth;
        public int ResetInterlocksShadowDepth
        {
            get { return _reset_interlocks_shadow_depth; }
            set {
                _reset_interlocks_shadow_depth = value;
                OnPropertyChanged( "ResetInterlocksShadowDepth");
            }
        }

        private Brush interlock_color_;
        public Brush InterlockColor
        {
            get { return interlock_color_; }
            set {
                interlock_color_ = value;
                OnPropertyChanged( "InterlockColor");
            }
        }

        public RelayCommand ResetInterlocksCommand { get; set; }

        public DiagnosticsPanel( SZ01S controller)
        {
            Controller = controller;
            InitializeComponent();
            WarningColor = Brushes.LightGray;
            InterlockColor = Brushes.LightGray;
            InterlockResetReadyColor = Brushes.LightGray;
            ResetInterlocksButtonGlowSize = 0;
            ResetInterlocksShadowDepth = 0;

            ResetInterlocksCommand = new RelayCommand( ExecuteResetInterlocks);

            DataContext = this;

            Timer = new DispatcherTimer();
            Timer.Interval = new TimeSpan( 0, 0, 0, 0, 100);
            Timer.Tick += new EventHandler(Timer_Tick);
            Timer.Start();
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            WarningColor = Controller.WarningCondition ? Brushes.DarkOrange : Brushes.LightGray;
            InterlockColor = Controller.InterlockCondition ? Brushes.DarkRed : Brushes.LightGray;
            InterlockResetReadyColor = Controller.InterlockResetReadyCondition ? Brushes.Goldenrod : Brushes.LightGray;
            ResetInterlocksButtonGlowSize = Controller.InterlockResetReadyCondition ? 10 : 0;
            ResetInterlocksShadowDepth = Controller.InterlockResetReadyCondition ? 1 : 0;
        }

        private void ExecuteResetInterlocks()
        {
            // #403: let Synapsis handle reset now because we added a software interlock that also needs to be reset.
            // keep all of the logic for safety reset in one place.  Synapsis will call into Controller.Reset() via
            // the SafetyInterface's Reset() method.
            //Controller.Reset();
            Messenger.Default.Send<ResetInterlocksMessage>( new ResetInterlocksMessage());
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }
}
