using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BioNex.HiveIntegration;

namespace HiveIntegrationUnitTests
{
    [TestClass]
    public class HiveStatusTests
    {
        [TestMethod]
        public void TestHiveStatusToInt()
        {
            BioNex.HiveIntegration.HiveStatus hs = new BioNex.HiveIntegration.HiveStatus();
            Assert.AreEqual( 0, hs.ToInt());
            hs.Full = true;
            Assert.AreEqual( 1, hs.ToInt());
            hs.Empty = true;
            Assert.AreEqual( 3, hs.ToInt());
            hs.Busy = true;
            Assert.AreEqual( 7, hs.ToInt());
            hs.MovingPlate = true;
            Assert.AreEqual( 15, hs.ToInt());
            hs.LoadingPlate = true;
            Assert.AreEqual( 31, hs.ToInt());
            hs.UnloadingPlate = true;
            Assert.AreEqual( 63, hs.ToInt());
            hs.ScanningInventory = true;
            Assert.AreEqual( 127, hs.ToInt());
        }

        [TestMethod]
        public void TestIntToHiveStatus()
        {
            int status = 0;
            BioNex.HiveIntegration.HiveStatus hs = new BioNex.HiveIntegration.HiveStatus();
            Assert.AreEqual( hs, HiveStatus.FromInt( status));
        }
    }
}
