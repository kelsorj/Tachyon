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
using BioNex.Shared.LibraryInterfaces;
using System.ComponentModel.Composition;
using GalaSoft.MvvmLight.Command;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using BioNex.Shared.Utils;
using System.IO;
using System.Xml.Serialization;
using log4net;
using System.Threading;

namespace BioNex.DC2DelidCycleTesterGUI
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [Export(typeof(ICustomerGUI))]
    public partial class Checklist : UserControl, ICustomerGUI
    {
        [Import]
        private Lazy<ICustomSynapsisQuery> SynapsisQuery { get; set; }
        public Model _model { get; private set; }
        public event EventHandler ProtocolComplete;
        public event EventHandler AbortableTaskStarted;
        public event EventHandler AbortableTaskComplete;

        // logging to _log goes into all of the main log's appenders
        private static readonly ILog _log = LogManager.GetLogger( "LabAutoGUI");

        private AutoResetEvent CloseHourglassWindowEvent = new AutoResetEvent( false);

        [ImportingConstructor]
        public Checklist([Import] Model model)
        {
            InitializeComponent();
            _model = model;
            this.DataContext =  this;
            InitializeCommands();
        }

        ~Checklist()
        {
            _model.ProtocolComplete -= this.ProtocolComplete;
        }

        private void InitializeCommands()
        {
            HomeAllDevicesCommand = new RelayCommand( HomeAllDevices, CanExecuteHomeAllDevicesCommand );
        }

        public RelayCommand HomeAllDevicesCommand { get; set; }


        private bool ExecuteHitpick()
        {
            // wrap the ExecuteHitpick call in the HourglassWindow so that the user knows the system
            // isn't hung.  Wrapper function is in the ViewModel because the parameters are BB-specific,
            // so the details couldn't be in the shared class.
            return ExecuteHitpickWithHourglassWindow( _model.ExecuteDelidCycleTest);
        }

        private bool ExecuteHitpickWithHourglassWindow( Func<bool> action)
        {
            action.BeginInvoke( OnActionComplete, null);
            var hg = new BioNex.Shared.Utils.HourglassWindow()
            {
                Title = "Enabling motors",
                Owner = Application.Current.MainWindow
            };
            hg.Show();
            //! \todo figure out another way to accomplish this non-blocking UI behavior without DoEvents
            while( !CloseHourglassWindowEvent.WaitOne( 10))
                System.Windows.Forms.Application.DoEvents();
            hg.Close();

            //! \todo get the real result from action!!!
            return _hitpick_ok_to_start;
        }

        private bool _hitpick_ok_to_start { get; set; }

        private void OnActionComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                var caller = (Func<bool>)ar.AsyncDelegate;
                _hitpick_ok_to_start = caller.EndInvoke( iar);
            } catch( Exception ex) {
                _log.Error( ex.Message);
            } finally {
                CloseHourglassWindowEvent.Set();
            }
        }



        private void HomeAllDevices()
        {
            SynapsisQuery.Value.HomeAllDevices();
        }

        private bool CanExecuteHomeAllDevicesCommand()
        {
            string reason;
            bool ok = SynapsisQuery.Value.ClearToHome( out reason);
            return ok;
        }

        #region ICustomerGUI Members

        public bool CanExecuteStart(out IEnumerable<string> failure_reasons)
        {
            List<string> reasons = new List<string>();
            failure_reasons = reasons;
            return reasons.Count() == 0;
        }

        public bool ExecuteStart()
        {
            _model.ProtocolComplete += this.ProtocolComplete;
            // REED change "" to "Change tip" to enable tip changing
            bool result =  ExecuteHitpick( );
            return result;
        }

        public bool ShowProtocolExecuteButtons()
        {
            return true;
        }

        public string GUIName
        {
            get { return "DC2 Delid Cycle Test GUI"; }
        }

        public bool Busy
        {
            get { return false; }
        }

        public string BusyReason
        {
            get 
            {
                return "Not busy";
            }
        }

        public void Close()
        {
            
        }

        public bool CanPause()
        {
            return true;
        }

        public bool CanClose()
        {
            return true;
        }

        public bool AllowDiagnostics() { return true; }

        public void CompositionComplete(){}
        #endregion
    }
}
