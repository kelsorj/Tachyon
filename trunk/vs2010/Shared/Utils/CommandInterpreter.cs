using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BioNex.Shared.CommandInterpreter
{
#if !HIG_INTEGRATION
    public class Command
    {
        public class Argument
        {
            public string Name { get; private set; }
            public string Value { get; private set; }

            public Argument( string name, string value)
            {
                Name = name;
                Value = value;
            }
        }

        public static string Delimiter = "\r";
        public string Name { get; private set; }
        public List<Argument> Args { get; private set; }
        public int NumberOfArguments
        {
            get { return Args.Count; }
        }

        public Command( string name, List<Argument> args)
        {
            Name = name;
            Args = args;
        }

        public override string ToString()
        {
            string cmd = Name;
            foreach( Argument arg in Args)
                cmd += String.Format( " --{0}=\"{1}\"", arg.Name, arg.Value);
            return cmd;
        }
    }

    public class CommandError
    {
        private readonly Command _cmd;

        public CommandError( Command cmd)
        {
            _cmd = cmd;
        }

        public override string ToString()
        {
            string cmd = _cmd.Name;
            foreach( Command.Argument arg in _cmd.Args)
                cmd += String.Format( " --{0}=\"{1}\"", arg.Name, arg.Value);
            return cmd;
        }
    }

    public class CommandInterpreter
    {
        /// <summary>
        /// this is the queue of commands that have already been parsed, but
        /// still need to be processed
        /// </summary>
        private readonly Queue<Command> _command_queue;
        /// <summary>
        /// this is basically just the input buffer from the client socket,
        /// and gets parsed into commands whenever AddToQueue gets called
        /// </summary>
        private string _command_buffer;

        public CommandInterpreter()
        {
            _command_queue = new Queue<Command>();
        }

        /// <summary>
        /// how many commands in the queue still need to be processed
        /// </summary>
        public int NumberOfCommandsInQueue
        {
            get {
                return _command_queue.Count;
            }
        }

        public Command GetNextCommand()
        {
            return _command_queue.Dequeue();
        }

        /// <summary>
        /// Adds command data to the incoming buffer, and if a command is completed
        /// (when \r\n is received), the entire command string is parsed and added
        /// to the command queue.
        /// </summary>
        /// <remarks>
        /// Only one command can be processed at a time.  Therefore, it is not advised
        /// to send a command line 'command1 --arg1="arg" command2 ==arg2="arg"\r\n'.
        /// In this case, only the last command will get processed properly.
        /// </remarks>
        /// <exception cref="InvalidOperationException" />
        /// <param name="command"></param>
        public void AddToQueue( string command)
        {
            _command_buffer += command;
            // now look at the queue and see if there are any commands that
            // get removed.  A complete command ends with \r\n.
            int delim_pos = _command_buffer.IndexOf( Command.Delimiter);
            if( delim_pos == -1)
                return;
            // take all of the chars up to the delimiter, and this should be our command string
            string command_and_args = _command_buffer.Substring( 0, delim_pos + Command.Delimiter.Length);
            // strip off the delimiter
            command_and_args = command_and_args.Substring( 0, delim_pos);

            // make sure we remove all of those characters from the buffer!!!
            _command_buffer = _command_buffer.Substring( delim_pos + Command.Delimiter.Length);

            // now parse command_and_args for the command and arguments using regex
            Regex re = new Regex( "(\\w+)(\\s*(--(\\w+)\\s*=\\s*\"([\\w\\s]+)\"\\s*)*)");
            Match m = re.Match( command_and_args);
            GroupCollection groups = m.Groups;
            /*
            for( int i=0; i<groups.Count; i++)
                Debug.WriteLine( "group " + i.ToString() + ": " + groups[i].ToString());
             */
            string command_name = groups[1].ToString();
            string all_args = groups[2].ToString();

            // now parse all_args to get the individual arguments
            re = new Regex( "\\s*--(\\w+)\\s*=\\s*\"([\\w\\s]+)\"\\s*");
            MatchCollection matches = re.Matches( all_args);
            List<Command.Argument> command_args = new List<Command.Argument>();
            for( int i=0; i<matches.Count; i++) {
                /*
                Debug.WriteLine( "group " + i.ToString() + ": " + matches[i].ToString());
                foreach( Group g in matches[i].Groups)
                    Debug.WriteLine( "--group: " + g.ToString());
                 */
                // be reasonably sure we have a key value pair
                // note that there are THREE groups because the first one includes everything
                Debug.Assert( matches[i].Groups.Count == 3, "There should only be two groups in the matched string");
                Command.Argument arg = new Command.Argument( matches[i].Groups[1].ToString(), matches[i].Groups[2].ToString());
                command_args.Add( arg);
            }

            // add the command and all arguments to the queue
            _command_queue.Enqueue( new Command( command_name, command_args));
        }
    }
#endif
}
