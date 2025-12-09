using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using BioNex.Shared.Utils;

namespace BioNex.Shared.SimpleWizard
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Wizard : Window, INotifyPropertyChanged
    {
        public event EventHandler WizardCompleted;
        public event EventHandler WizardFailed;

        public class WizardStep : INotifyPropertyChanged
        {
            public SimpleRelayCommand NextCommand { get; set; }

            private Visibility _success_visibility;
            public Visibility SuccessVisibility
            {
                get { return _success_visibility; }
                set {
                    _success_visibility = value;
                    OnPropertyChanged( "SuccessVisibility");
                }
            }

            private Visibility _fail_visibility;
            public Visibility FailVisibility
            {
                get { return _fail_visibility; }
                set {
                    _fail_visibility = value;
                    OnPropertyChanged( "FailVisibility");
                }
            }

            public string Caption { get; private set; }
            public Func<bool> FunctionWrapper { get; private set; }
            
            private bool _next_enabled;
            public bool NextEnabled
            {
                get { return _next_enabled; }
                set {
                    _next_enabled = value;
                    OnPropertyChanged( "NextEnabled");
                }
            }

            private Visibility _next_visibility;
            public Visibility NextVisibility
            {
                get { return _next_visibility; }
                set {
                    _next_visibility = value;
                    OnPropertyChanged( "NextVisibility");
                }
            }

            public AutoResetEvent NextEvent { get; private set; }

            public WizardStep( string caption, Func<bool> func, bool wait_for_user_click)
            {
                SuccessVisibility = Visibility.Hidden;
                FailVisibility = Visibility.Hidden;
                Caption = caption;
                FunctionWrapper = func;
                NextVisibility = wait_for_user_click ? Visibility.Visible : Visibility.Hidden;
                NextEnabled = false;
                NextEvent = new AutoResetEvent( false); 

                NextCommand = new SimpleRelayCommand( () => { NextEvent.Set(); });
            }

            public bool Execute()
            {
                // might not need to do anything for this step, i.e. it could be the last one that's just a user message to close the window.
                if( FunctionWrapper == null) {
                    if( NextVisibility == Visibility.Visible)
                        NextEnabled = true;
                    return true;
                }

                if( FunctionWrapper()) {
                    SuccessVisibility = Visibility.Visible;
                    NextEnabled = true;
                    return true;
                } else {
                    FailVisibility = Visibility.Visible;
                    return false;
                }
            }

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged( string property_name)
            {
                if( PropertyChanged != null)
                    PropertyChanged( this, new PropertyChangedEventArgs( property_name));
            }

            #endregion
        }

        /// <summary>
        /// This is the original list of steps that gets passed in.  We need this so that we are able
        /// to display a step at a time, instead of all of the steps at once.
        /// </summary>
        private readonly IEnumerable<WizardStep> _master_steps;

        private ObservableCollection<WizardStep> _steps;
        public ObservableCollection<WizardStep> Steps
        {
            get { return _steps; }
            set {
                _steps = value;
                OnPropertyChanged( "Steps");
            }
        }

        public Wizard( string title, IEnumerable<WizardStep> steps, Dispatcher main_thread_dispatcher=null)
        {
            InitializeComponent();

            this.Title = title;
            _master_steps = steps;
            Steps = new ObservableCollection<WizardStep>();

            // based on the number of steps and their content, approximate the ideal window size
            const int unit_height = 40;
            Height = unit_height * _master_steps.Count();
            const int max_width = 500;
            // getting the font size is a PITA, even though it's been documented in this blog: http://incrediblejourneysintotheknown.blogspot.com/2008/07/glyphrun-and-so-forth.html
            // for now, let's just fix the width and not even deal with it.
            Width = max_width;

            // launch the thread that will run the steps
            Action<Dispatcher> run_steps = new Action<Dispatcher>( ExecuteStepsThread);
            run_steps.BeginInvoke( this.Dispatcher, null, null);            
        }

        private void ExecuteStepsThread( Dispatcher d)
        {
            bool succeeded = true;
            foreach( var step in _master_steps) {
                d.Invoke( new Action( () => { AddStep( step); }));

                if( !step.Execute()) {
                    succeeded = false;
                    break;
                }

                // wait for the next button, if necessary
                if( step.NextVisibility == System.Windows.Visibility.Visible) {
                    step.NextEvent.WaitOne();                
                    step.NextVisibility = System.Windows.Visibility.Hidden;
                    step.SuccessVisibility = Visibility.Visible;
                }
            }

            if( succeeded && WizardCompleted != null)
                WizardCompleted( this, null);
            else if( !succeeded && WizardFailed != null)
                WizardFailed( this, null);
        }

        /// <summary>
        /// Allows us to display the next step in the process when the previous step is completed
        /// </summary>
        /// <param name="step"></param>
        private void AddStep( WizardStep step)
        {
            Steps.Add( step);
            OnPropertyChanged( "Steps");
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }
}
