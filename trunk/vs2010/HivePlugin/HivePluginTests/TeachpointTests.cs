using BioNex.Hive.Hardware;
using BioNex.Shared.Teachpoints;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HivePluginTests
{
    [TestClass]
    public class TeachpointTests
    {
        [TestMethod]
        public void TestLoadTestTeachpoints()
        {
            GenericTeachpointCollection< HiveTeachpoint> teachpoints = new GenericTeachpointCollection< HiveTeachpoint>();
            // teachpoints.LoadTeachpointFile( @"..\..\..\..\..\HivePrototypePlugin\HivePluginTests\teachpoints.xml", null);
            Assert.AreEqual( 0, teachpoints.GetTeachpoint( "BB stage 1").X);
            Assert.AreEqual( 0, teachpoints.GetTeachpoint( "BB stage 1").Y);
            Assert.AreEqual( 0, teachpoints.GetTeachpoint( "BB stage 1").Z);
            Assert.AreEqual( 1, teachpoints.GetTeachpoint( "BB stage 2").X);
            Assert.AreEqual( 1, teachpoints.GetTeachpoint( "BB stage 2").Y);
            Assert.AreEqual( 1, teachpoints.GetTeachpoint( "BB stage 2").Z);
            Assert.AreEqual( 2, teachpoints.GetTeachpoint( "BB stage 3").X);
            Assert.AreEqual( 2, teachpoints.GetTeachpoint( "BB stage 3").Y);
            Assert.AreEqual( 2, teachpoints.GetTeachpoint( "BB stage 3").Z);
        }

        [TestMethod]
        public void TestAddTeachpoints()
        {
            GenericTeachpointCollection< HiveTeachpoint> teachpoints = new GenericTeachpointCollection< HiveTeachpoint>();
            teachpoints.SetTeachpoint( new HiveTeachpoint( "test1", 0, 1, 2, 10, HiveTeachpoint.TeachpointOrientation.Portrait));
            teachpoints.SetTeachpoint( new HiveTeachpoint( "test2", 3, 4, 5, 10, HiveTeachpoint.TeachpointOrientation.Portrait));
            teachpoints.SetTeachpoint( new HiveTeachpoint( "test3", 6, 7, 8, 10, HiveTeachpoint.TeachpointOrientation.Portrait));
            Assert.AreEqual( 0, teachpoints.GetTeachpoint( "test1").X);
            Assert.AreEqual( 1, teachpoints.GetTeachpoint( "test1").Y);
            Assert.AreEqual( 2, teachpoints.GetTeachpoint( "test1").Z);
            Assert.AreEqual( 10, teachpoints.GetTeachpoint( "test1").ApproachHeight);
            Assert.AreEqual( 3, teachpoints.GetTeachpoint("test2").X);
            Assert.AreEqual( 4, teachpoints.GetTeachpoint( "test2").Y);
            Assert.AreEqual( 5, teachpoints.GetTeachpoint( "test2").Z);
            Assert.AreEqual( 10, teachpoints.GetTeachpoint( "test2").ApproachHeight);
            Assert.AreEqual( 6, teachpoints.GetTeachpoint("test3").X);
            Assert.AreEqual( 7, teachpoints.GetTeachpoint( "test3").Y);
            Assert.AreEqual( 8, teachpoints.GetTeachpoint( "test3").Z);
            Assert.AreEqual( 10, teachpoints.GetTeachpoint( "test3").ApproachHeight);
        }

        [TestMethod]
        public void TestOverwriteTeachpoint()
        {
            GenericTeachpointCollection< HiveTeachpoint> teachpoints = new GenericTeachpointCollection< HiveTeachpoint>();
            // set data
            teachpoints.SetTeachpoint( new HiveTeachpoint( "test1", 0, 1, 2, 10, HiveTeachpoint.TeachpointOrientation.Portrait));
            // overwrite data
            teachpoints.SetTeachpoint( new HiveTeachpoint( "test1", 3, 4, 5, 10, HiveTeachpoint.TeachpointOrientation.Portrait));
            // check data
            HiveTeachpoint test = teachpoints.GetTeachpoint( "test1");
            Assert.AreEqual( 3, test.X);
            Assert.AreEqual( 4, test.Y);
            Assert.AreEqual( 5, test.Z);
            Assert.AreEqual( 10, test.ApproachHeight);
            Assert.AreEqual( 1, teachpoints.GetTeachpointNames().Count);
        }

        [TestMethod]
        public void TestSaveTeachpoints()
        {
            GenericTeachpointCollection< HiveTeachpoint> teachpoints = new GenericTeachpointCollection< HiveTeachpoint>();
            teachpoints.SetTeachpoint( new HiveTeachpoint( "test1", 0, 1, 2, -1, HiveTeachpoint.TeachpointOrientation.Portrait));
            teachpoints.SetTeachpoint( new HiveTeachpoint( "test2", 3, 4, 5, -2, HiveTeachpoint.TeachpointOrientation.Portrait));
            // teachpoints.SaveTeachpointFile( "test_teachpoints.xml");
            // GenericTeachpointCollection< HiveTeachpoint> loaded = new GenericTeachpointCollection< HiveTeachpoint>();
            // loaded.LoadTeachpointFile( "test_teachpoints.xml", null);
            // Assert.IsTrue( new TeachpointComparer().Equals( teachpoints.GetTeachpoint( "test1"), loaded.GetTeachpoint( "test1")));
            // Assert.IsTrue( new TeachpointComparer().Equals( teachpoints.GetTeachpoint( "test2"), loaded.GetTeachpoint( "test2")));
        }    
    }
}
