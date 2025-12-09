using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GalaSoft.MvvmLight.Messaging;
using System.Diagnostics;

namespace BioNex.PlateStorageStackerMockPlugin
{
    public class Controller
    {
        IWorksStackerListener _listener = new IWorksStackerListener();

        public void Initialize()
        {
            // initialize the listener that looks for commands coming over the network from an IWorks plugin
            _listener.StartListening( System.Net.IPAddress.Parse( "127.0.0.1"), 7890);
            // register for commands via the static Messenger object
            Messenger.Default.Register<Command>( this, HandleCommand);
            Debug.WriteLine( "Listening...");
        }

        public void Close()
        {
            _listener.StopListening();
            Messenger.Default.Unregister<Command>( this, HandleCommand);
            Debug.WriteLine( "No longer listening");
        }

        private void HandleCommand( Command cmd)
        {
            Debug.WriteLine( "received the command '" + cmd.ToString() + "'");
            switch( cmd.Name) {
                case "upstack":
                    break;
                case "downstack":
                    break;
                default:
                    // send back an error
                    Messenger.Default.Send<CommandError>( new CommandError( cmd));
                    break;
            }
        }
    }
}
