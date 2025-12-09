using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using BioNex.SynapsisPrototype;
using BioNex.Shared.DeviceInterfaces;

namespace SynapsisTests
{
    [TestFixture]
    public class SynapsisTests
    {
        private PlateLocationTracker _tracker { get; set; }
        private AccessibleDeviceInterface _device { get; set; }
        const string _barcode = "record test";
        const string _location_name = "record test location";

        [SetUp]
        public void Init()
        {
            _tracker = new PlateLocationTracker();
            _device = new BioNex.BumblebeePlugin.Bumblebee();
        }

        [Test]
        public void TestPlateLocationTracker()
        {
            // put a plate at a location
            _tracker.RecordPlateLocation( _barcode, _device, _location_name);
            // get the plate back
            Tuple<DeviceInterface, string> location = _tracker.GetPlateLocation( _barcode);
            Assert.AreEqual( _device, location.Item1);
            Assert.AreEqual( _location_name, location.Item2);
            // check the barcode at the location
            string checked_barcode = _tracker.GetPlateAtLocation( _device, _location_name);
            Assert.AreEqual( _barcode, checked_barcode);
            // clear the barcode and confirm error behavior
            _tracker.ClearPlateLocation( _barcode);
            Assert.Throws<KeyNotFoundException>( GetPlateLocationDelegate);
        }

        private void GetPlateLocationDelegate()
        {
            _tracker.GetPlateLocation( _barcode);
        }
    }
}
