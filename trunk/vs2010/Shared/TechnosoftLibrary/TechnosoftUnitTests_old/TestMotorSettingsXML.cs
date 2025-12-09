using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using BioNex.Shared.TechnosoftLibrary;
using NUnit.Framework;

namespace BioNex.Shared.TechnosoftLibrary
{
    public partial class UnitTests
    {
        [Test]
        public void TestReadMissingMotorSettings()
        {
            string path = @"..\..\motor_settings.xml";
            try {
                Dictionary<byte,MotorSettings> settings = MotorSettings.LoadMotorSettings( path);
            } catch( InvalidMotorSettingsException) {
                return;
            }
            Assert.Fail( "Should have caught missing motor setting parameter, but didn't");
        }

        [Test]
        public void TestReadMotorSettings()
        {
            string path = @"..\..\good_motor_settings.xml";
            Dictionary<byte,MotorSettings> settings = MotorSettings.LoadMotorSettings( path);
            // NOTE: these values that we are testing are from the XML file directly.
            //       NO conversion from engineering units to IU was made because it wasn't
            //       loaded via the TechnosoftLibrary
            Assert.AreEqual( 2, settings.Count);
            // check X
            MotorSettings ms = settings[11];
            Assert.AreEqual( "X1", ms.AxisName);
            Assert.AreEqual( 1, ms.Velocity);
            Assert.AreEqual( 2, ms.Acceleration);
            Assert.AreEqual( 3, ms.Jerk);
            Assert.AreEqual( 4096, ms.EncoderLines);
            Assert.AreEqual( 4, ms.GearRatio);
            Assert.AreEqual( 5, ms.MinLimit);
            Assert.AreEqual( 500, ms.MaxLimit);
            Assert.AreEqual( 10, ms.MoveDoneWindow);
            Assert.AreEqual( 100, ms.SettlingTimeMS);
            Assert.AreEqual( false, ms.FlipAxisDirection);
            // check Z axis flip
            ms = settings[13];
            Assert.AreEqual( true, ms.FlipAxisDirection);
        }

        [Test]
        public void TestTrapezoidalMoveTimeCalculation()
        {
            MotorSettings settings = new MotorSettings( "temp", 2000, 8000, 4, 5000, 75, 0, 530, 0.08625, 50, 0.01);
            // calculate for a 100mm move
            int move_time_ms = settings.CalculateTrapezoidalMoveTime( 100);
            Assert.IsTrue( Math.Abs( 10 - move_time_ms) < 100);
        }
    }
}
