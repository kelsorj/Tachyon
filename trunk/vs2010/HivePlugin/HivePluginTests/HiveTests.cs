using BioNex.Shared.Location;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HivePluginTests
{
    [TestClass]
    public class HiveTests
    {
        [TestMethod]
        public void TestNameToLocationParsing()
        {
            PlateLocation location = new PlateLocation( "Rack 3, Slot 5");
            // Assert.AreEqual( 3, location.RackNumber);
            // Assert.AreEqual( 5, location.SlotNumber);
            location = new PlateLocation( "rack 4, slot 6");
            // Assert.AreEqual( 4, location.RackNumber);
            // Assert.AreEqual( 6, location.SlotNumber);
        }
    }
}
