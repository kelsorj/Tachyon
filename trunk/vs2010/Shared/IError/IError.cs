using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BioNex.Shared.IError
{
    public delegate void ErrorEventHandler(object sender, ErrorData e);
    public interface IError
    {
        event ErrorEventHandler ErrorEvent; // To notify listeners that an error has ocurred, to be called by implementation from AddError 
        IEnumerable<ErrorData> PendingErrors { get; }
        /// <summary>
        /// Typically, the error handling function in the state machines will block and wait until the user has clicked
        /// a button.  This is okay because state machines as such should be running inside of a thread.  However, in one
        /// case, the state machine is executed and could encounter an error during device initialization (e.g. V11
        /// device drivers).  We have the LoadPluginsErrorInterface for this, and it should not block and wait for
        /// user input.  It should cache the errors, and once the GUI is completely loaded, add them to the main GUI's
        /// error panel.
        /// </summary>
        bool WaitForUserToHandleError { get; }
        void AddError( ErrorData error);
        void Clear();
    }

    public class LoadPluginsErrorInterface : IError
    {
        private readonly List<ErrorData> _pending_errors;

        public LoadPluginsErrorInterface()
        {
            _pending_errors = new List<ErrorData>();
        }

        public void AddError(ErrorData error)
        {
            _pending_errors.Add( error);
        }

        public event ErrorEventHandler ErrorEvent { add {} remove {} }

        public IEnumerable<ErrorData> PendingErrors
        {
            get { return _pending_errors; }
        }

        public bool WaitForUserToHandleError { get { return false; } }
        public void Clear() { _pending_errors.Clear(); }
    }

    public class ErrorData
    {
        public string ErrorMessage { get; private set; }
        public string Details { get; private set; }
        public DateTime TimeStamp { get; private set; }

        public string TriggeredEvent
        {
            get {
                // iterate over the events and return text for whichever one got signalled
                foreach( KeyValuePair<string,ManualResetEvent> kvp in Events) {
                    ManualResetEvent manual_event = kvp.Value;
                    if( manual_event.WaitOne( 0))
                        return kvp.Key;
                }
                return "";
            }
            private set {}
        }
        
        public Dictionary<string,ManualResetEvent> Events { get; private set; }
        public ManualResetEvent[] EventArray
        {
            get {
                return Events.Values.ToArray<ManualResetEvent>();
            }
            private set {}
        }

        public ErrorData( string description, IEnumerable<string> handlers, string details="")
        {
            TimeStamp = DateTime.Now;
            Events = new Dictionary<string, ManualResetEvent>();
            ErrorMessage = description;
            Details = details;
            foreach( string s in handlers)
                AddEvent( s);
        }

        public void AddEvent( string caption)
        {
            ManualResetEvent new_event = new ManualResetEvent( false);
            if( Events.ContainsKey( caption))
                Events[caption] = new_event;
            else
                Events.Add( caption, new_event);
        }
    }

}
