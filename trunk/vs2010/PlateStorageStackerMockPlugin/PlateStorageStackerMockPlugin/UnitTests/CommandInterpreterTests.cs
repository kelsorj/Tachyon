using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace BioNex.PlateStorageStackerMockPlugin.UnitTests
{
    public class CommandInterpreterTests
    {
        [Test]
        public void TestParsingTwoArguments()
        {
            CommandInterpreter ci  = new CommandInterpreter();
            string command = "command --arga=\"arg1\" --argb=\"arg2\"" + Command.Delimiter;
            ci.AddToQueue( command);
            Assert.AreEqual( 1, ci.NumberOfCommandsInQueue);
            Command next_command = ci.GetNextCommand();
            Assert.AreEqual( 0, ci.NumberOfCommandsInQueue);
            Assert.AreEqual( "command", next_command.Name);
            Assert.AreEqual( 2, next_command.NumberOfArguments);
            Assert.AreEqual( "arga", next_command.Args[0].Name);
            Assert.AreEqual( "arg1", next_command.Args[0].Value);
            Assert.AreEqual( "argb", next_command.Args[1].Name);
            Assert.AreEqual( "arg2", next_command.Args[1].Value);
        }

        [Test]
        public void TestParsingTwoCommands()
        {
            CommandInterpreter ci  = new CommandInterpreter();
            string command = "command1 command2 --arga=\"arg1\" --argb=\"arg2\"" + Command.Delimiter;
            ci.AddToQueue( command);
            Assert.AreEqual( 1, ci.NumberOfCommandsInQueue);
            Command next_command = ci.GetNextCommand();
            Assert.AreEqual( 0, ci.NumberOfCommandsInQueue);
            Assert.AreEqual( "command1", next_command.Name);
            Assert.AreEqual( 0, next_command.NumberOfArguments);
        }

        [Test]
        public void TestParsingNoArguments()
        {
            CommandInterpreter ci = new CommandInterpreter();
            // set up tests to loop over
            string[] test_commands = new string[] { "command1", " command1", "command1 ", "\tcommand1", "command1 \t" };
            foreach( string s in test_commands) {
                string command = s + Command.Delimiter;
                ci.AddToQueue( command);
                Assert.AreEqual( 1, ci.NumberOfCommandsInQueue);
                Command next_command = ci.GetNextCommand();
                Assert.AreEqual( 0, ci.NumberOfCommandsInQueue);
                Assert.AreEqual( "command1", next_command.Name);
            }
        }

        [Test]
        public void TestParsingArgumentsWithSpaces()
        {
            CommandInterpreter ci  = new CommandInterpreter();
            string command = "command --arga=\"this is a test\"" + Command.Delimiter;
            ci.AddToQueue( command);
            Assert.AreEqual( 1, ci.NumberOfCommandsInQueue);
            Command next_command = ci.GetNextCommand();
            Assert.AreEqual( 0, ci.NumberOfCommandsInQueue);
            Assert.AreEqual( "command", next_command.Name);
            Assert.AreEqual( 1, next_command.NumberOfArguments);
            Assert.AreEqual( "arga", next_command.Args[0].Name);
            Assert.AreEqual( "this is a test", next_command.Args[0].Value);
        }

        [Test]
        public void TestGetNextCommandFromEmptyQueue()
        {
            CommandInterpreter ci = new CommandInterpreter();
            Assert.AreEqual( 0, ci.NumberOfCommandsInQueue);
            Assert.Throws<InvalidOperationException>( () => ci.GetNextCommand());
        }
    }
}
