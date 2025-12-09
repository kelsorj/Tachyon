namespace BioNex.BumblebeePlugin.ViewModel
{
    public partial class MainViewModel
    {
        // ----------------------------------------------------------------------
        // tooltips.
        // ----------------------------------------------------------------------
        string jog_positive_tooltip_;
        public string JogPositiveToolTip
        {
            get { return jog_positive_tooltip_; }
            set {
                jog_positive_tooltip_ = value;
                RaisePropertyChanged( "JogPositiveToolTip");
            }
        }
        // ----------------------------------------------------------------------
        string jog_negative_tooltip_;
        public string JogNegativeToolTip
        {
            get { return jog_negative_tooltip_; }
            set {
                jog_negative_tooltip_ = value;
                RaisePropertyChanged( "JogNegativeToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string _home_xaxis_tooltip;
        public string HomeXAxisToolTip
        {
            get { return _home_xaxis_tooltip; }
            set {
                _home_xaxis_tooltip = value;
                RaisePropertyChanged( "HomeXAxisToolTip");
            }
        }
        // ----------------------------------------------------------------------
        // DKM 2011-03-02 actually can't use this right now, because the tooltip is currently something that shows a popup with position information in it.
        private string _move_above_ul_tooltip;
        public string MoveAboveULToolTip
        {
            get { return _move_above_ul_tooltip; }
            set {
                _move_above_ul_tooltip = value;
                RaisePropertyChanged( "MoveAboveULToolTip");
            }
        }
        // ----------------------------------------------------------------------
        // DKM 2011-03-02 actually can't use this right now, because the tooltip is currently something that shows a popup with position information in it.
        private string _move_above_lr_tooltip;
        public string MoveAboveLRToolTip
        {
            get { return _move_above_lr_tooltip; }
            set {
                _move_above_lr_tooltip = value;
                RaisePropertyChanged( "MoveAboveLRToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string _move_above_tipshuck_tooltip;
        public string MoveAboveTipShuckToolTip
        {
            get { return _move_above_tipshuck_tooltip; }
            set {
                _move_above_tipshuck_tooltip = value;
                RaisePropertyChanged( "MoveAboveTipShuckToolTip");
            }
        }
        // ----------------------------------------------------------------------
        // DKM 2011-03-02 actually can't use this right now, because the tooltip is currently something that shows a popup with position information in it.
        private string _move_to_tipshuck_tooltip;
        public string MoveToTipShuckToolTip
        {
            get { return _move_to_tipshuck_tooltip; }
            set {
                _move_to_tipshuck_tooltip = value;
                RaisePropertyChanged( "MoveToTipShuckToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string _teach_tipshuck_tooltip;
        public string TeachTipShuckToolTip
        {
            get { return _teach_tipshuck_tooltip; }
            set {
                _teach_tipshuck_tooltip = value;
                RaisePropertyChanged( "TeachTipShuckToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string _press_tip_on_tooltip;
        public string PressTipOnToolTip
        {
            get { return _press_tip_on_tooltip; }
            set {
                _press_tip_on_tooltip = value;
                RaisePropertyChanged( "PressTipOnToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string _shuck_tip_off_tooltip;
        public string ShuckTipOffToolTip
        {
            get { return _shuck_tip_off_tooltip; }
            set {
                _shuck_tip_off_tooltip = value;
                RaisePropertyChanged( "ShuckTipOffToolTip");
            }
        }
        // ----------------------------------------------------------------------
        private string _random_tip_test_tooltip;
        public string RandomTipTestToolTip
        {
            get { return _random_tip_test_tooltip; }
            set {
                _random_tip_test_tooltip = value;
                RaisePropertyChanged( "RandomTipTestToolTip");
            }
        }
    }
}