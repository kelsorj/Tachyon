using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BioNex.Shared.TechnosoftLibrary;

namespace TechnosoftUnitTests
{
    [TestClass]
    public class TechnosoftUnitTests
    {
        [TestMethod]
        public void TestConvertPositionToIU()
        {
            // linear axis
            double gear_ratio = 1.0 / 20.0; // 1 rot => 20mm
            Assert.AreEqual(20000, TechnosoftConnection.ConvertPositionToIU(20, 5000, 1 / gear_ratio));
            // theta axis with crazy ratio
            gear_ratio = (77.6 / 10.35) / 360;
            Assert.AreEqual(6824, TechnosoftConnection.ConvertPositionToIU(20, 4096, 1 / gear_ratio));
        }

        [TestMethod]
        public void TestConvertVelocityToIU()
        {
            // linear axis
            double gear_ratio = 1.0 / 75.0; // 1 rot => 75mm
            Assert.IsTrue(Math.Abs(266.67 - TechnosoftConnection.ConvertVelocityToIU(1000, 0.001, 5000, 1 / gear_ratio)) < 0.1);
            // theta axis with crazy ratio
            gear_ratio = (77.6 / 10.35) / 360;
            Assert.IsTrue(Math.Abs(0.3412 - TechnosoftConnection.ConvertVelocityToIU(1, 0.001, 4096, 1 / gear_ratio)) < 0.1);
        }

        [TestMethod]
        public void TestConvertAccelerationToIU()
        {
            // linear axis
            double gear_ratio = 1.0 / 75.0;
            Assert.IsTrue(Math.Abs(0.267 - TechnosoftConnection.ConvertAccelerationToIU(1000, 0.001, 5000, 1 / gear_ratio)) < 0.1);
            // theta axis with crazy ratio
            gear_ratio = (77.6 / 10.35) / 360;
            Assert.IsTrue(Math.Abs(0.0512 - TechnosoftConnection.ConvertAccelerationToIU(150, 0.001, 4096, 1 / gear_ratio)) < 0.1);
        }

        [TestMethod]
        public void TestConvertTimeToIU()
        {
            Assert.AreEqual(1, TechnosoftConnection.ConvertTimeToIU(1, 0.001));
            Assert.AreEqual(35, TechnosoftConnection.ConvertTimeToIU(35, 0.001));
            // true answer is 23.5, but should round to nearest integer
            Assert.AreEqual(24, TechnosoftConnection.ConvertTimeToIU(47, 0.002));
        }
    }
}
