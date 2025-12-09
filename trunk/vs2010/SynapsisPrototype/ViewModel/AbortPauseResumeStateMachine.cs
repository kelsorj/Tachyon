using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stateless;
using System.ComponentModel;
using System.Windows;
using System.ComponentModel.Composition;
using System.Threading;

namespace BioNex.SynapsisPrototype.ViewModel
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(AbortPauseResumeStateMachine))]
    public class AbortPauseResumeStateMachine : INotifyPropertyChanged
    {
        private StateMachine<State,Trigger> SM { get; set; }

        public event EventHandler GuiResumeEvent;
        public event EventHandler GuiPauseEvent;
        public event EventHandler GuiAbortEvent;

        public enum State
        {
            Uninitialized,
            Idle,
            Running,
            Paused,
            SoftwarePause,
            HardwarePause,
            Aborting,
            Resuming,
        }

        private enum Trigger
        {
            Initialized,
            Start,
            UserClickedPauseResume,
            UserClickedAbort,
            InterlockTriggered,
            InterlockReset,
            Done,
        }

        public AbortPauseResumeStateMachine()
        {
            SM = new StateMachine<State,Trigger>( State.Uninitialized);
            PauseResumeButtonEnabled = false;

            SM.Configure(State.Uninitialized)
                .Permit(Trigger.Initialized, State.Idle);
            SM.Configure(State.Idle)
                .Ignore(Trigger.Done)
                .Ignore(Trigger.InterlockTriggered)
                .Ignore(Trigger.InterlockReset)
                .Ignore(Trigger.UserClickedPauseResume)
                .Ignore(Trigger.UserClickedAbort) // to prevent hanging on shutdown w/o customer GUI loaded
                .Permit(Trigger.Start, State.Running)
                .OnEntry( () => { PauseResumeButtonText = "Pause"; PauseResumeButtonEnabled = false;});
            SM.Configure(State.Running)
                .Permit(Trigger.UserClickedPauseResume, State.SoftwarePause)
                .Permit(Trigger.Done, State.Idle)
                .Permit(Trigger.UserClickedAbort, State.Aborting)
                .Permit(Trigger.InterlockTriggered, State.HardwarePause)
                .OnEntry( () => { PauseResumeButtonText = "Pause"; PauseResumeButtonEnabled = true;});
            SM.Configure(State.SoftwarePause)
                .SubstateOf(State.Paused)
                .Ignore(Trigger.InterlockTriggered)
                .Ignore(Trigger.InterlockReset)
                .Permit(Trigger.UserClickedPauseResume, State.Resuming)
                .OnEntry( () => { PauseResumeButtonEnabled = true; });
            SM.Configure(State.HardwarePause)
                .SubstateOf(State.Paused)
                .Ignore(Trigger.UserClickedPauseResume)
                .Permit(Trigger.InterlockReset, State.SoftwarePause)
                // hardware pause disables the button to prevent someone from reactivating the machine when someone is in the workspace
                .OnEntry( () => { PauseResumeButtonEnabled = false; });
            SM.Configure(State.Resuming)
                .Permit(Trigger.Done, State.Running)
                .OnEntry( Resuming);
            SM.Configure(State.Paused)
                .Permit(Trigger.UserClickedAbort, State.Aborting)
                .Permit(Trigger.Done, State.Idle)
                .OnEntry( Pause);
            SM.Configure(State.Aborting)
                .Permit(Trigger.Done, State.Idle)
                .OnEntry( Aborting);

            SM.Fire( Trigger.Initialized);
        }

        private string pause_resume_button_text_;
        public string PauseResumeButtonText
        {
            get { return pause_resume_button_text_; }
            set {
                pause_resume_button_text_ = value;
                OnPropertyChanged( "PauseResumeButtonText");
            }
        }

        public bool PauseResumeButtonEnabled { get; private set; }
        
        public bool Idle { get { return SM.IsInState(State.Idle); }}
        public bool Running { get { return SM.IsInState(State.Running); }}
        public bool Paused { get { return SM.IsInState(State.Paused); }}

        //********** called by GUI **********
        public void Start()
        {
            SM.Fire( Trigger.Start);
        }

        public void Abort()
        {
            SM.Fire( Trigger.UserClickedAbort);
        }

        public void PauseResume()
        {
            SM.Fire( Trigger.UserClickedPauseResume);
        }

        public void Done()
        {
            SM.Fire( Trigger.Done);
        }

        public void InterlockTriggered()
        {
            SM.Fire( Trigger.InterlockTriggered);
        }

        public void InterlockReset()
        {
            SM.Fire( Trigger.InterlockReset);
        }
        //***********************************

        /// <summary>
        /// pauses the protocol and changes the button text accordingly
        /// </summary>
        private void Pause()
        {
            PauseResumeButtonText = "Resume";
            if( GuiPauseEvent != null)
                GuiPauseEvent( this, new EventArgs());
        }

        /// <summary>
        /// aborts the protocol and changes the pause/resume button text accordingly
        /// </summary>
        private void Aborting()
        {
            PauseResumeButtonText = "Pause";
            PauseResumeButtonEnabled = false;
            if( GuiAbortEvent != null)
                GuiAbortEvent( this, new EventArgs());

            SM.Fire( Trigger.Done);
        }

        private void Resuming()
        {
            // DKM 2010-10-05 not sure if this is the right place to put this, but we need to sleep when recovering because
            //                it takes time for the power supplies to re-enable
            Thread.Sleep( 1000);
            if( GuiResumeEvent != null)
                GuiResumeEvent( this, new EventArgs());
            SM.Fire( Trigger.Done);
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
