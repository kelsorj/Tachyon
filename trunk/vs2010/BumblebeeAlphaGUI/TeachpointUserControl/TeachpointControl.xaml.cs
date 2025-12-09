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
using BumblebeeAlphaGUI;
using BioNex.Teachpoints;
using BioNex.TechnosoftLibrary;

namespace TeachpointUserControl
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        private AlphaHardware _hw = null;
        private Teachpoints _tps = null;

        public UserControl1()
        {
            InitializeComponent();
        }

        public void CreateTeachpointGUI( AlphaHardware hw, Teachpoints tps)
        {
            _hw = hw;
            _tps = tps;

            byte num_stages = hw.GetNumberOfStages();
            foreach( Channel c in hw)
                CreateChannelGUI( c.GetID(), num_stages);
            
            Button auto_teach_button = new Button();
            auto_teach_button.Content = "Auto teach remaining channels based on Channel 1";
            auto_teach_button.Margin = new Thickness( 3);
            auto_teach_button.Click += new RoutedEventHandler(auto_teach_button_Click);
            teachpoint_panel.Children.Add( auto_teach_button);

            CreateRobotTeachpointGUI( num_stages);
        }

        void auto_teach_button_Click(object sender, RoutedEventArgs e)
        {
            if( MessageBox.Show( "Are you sure you want to auto teach the remaining channels?", "Auto-teach confirmation", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            // auto teach
            //! \todo add channel spacing to hardware configuration, but for now, channel 2 is 18mm away,
            //!       channel 3 is 108mm away, and channel 4 is 126mm away
            // channel 2
            for( byte i=1; i<= _hw.GetNumberOfStages(); i++) {
                StageTeachpoint stp = _tps.GetStageTeachpoint( 1, i);
                _tps.AddUpperLeftStageTeachpoint( 2, i, stp.UpperLeft["x"], stp.UpperLeft["z"] - 5, stp.UpperLeft["y"] - 18, stp.UpperLeft["r"]);
                _tps.AddLowerRightStageTeachpoint( 2, i, stp.LowerRight["x"], stp.LowerRight["z"] - 5, stp.LowerRight["y"] - 18, stp.LowerRight["r"]);
            }
            // channel 3
            for( byte i=1; i<= _hw.GetNumberOfStages(); i++) {
                StageTeachpoint stp = _tps.GetStageTeachpoint( 1, i);
                _tps.AddUpperLeftStageTeachpoint( 3, i, stp.UpperLeft["x"], stp.UpperLeft["z"] - 5, stp.UpperLeft["y"] - 108, stp.UpperLeft["r"]);
                _tps.AddLowerRightStageTeachpoint( 3, i, stp.LowerRight["x"], stp.LowerRight["z"] - 5, stp.LowerRight["y"] - 108, stp.LowerRight["r"]);
            }
            // channel 4
            for( byte i=1; i<= _hw.GetNumberOfStages(); i++) {
                StageTeachpoint stp = _tps.GetStageTeachpoint( 1, i);
                _tps.AddUpperLeftStageTeachpoint( 4, i, stp.UpperLeft["x"], stp.UpperLeft["z"] - 5, stp.UpperLeft["y"] - 126, stp.UpperLeft["r"]);
                _tps.AddLowerRightStageTeachpoint( 4, i, stp.LowerRight["x"], stp.LowerRight["z"] - 5, stp.LowerRight["y"] - 126, stp.LowerRight["r"]);
            }
            _tps.SaveTeachpointFile( @"..\..\teachpoints.xml");
        }

        private void CreateChannelGUI( byte channel_id, byte num_stages)
        {
            GroupBox gb = new GroupBox();
            gb.Header = String.Format( "Channel {0}", channel_id);
            gb.BorderBrush = Brushes.Gray;
            gb.BorderThickness = new Thickness( 2);
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Vertical;
            // add the stage teachpoint buttons
            for( byte i=0; i<num_stages; i++) {
                StageTeachpointControl stc = new StageTeachpointControl();
                // need to name the teachpoint control so that we know where each button click
                // event originated from
                stc.Name = String.Format( "teachpointcontrol_{0}_{1}", channel_id, i + 1);
                stc.Stage = (byte)(i + 1);
                stc.Channel = channel_id;
                if( i % 2 == 0)
                    stc.SetBackgroundColor( Brushes.LightGray);
                stc.CommonStageButtonClick += new RoutedEventHandler(stc_CommonStageButtonClick);
                sp.Children.Add( stc);
            }
            // add the channel wash buttons
            WashTeachpointControl wtc = null;
            try {
                wtc = new WashTeachpointControl();
            } catch( TypeInitializationException ex) {
                MessageBox.Show( ex.InnerException.Message);
            }
            wtc.CommonWashButtonClick += new RoutedEventHandler( stc_CommonWashButtonClick);
            wtc.SetBackgroundColor( Brushes.LightBlue);
            wtc.Channel = channel_id;
            sp.Children.Add( wtc);
            // finally, add the stack panel to the groupbox for this channel
            gb.Content = sp;
            teachpoint_panel.Children.Add( gb);
        }

        void CreateRobotTeachpointGUI( byte num_stages)
        {
            GroupBox gb = new GroupBox();
            gb.Header = "Robot Teachpoints";
            gb.BorderBrush = Brushes.Gray;
            gb.BorderThickness = new Thickness( 2);
            StackPanel sp = new StackPanel();
            // also add controls for robot teachpoints
            for( byte i=1; i<=num_stages; i++) {
                RobotTeachpointControl rtc = new RobotTeachpointControl();
                rtc.Stage = i;
                rtc.CommonRobotButtonClick += new RoutedEventHandler(rtc_CommonRobotButtonClick);
                sp.Children.Add( rtc);
            }
            gb.Content = sp;
            teachpoint_panel.Children.Add( gb);
        }

        void rtc_CommonRobotButtonClick(object sender, RoutedEventArgs e)
        {
            ButtonEventIDWrapper id = e.OriginalSource as ButtonEventIDWrapper;
            if( id == null)
                return;
            switch( id.ButtonName) {
                case "button_robot_teach":
                    {
                        if( MessageBox.Show( "Are you sure you want to teach this point?", "Confirm teachpoint", MessageBoxButton.YesNo) == MessageBoxResult.No)
                            return;
                        Stage stage = _hw.GetStage( id.Stage);
                        double y = stage.GetY().GetPositionMM();
                        double r = stage.GetR().GetPositionMM();
                        _tps.AddRobotTeachpoint( id.Stage, y, r);
                        _tps.SaveTeachpointFile(@"..\..\teachpoints.xml");
                        break;
                    }
                case "button_robot_move":
                    {
                        Stage stage = _hw.GetStage( id.Stage);
                        Teachpoint tp = _tps.GetRobotTeachpoint( id.Stage);
                        stage.MoveAbsolute( tp["y"], tp["r"]);
                        break;
                    }
            }
        }

        void stc_CommonStageButtonClick(object sender, RoutedEventArgs e)
        {
            ButtonEventIDWrapper id = e.OriginalSource as ButtonEventIDWrapper;
            if( id == null)
                return;
            try
            {
                switch (id.ButtonName)
                {
                    // teaching statements
                    case "button_teach_ul":
                    case "button_teach_lr":
                        {
                            if( MessageBox.Show( "Are you sure you want to teach this point?", "Confirm teachpoint", MessageBoxButton.YesNo) == MessageBoxResult.No)
                                return;

                            Channel channel = _hw.GetChannel(id.Channel);
                            Stage stage = _hw.GetStage(id.Stage);
                            double x = channel.GetX().GetPositionMM();
                            double z = channel.GetZ().GetPositionMM();
                            double y = stage.GetY().GetPositionMM();
                            double r = stage.GetR().GetPositionMM();
                            if (id.ButtonName == "button_teach_ul")
                                _tps.AddUpperLeftStageTeachpoint(id.Channel, id.Stage, x, z, y, r);
                            else
                                _tps.AddLowerRightStageTeachpoint(id.Channel, id.Stage, x, z, y, r);
                            _tps.SaveTeachpointFile(@"..\..\teachpoints.xml");
                            break;
                        }
                    // move statements
                    case "button_moveabove_ul":
                    case "button_moveabove_lr":
                    case "button_move_ul":
                    case "button_move_lr":
                        {
                            // first, need to move all of the channels up so we don't crash other tips
                            // that may have been deployed!
                            for( byte i=1; i<=_hw.GetNumberOfChannels(); i++)
                                _hw.GetChannel( i).GetZ().MoveAbsolute( 0);
                            Channel channel = _hw.GetChannel(id.Channel);
                            Stage stage = _hw.GetStage(id.Stage);
                            StageTeachpoint stp = _tps.GetStageTeachpoint(id.Channel, id.Stage);
                            Teachpoint tp = null;
                            if (id.ButtonName == "button_moveabove_ul" || id.ButtonName == "button_move_ul")
                                tp = stp.UpperLeft;
                            else
                                tp = stp.LowerRight;
                            // move to z = 0;
                            channel.GetZ().MoveAbsolute(0);
                            // now move all other axes
                            channel.GetX().MoveAbsolute(tp["x"]);
                            stage.GetY().MoveAbsolute(tp["y"]);
                            stage.GetR().MoveAbsolute(tp["r"]);

                            // now move z down if we want to move to the teachpoint, instead of just moving above it
                            if (id.ButtonName == "button_move_ul" || id.ButtonName == "button_move_lr")
                                channel.GetZ().MoveAbsolute(tp["z"]);
                            break;
                        }
                }
            } catch (MotorException ex) {
                ErrorDialog.ErrorDialog dlg = new ErrorDialog.ErrorDialog();
                dlg.SetError( ex.Message);
                dlg.Show();
                //! \todo temp fix
                _hw.ResetAxisFault( ex.AxisID);
            }
        }

        void stc_CommonWashButtonClick(object sender, RoutedEventArgs e)
        {
            ButtonEventIDWrapper id = e.OriginalSource as ButtonEventIDWrapper;
            if( id == null)
                return;
            switch( id.ButtonName) {
                case "button_wash_teach_here":
                    {
                        Channel channel = _hw.GetChannel( id.Channel);
                        double x = channel.GetX().GetPositionMM();
                        double z = channel.GetZ().GetPositionMM();
                        _tps.AddWashTeachpoint( id.Channel, x, z);
                        _tps.SaveTeachpointFile( @"..\..\teachpoints.xml");
                        break;
                    }
                case "button_wash_move_above":
                case "button_wash_move_here":
                    {
                        Channel channel = _hw.GetChannel( id.Channel);
                        Teachpoint wtp = _tps.GetWashTeachpoint( id.Channel);
                        // move to z = 0;
                        channel.GetZ().MoveAbsolute( 0);
                        // now move all other axes
                        channel.GetX().MoveAbsolute( wtp["x"]);
                        // now move z down if we want to move to the teachpoint, instead of just moving above it
                        if( id.ButtonName == "button_wash_move_here")
                            channel.GetZ().MoveAbsolute( wtp["z"]);
                        break;
                    }
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach( Control c in teachpoint_panel.Children) {
                GroupBox gb = c as GroupBox;
                if( gb == null)
                    continue;
                StackPanel sp = gb.Content as StackPanel;
                if( sp == null)
                    continue;
                // loop over the stackpanel's children (stageteachpointcontrols) and resize
                foreach( Control stc in sp.Children)
                    stc.Width = ((TeachpointUserControl.UserControl1)sender).ActualWidth - 35;
            }
        }
    }
}
