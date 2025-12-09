using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.BumblebeeAlphaGUI;
using NUnit.Framework;
using BioNex.Shared.Teachpoints;

namespace BioNex.BumblebeeAlphaGUI
{
    public class BumblebeeTeachpointsTests
    {
        [Test]
        public void TestLoadTestTeachpoints()
        {
            Teachpoints teachpoint_file = new Teachpoints();
            teachpoint_file.LoadTeachpointFile( @"..\..\teachpoints.xml", null);
        }

        [Test]
        public void TestAddStageTeachpoints()
        {
            Teachpoints teachpoints = new Teachpoints();
            teachpoints.AddUpperLeftStageTeachpoint( 1, 2, 1.0, 2.0, 3.0, 4.0);
            teachpoints.AddLowerRightStageTeachpoint( 1, 2, 2.0, 3.0, 4.0, 5.0);
            teachpoints.AddLowerRightStageTeachpoint( 1, 1, 3.0, 4.0, 5.0, 6.0);
            teachpoints.AddUpperLeftStageTeachpoint( 1, 1, 4.0, 5.0, 6.0, 7.0);
            StageTeachpoint stage_1_teachpoint = teachpoints.GetStageTeachpoint( 1, 1);
            StageTeachpoint stage_2_teachpoint = teachpoints.GetStageTeachpoint( 1, 2);
            // stage 1
            Assert.AreEqual( 4.0, stage_1_teachpoint.UpperLeft["x"]);
            Assert.AreEqual( 5.0, stage_1_teachpoint.UpperLeft["z"]);
            Assert.AreEqual( 6.0, stage_1_teachpoint.UpperLeft["y"]);
            Assert.AreEqual( 7.0, stage_1_teachpoint.UpperLeft["r"]);
            Assert.AreEqual( 3.0, stage_1_teachpoint.LowerRight["x"]);
            Assert.AreEqual( 4.0, stage_1_teachpoint.LowerRight["z"]);
            Assert.AreEqual( 5.0, stage_1_teachpoint.LowerRight["y"]);
            Assert.AreEqual( 6.0, stage_1_teachpoint.LowerRight["r"]);
            // stage 2
            Assert.AreEqual( 1.0, stage_2_teachpoint.UpperLeft["x"]);
            Assert.AreEqual( 2.0, stage_2_teachpoint.UpperLeft["z"]);
            Assert.AreEqual( 3.0, stage_2_teachpoint.UpperLeft["y"]);
            Assert.AreEqual( 4.0, stage_2_teachpoint.UpperLeft["r"]);
            Assert.AreEqual( 2.0, stage_2_teachpoint.LowerRight["x"]);
            Assert.AreEqual( 3.0, stage_2_teachpoint.LowerRight["z"]);
            Assert.AreEqual( 4.0, stage_2_teachpoint.LowerRight["y"]);
            Assert.AreEqual( 5.0, stage_2_teachpoint.LowerRight["r"]);
        }

        [Test]
        public void TestOverwriteStageTeachpoint()
        {
            Teachpoints teachpoints = new Teachpoints();
            // set data
            teachpoints.AddUpperLeftStageTeachpoint( 1, 2, 1.0, 2.0, 3.0, 4.0);
            teachpoints.AddLowerRightStageTeachpoint( 1, 2, 2.0, 3.0, 4.0, 5.0);
            // overwrite data
            teachpoints.AddUpperLeftStageTeachpoint( 1, 2, 4.0, 5.0, 6.0, 7.0);
            teachpoints.AddLowerRightStageTeachpoint( 1, 2, 3.0, 4.0, 5.0, 6.0);
            // check data
            StageTeachpoint stage_2_teachpoint = teachpoints.GetStageTeachpoint( 1, 2);
            Assert.AreEqual( 4.0, stage_2_teachpoint.UpperLeft["x"]);
            Assert.AreEqual( 5.0, stage_2_teachpoint.UpperLeft["z"]);
            Assert.AreEqual( 6.0, stage_2_teachpoint.UpperLeft["y"]);
            Assert.AreEqual( 7.0, stage_2_teachpoint.UpperLeft["r"]);
            Assert.AreEqual( 3.0, stage_2_teachpoint.LowerRight["x"]);
            Assert.AreEqual( 4.0, stage_2_teachpoint.LowerRight["z"]);
            Assert.AreEqual( 5.0, stage_2_teachpoint.LowerRight["y"]);
            Assert.AreEqual( 6.0, stage_2_teachpoint.LowerRight["r"]);
        }

        // this class is defined merely to enable a comparison of two objects BY VALUE
        // see http://stackoverflow.com/questions/318210/compare-equality-between-two-objects-in-nunit
        private class TeachpointComparer : IEqualityComparer<Teachpoint>
        {
            public bool Equals( Teachpoint x, Teachpoint y)
            {
                for( int i=0; i<x.NumTeachpointItems; i++) {
                    if( x.TeachpointItems[i].AxisName != y.TeachpointItems[i].AxisName ||
                        x.TeachpointItems[i].Position != y.TeachpointItems[i].Position)
                        return false;
                }
                return true;
            }

            public int GetHashCode( Teachpoint obj)
            {
                throw new NotImplementedException();
            }
        }
    
        [Test]
        public void TestSaveStageTeachpoints()
        {
            Teachpoints teachpoints = new Teachpoints();
            teachpoints.AddUpperLeftStageTeachpoint( 1, 2, 1.0, 2.0, 3.0, 4.0);
            teachpoints.AddLowerRightStageTeachpoint( 1, 2, 2.0, 3.0, 4.0, 5.0);
            teachpoints.AddLowerRightStageTeachpoint( 1, 1, 3.0, 4.0, 5.0, 6.0);
            teachpoints.AddUpperLeftStageTeachpoint( 1, 1, 4.0, 5.0, 6.0, 7.0);
            teachpoints.SaveTeachpointFile( "test_teachpoints.xml");
            Teachpoints loaded = new Teachpoints();
            loaded.LoadTeachpointFile( "test_teachpoints.xml", null);
            Assert.IsTrue( new TeachpointComparer().Equals( teachpoints.GetStageTeachpoint( 1, 1), loaded.GetStageTeachpoint( 1, 1)));
            Assert.IsTrue( new TeachpointComparer().Equals( teachpoints.GetStageTeachpoint( 1, 2), loaded.GetStageTeachpoint( 1, 2)));
        }

        [Test]
        public void TestAddWashTeachpoint()
        {
            Teachpoints teachpoints = new Teachpoints();
            teachpoints.AddWashTeachpoint( 1, 1.0, 2.0);
            teachpoints.AddWashTeachpoint( 2, 2.0, 3.0);
            Teachpoint wt1 = teachpoints.GetWashTeachpoint( 1);
            Teachpoint wt2 = teachpoints.GetWashTeachpoint( 2);
            Assert.AreEqual( 1.0, wt1["x"]);
            Assert.AreEqual( 2.0, wt1["z"]);
            Assert.AreEqual( 2.0, wt2["x"]);
            Assert.AreEqual( 3.0, wt2["z"]);
        }    

        [Test]
        public void TestSaveWashTeachpoints()
        {
            Teachpoints teachpoints = new Teachpoints();
            teachpoints.AddWashTeachpoint( 1, 1.0, 2.0);
            teachpoints.AddWashTeachpoint( 2, 2.0, 3.0);
            teachpoints.SaveTeachpointFile( "test_teachpoints.xml");
            Teachpoints loaded = new Teachpoints();
            loaded.LoadTeachpointFile( "test_teachpoints.xml", null);
            Assert.IsTrue( new TeachpointComparer().Equals( teachpoints.GetWashTeachpoint( 1), loaded.GetWashTeachpoint( 1)));
            Assert.IsTrue( new TeachpointComparer().Equals( teachpoints.GetWashTeachpoint( 2), loaded.GetWashTeachpoint( 2)));
        }

        [Test]
        public void TestSaveRobotTeachpoints()
        {
            Teachpoints teachpoints = new Teachpoints();
            teachpoints.AddRobotTeachpoint( 1, 1.23, 2.34);
            teachpoints.AddRobotTeachpoint( 3, 3.45, 4.56);
            teachpoints.SaveTeachpointFile( "test_teachpoints.xml");
            Teachpoints loaded = new Teachpoints();
            loaded.LoadTeachpointFile( "test_teachpoints.xml", null);
            Assert.IsTrue( new TeachpointComparer().Equals( teachpoints.GetRobotTeachpoint( 1), loaded.GetRobotTeachpoint( 1)));
            Assert.IsTrue( new TeachpointComparer().Equals( teachpoints.GetRobotTeachpoint( 3), loaded.GetRobotTeachpoint( 3)));
        }
    }
}
