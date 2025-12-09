using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace BioNex.BNX1536Plugin
{
    public class Commands
    {
        public const string Status = "#?";
        public const string Start = "#S";
        /// <summary>
        /// this command is messed up because you send it once to stop, then
        /// you send the SAME command again to clear the e-stop.
        /// </summary>
        public const string ToggleStop = "#R";

        /// <summary>
        /// returns the command string that gets sent to the device for selecting a program
        /// </summary>
        /// <exception cref="InvalidArgumentException"></exception>
        /// <param name="program_number">valid range is 1 - 99</param>
        /// <returns>the "select program" command string</returns>
        public static string GetProgramNumberCommandString( int program_number)
        {
            if( program_number < 1 || program_number > 99)
                throw new BioNex.Exceptions.InvalidArgumentException( "program number", program_number.ToString(), "1", "99");
            return String.Format( "#P{0:00}", program_number);
        }

        /// <summary>
        /// returns the command string that gets sent to the device for selecting a service program
        /// </summary>
        /// <exception cref="InvalidArgumentException"></exception>
        /// <param name="program_number">valid range is 1 - 4</param>
        /// <returns>the "select service program" command string</returns>
        public static string GetServiceProgramCommandString( int program_number)
        {
            if( program_number < 1 || program_number > 4)
                throw new BioNex.Exceptions.InvalidArgumentException( "service program number", program_number.ToString(), "1", "4");
            return String.Format( "#PA{0}", program_number);
        }
    }

    /*
    [TestFixture]
    public class UtilsTests
    {
        [Test]
        public void TestProgramNumberCommand()
        {
            string test_01 = Commands.GetProgramNumberCommandString( 1);
            Assert.AreEqual( "#P01", test_01);
            string test_99 = Commands.GetProgramNumberCommandString( 99);
            Assert.AreEqual( "#P99", test_99);
        }

        [Test, Ignore]
        public void TestProgramNumberCommandExceptions()
        {
            // doesn't seem to catch exception properly???
            Assert.Throws<BioNex.Exceptions.InvalidArgumentException>( () => Commands.GetProgramNumberCommandString( 0));
            Assert.Throws<BioNex.Exceptions.InvalidArgumentException>( () => Commands.GetProgramNumberCommandString( 99));
        }

        [Test]
        public void TestServiceProgramNumberCommand()
        {
            string test_1 = Commands.GetServiceProgramCommandString( 1);
            Assert.AreEqual( "#PA1", test_1);
            string test_4 = Commands.GetServiceProgramCommandString( 4);
            Assert.AreEqual( "#PA4", test_4);
        }

        [Test, Ignore]
        public void TestServiceProgramNumberCommandExceptions()
        {
            Assert.Throws<BioNex.Exceptions.InvalidArgumentException>( () => Commands.GetServiceProgramCommandString( 0));
            Assert.Throws<BioNex.Exceptions.InvalidArgumentException>( () => Commands.GetServiceProgramCommandString( 5));
        }
    }
     */
}
