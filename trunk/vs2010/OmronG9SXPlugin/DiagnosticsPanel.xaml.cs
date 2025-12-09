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

namespace BioNex.OmronG9SXPlugin
{
    /// <summary>
    /// Interaction logic for DiagnosticsPanel.xaml
    /// </summary>
    public partial class DiagnosticsPanel : UserControl, INotifyPropertyChanged
    {
        private G9SX Controller { get; set; }
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

        private Brush _interlock_text_color;
        public Brush InterlockTextColor
        {
            get { return _interlock_text_color; }
            set {
                _interlock_text_color = value;
                OnPropertyChanged( "InterlockTextColor");
            }
        }

        private bool _enable_reset_animation;
        public bool EnableResetAnimation
        {
            get { return _enable_reset_animation; }
            set {
                _enable_reset_animation = value;
                OnPropertyChanged( "EnableResetAnimation");
            }
        }

        public RelayCommand ResetInterlocksCommand { get; set; }

        public DiagnosticsPanel( G9SX controller)
        {
            Controller = controller;
            InitializeComponent();
            WarningColor = Brushes.LightGray;
            InterlockColor = Brushes.LightGray;
            InterlockResetReadyColor = Brushes.LightGray;
            EnableResetAnimation = false;
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
            // DKM 2011-10-05 only used for Keyence plugin
            //WarningColor = Controller.WarningCondition ? Brushes.DarkOrange : Brushes.LightGray;
            InterlockColor = Controller.InterlockCondition ? Brushes.DarkRed : Brushes.LightGray;
            InterlockTextColor = Controller.InterlockCondition ? Brushes.Salmon : Brushes.Black;
            InterlockResetReadyColor = Controller.InterlockResetReadyCondition ? Brushes.Goldenrod : Brushes.LightGray;
            ResetInterlocksButtonGlowSize = Controller.InterlockResetReadyCondition ? 10 : 0;
            ResetInterlocksShadowDepth = Controller.InterlockResetReadyCondition ? 1 : 0;
            EnableResetAnimation = Controller.InterlockResetReadyCondition && Controller.InterlockCondition;
        }

        private void ExecuteResetInterlocks()
        {
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
