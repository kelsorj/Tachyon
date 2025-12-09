using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.BumblebeeAlphaGUI;
using BioNex.Shared.TechnosoftLibrary;
using NUnit.Framework;

namespace AlphaHardwareUnitTests
{
    public class AlphaHardwareTests
    {
        [Test]
        public void TestLoadHardwareConfigurationOK()
        {
            string path = @"..\..\sample_hardware_configuration.xml";
            AlphaHardware hw = new AlphaHardware( null);
            string motor_settings_path = @"..\..\sample_motor_settings.xml";
            TechnosoftConnection ts = new TechnosoftConnection();
            ts.LoadConfiguration( motor_settings_path, "");
            hw.LoadConfiguration( path, ts);
            Channel channel = hw.GetChannel( 1);
            Assert.AreEqual( 11, channel.XId);
            Assert.AreEqual( 13, channel.ZId);
            Assert.AreEqual( 14, channel.WId);
            Stage stage = hw.GetStage( 1);
            Assert.AreEqual( 12, stage.YId);
            Assert.AreEqual( 15, stage.RId);
            // enumerate channels and stages
            Assert.AreEqual( 1, hw.GetChannel(1).GetID());
            Assert.AreEqual( 1, hw.GetStage(1).GetID());
        }

        [Test, Ignore]
        public void TestLoadHardwareConfigurationBad()
        {
            string path = @"..\..\sample_hardware_configuration.xml";
            AlphaHardware hw = new AlphaHardware( null);
            string motor_settings_path = @"..\..\sample_empty_motor_settings.xml";
            TechnosoftConnection ts = new TechnosoftConnection();
            ts.LoadConfiguration( motor_settings_path, "");
            // create a local map here so we can test the LoadConfiguration function
            Dictionary<byte,IAxis> axes = new Dictionary<byte,IAxis>();
            try {
                hw.LoadConfiguration( path, ts);
                Assert.Fail( "Hardware configuration loading should have failed because of missing axes, but it didn't");
            } catch( ApplicationException) {
                // passed!
            }
        }    
    }
}
