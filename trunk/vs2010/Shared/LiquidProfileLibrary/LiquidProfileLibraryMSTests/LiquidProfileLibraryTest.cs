using System;
using System.Collections.Generic;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.LiquidProfileLibrary;
using BioNex.Shared.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiquidProfileLibraryMSTests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class LiquidProfileLibraryTest
    {
        public LiquidProfileLibraryTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestLiquidProfile()
        {
        }

        [TestMethod]
        public void TestLiquidCalibrationDatum()
        {
        }

        [TestMethod]
        public void TestLiquidProfileLibraryHighLevel()
        {
            const string connection_string = "test_lp_lib.s3db";
            LiquidProfileLibrary lp_lib = new LiquidProfileLibrary(connection_string);
            DatabaseIntegration db_int = lp_lib.GetDatabaseIntegration();

            // start over with fresh "liquid_profiles" and "liquid_calibration_data" tables.
            Type[] ts = { typeof(LiquidProfile), typeof(LiquidCalibrationDatum) };
            foreach (Type t in ts)
            {
                db_int.DropTable(t);
                db_int.CreateTable(t);
            }

            // temp LiquidProfile declaration.

            // create a LiquidProfile for "dmso".
            LiquidProfile dmso_profile = new LiquidProfile("dmso");
            dmso_profile.BaseId = 0;
            dmso_profile.IsFactoryProfile = true;
            dmso_profile.MaxAccelDuringAspirate = 1.0;
            dmso_profile.MaxAccelDuringDispense = 2.0;
            // dmso_profile.Name = "dmso";
            dmso_profile.PostAspirateDelay = 3.0;
            dmso_profile.PostDispenseDelay = 4.0;
            dmso_profile.PostDispenseVolume = 5.0;
            dmso_profile.PreAspirateVolume = 6.0;
            dmso_profile.RateToAspirate = 7.0;
            dmso_profile.RateToDispense = 8.0;
            dmso_profile.SyringeSerialNumber = "001";
            dmso_profile.SyringeTypeId = 9;
            dmso_profile.TimeToEnterLiquid = 10.0;
            dmso_profile.TimeToExitLiquid = 11.0;
            dmso_profile.TipTypeId = 12;
            dmso_profile.TrackFluidHeight = false;
            dmso_profile.ZMoveDuringAspirating = 13.0;
            dmso_profile.ZMoveDuringDispensing = 13.0;

            // create a LiquidProfile for "water".
            LiquidProfile water_profile = new LiquidProfile("water");
            water_profile.BaseId = 0;
            water_profile.IsFactoryProfile = true;
            water_profile.MaxAccelDuringAspirate = 1.0;
            water_profile.MaxAccelDuringDispense = 2.0;
            // water_profile.Name = "water";
            water_profile.PostAspirateDelay = 3.0;
            water_profile.PostDispenseDelay = 4.0;
            water_profile.PostDispenseVolume = 5.0;
            water_profile.PreAspirateVolume = 6.0;
            water_profile.RateToAspirate = 7.0;
            water_profile.RateToDispense = 8.0;
            water_profile.SyringeSerialNumber = "001";
            water_profile.SyringeTypeId = 9;
            water_profile.TimeToEnterLiquid = 10.0;
            water_profile.TimeToExitLiquid = 11.0;
            water_profile.TipTypeId = 12;
            water_profile.TrackFluidHeight = false;
            water_profile.ZMoveDuringAspirating = 13.0;
            water_profile.ZMoveDuringDispensing = 13.0;

            // since the database is still empty, there shouldn't be any liquid profile names to enumerate.
            Assert.AreEqual(0, lp_lib.EnumerateLiquidProfileNames().Count);

            // likewise, the count of rows (entities) in the tables should be 0.
            Assert.AreEqual(0, db_int.CountEntities(typeof(LiquidProfile), ""));
            Assert.AreEqual(0, db_int.CountEntities(typeof(LiquidCalibrationDatum), ""));

            // querying by illegal names such as null or "" should result in an IllegalLiquidProfileNameException.
            try {
                lp_lib.LoadLiquidProfileByName(null);
                Assert.Fail("LoadLiquidProfileByName(null) should have thrown an exception");
            } catch (LiquidProfileLibrary.IllegalLiquidProfileNameException) {
                // good
            } catch (Exception) {
                // bad
                Assert.Fail("LoadLiquidProfileByName(null) threw the wrong type of exception");
            }

            try {
                lp_lib.LoadLiquidProfileByName("");
                Assert.Fail("LoadLiquidProfileByName(\"\") should have thrown an exception");
            } catch (LiquidProfileLibrary.IllegalLiquidProfileNameException) {
                // good
            } catch (Exception) {
                // bad
                Assert.Fail("LoadLiquidProfileByName(null) threw the wrong type of exception");
            }

            // database still empty, loading a liquid profile by any valid name ("dmso" in this case) should result in a LiquidProfileNotFoundException.
            try {
                lp_lib.LoadLiquidProfileByName("dmso");
                Assert.Fail( "LoadLiquidProfileByName(\"dmso\") should have thrown an exception");
            } catch( LiquidProfileLibrary.LiquidProfileNotFoundException) {
                // good
            } catch( Exception) {
                // bad
                Assert.Fail( "LoadLiquidProfileByName(\"dmso\") threw the wrong type of exception");
            }

            // add dmso_profile to the library.
            lp_lib.SaveLiquidProfileByName(dmso_profile);

            // now, loading "dmso" shouldn't throw an exception.
            try {
                lp_lib.LoadLiquidProfileByName("dmso");
            } catch (Exception) {
                Assert.Fail( "LoadLiquidProfileByName(\"dmso\") should not have thrown an exception");
            }

            // count of liquid profile names should be 1.
            Assert.AreEqual(1, lp_lib.EnumerateLiquidProfileNames().Count);

            // make sure at least one of the values within the "dmso" profile retrieved from the database is correct.
            ILiquidProfile temp_profile = lp_lib.LoadLiquidProfileByName("dmso");
            Assert.AreEqual(2.0, temp_profile.MaxAccelDuringDispense);
            // save the Id for later comparison.
            long dmso_id = temp_profile.Id;

            // add water_profile to the library.
            lp_lib.SaveLiquidProfileByName(water_profile);

            // now, we should be up to two profile names.
            Assert.AreEqual(2, lp_lib.EnumerateLiquidProfileNames().Count);

            // make modifications to local dmso_profile.
            dmso_profile.BaseId = 14;
            dmso_profile.IsFactoryProfile = false;
            dmso_profile.MaxAccelDuringAspirate = 15.0;
            dmso_profile.MaxAccelDuringDispense = 16.0;
            dmso_profile.Name = "dmso";
            dmso_profile.PostAspirateDelay = 17.0;
            dmso_profile.PostDispenseDelay = 18.0;
            dmso_profile.PostDispenseVolume = 19.0;
            dmso_profile.PreAspirateVolume = 20.0;
            dmso_profile.RateToAspirate = 21.0;
            dmso_profile.RateToDispense = 22.0;
            dmso_profile.SyringeSerialNumber = "002";
            dmso_profile.SyringeTypeId = 23;
            dmso_profile.TimeToEnterLiquid = 24.0;
            dmso_profile.TimeToExitLiquid = 25.0;
            dmso_profile.TipTypeId = 26;
            dmso_profile.TrackFluidHeight = true;
            dmso_profile.ZMoveDuringAspirating = 27.0;
            dmso_profile.ZMoveDuringDispensing = 27.0;

            // since the name stays "dmso", saving this profile should only update the entity.
            lp_lib.SaveLiquidProfileByName(dmso_profile);

            // there should still be just 2 liquid profile names.
            Assert.AreEqual(2, lp_lib.EnumerateLiquidProfileNames().Count);

            // the value checked earlier should now be different.
            temp_profile = lp_lib.LoadLiquidProfileByName("dmso");
            Assert.AreEqual(16.0, temp_profile.MaxAccelDuringDispense);
            // IT IS CRUCIAL THAT AN UPDATE DOESN'T CAUSE THE ID TO CHANGE.
            Assert.AreEqual(dmso_id, temp_profile.Id);

            // give dmso_profile a new name.
            dmso_profile.Name = "dmso 2";
            // save it to the database.
            lp_lib.SaveLiquidProfileByName(dmso_profile);
            // now there should be 3!
            Assert.AreEqual(3, lp_lib.EnumerateLiquidProfileNames().Count);

            // and, there should be 2 liquid calibration data per liquid profile since a default liquid profile has (0, 0) and (1000ul, 1000ul).
            Assert.AreEqual(3 * 2, db_int.CountEntities(typeof(LiquidCalibrationDatum), ""));

            // create a sorted list to use as calibration data.
            SortedList<double, double> data = new SortedList<double, double>();
            data[0] = 0;
            data[10] = 11;
            data[100] = 120;
            data[1000] = 1300;

            // set dmso_profile's name back to "dmso".
            dmso_profile.Name = "dmso";

            // associate calibration data to dmso_profile.
            dmso_profile.SetCalibrationData(data);
            // update dmso_profile.
            lp_lib.SaveLiquidProfileByName(dmso_profile);

            // now, there should be 8 data points in liquid_calibration_data.
            // we've added (10ul, 11ul) and (100, 120ul); we changed (1000ul, 1000ul) to (1000ul, 1300ul).
            Assert.AreEqual(3 * 2 + 2, db_int.CountEntities(typeof(LiquidCalibrationDatum), ""));

            // associate calibration data to water_profile.
            water_profile.SetCalibrationData(data);
            // update water_profile.
            lp_lib.SaveLiquidProfileByName(water_profile);

            // now, there should be 10 data points in liquid_calibration_data.
            // added and updated similarly for water profile.
            Assert.AreEqual(3 * 2 + 2 + 2, db_int.CountEntities(typeof(LiquidCalibrationDatum), ""));

            // we should have 3 profiles, 4 data points with "dmso" and "water", and 2 (default) data points with "dmso 2".
            Assert.AreEqual(3, lp_lib.EnumerateLiquidProfileNames().Count);
            Assert.AreEqual(4, lp_lib.LoadLiquidProfileByName("dmso").GetCalibrationData().Count);
            Assert.AreEqual(2, lp_lib.LoadLiquidProfileByName("dmso 2").GetCalibrationData().Count);
            Assert.AreEqual(4, lp_lib.LoadLiquidProfileByName("water").GetCalibrationData().Count);

            // modify the data at 100 and add one more point.
            data[100] = 121;
            data[10000] = 14000;
            // reassociate with dmso_profile.
            dmso_profile.SetCalibrationData(data);
            // update dmso_profile.
            lp_lib.SaveLiquidProfileByName(dmso_profile);

            // now there should be 11; 5 with "dmso", 2 with "dmso 2", and 4 with "water".
            Assert.AreEqual(11, db_int.CountEntities(typeof(LiquidCalibrationDatum), ""));
            Assert.AreEqual(5, lp_lib.LoadLiquidProfileByName("dmso").GetCalibrationData().Count);
            Assert.AreEqual(2, lp_lib.LoadLiquidProfileByName("dmso 2").GetCalibrationData().Count);
            Assert.AreEqual(4, lp_lib.LoadLiquidProfileByName("water").GetCalibrationData().Count);

            // make sure "dmso" data modification took hold.
            Assert.AreEqual(121, lp_lib.LoadLiquidProfileByName("dmso").GetCalibrationData()[100]);
            // "water" data should be unaffected.
            Assert.AreEqual(120, lp_lib.LoadLiquidProfileByName("water").GetCalibrationData()[100]);

            // remove a point from the data.
            data.Remove(10);
            Console.WriteLine("size of data = " + data.Count);
            // reassociate with dmso_profile.
            dmso_profile.SetCalibrationData(data);
            // update dmso_profile.
            lp_lib.SaveLiquidProfileByName(dmso_profile);

            // should be back down to 10; 4 with "dmso", 2 with "dmso 2", and 4 with "water".
            Assert.AreEqual(10, db_int.CountEntities(typeof(LiquidCalibrationDatum), ""));
            Assert.AreEqual(4, lp_lib.LoadLiquidProfileByName("dmso").GetCalibrationData().Count);
            Assert.AreEqual(2, lp_lib.LoadLiquidProfileByName("dmso 2").GetCalibrationData().Count);
            Assert.AreEqual(4, lp_lib.LoadLiquidProfileByName("water").GetCalibrationData().Count);

            // delete "dmso 2" profile.
            lp_lib.DeleteLiquidProfileByName("dmso 2");

            // still 8 total; 4 with "dmso" and 4 with "water", but query to "dmso 2" should result in a LiquidProfileNotFoundException.
            Assert.AreEqual(4, lp_lib.LoadLiquidProfileByName("dmso").GetCalibrationData().Count);
            try {
                lp_lib.LoadLiquidProfileByName("dmso 2").GetCalibrationData();
                Assert.Fail( "LoadLiquidProfileByName(\"dmso 2\").GetCalibrationData() should have thrown an exception");
            } catch (LiquidProfileLibrary.LiquidProfileNotFoundException) {
                // good
            } catch (Exception) {
                Assert.Fail( "LoadLiquidProfileByName(\"dmso 2\").GetCalibrationData() threw the wrong type of exception");
            }
            Assert.AreEqual(4, lp_lib.LoadLiquidProfileByName("water").GetCalibrationData().Count);

            // before deleting "water" profile, querying "water" profile doesn't throw an exception.
            try {
                lp_lib.LoadLiquidProfileByName("water").GetCalibrationData();
            } catch( Exception) {
                Assert.Fail( "LoadLiquidProfileByName(\"water\") should not have thrown an exception");
            }
            // delete "water" profile.
            lp_lib.DeleteLiquidProfileByName("water");
            // after deleting "water" profile, querying "water" profile throws a LiquidProfileNotFoundException.
            try {
                lp_lib.LoadLiquidProfileByName("water").GetCalibrationData();
                Assert.Fail( "LoadLiquidProfileByName(\"water\").GetCalibrationData() should have thrown an exception");
            } catch (LiquidProfileLibrary.LiquidProfileNotFoundException) {
                // good
            } catch (Exception) {
                Assert.Fail( "LoadLiquidProfileByName(\"dmso 2\").GetCalibrationData() threw the wrong type of exception");
            }

            // now the total number of "liquid_calibration_data" is down to 4.
            Assert.AreEqual(4, db_int.CountEntities(typeof(LiquidCalibrationDatum), ""));

            // delete "dmso" profile.
            lp_lib.DeleteLiquidProfileByName("dmso");

            // now both tables should be empty again...
            Assert.AreEqual(0, db_int.CountEntities(typeof(LiquidProfile), ""));
            Assert.AreEqual(0, db_int.CountEntities(typeof(LiquidCalibrationDatum), ""));
        }

        [TestMethod]
        public void TestLiquidProfileLibraryLowLevel()
        {
            const string connection_string = "test_lp_lib.s3db";
            LiquidProfileLibrary lp_lib = new LiquidProfileLibrary(connection_string);
            DatabaseIntegration db_int = lp_lib.GetDatabaseIntegration();

            // test query string generation.
            // test dropping and creating tables.
            Type[] ts = { typeof(LiquidProfile), typeof(LiquidCalibrationDatum) };
            foreach (Type t in ts)
            {
                db_int.DropTable(t);
                db_int.CreateTable(t);
            }

            // basic insertion of data.
            String[] ss = { "DMSO", "Water" };
            foreach (string s in ss)
            {
                LiquidProfile lp = new LiquidProfile(s);
                lp.BaseId = 0;
                lp.IsFactoryProfile = true;
                lp.MaxAccelDuringAspirate = 1.0;
                lp.MaxAccelDuringDispense = 2.0;
                // lp.Name = s;
                lp.PostAspirateDelay = 3.0;
                lp.PostDispenseDelay = 4.0;
                lp.PostDispenseVolume = 5.0;
                lp.PreAspirateVolume = 6.0;
                lp.RateToAspirate = 7.0;
                lp.RateToDispense = 8.0;
                lp.SyringeSerialNumber = "mr roboto";
                lp.SyringeTypeId = 9;
                lp.TimeToEnterLiquid = 10.0;
                lp.TimeToExitLiquid = 11.0;
                lp.TipTypeId = 12;
                lp.TrackFluidHeight = false;
                lp.ZMoveDuringAspirating = 13.0;
                lp.ZMoveDuringDispensing = 13.0;

                db_int.InsertEntity(lp);
            }

            for (long l = 1; l <= 2; l++)
            {
                LiquidCalibrationDatum lcd = new LiquidCalibrationDatum();
                lcd.LiquidProfileId = l;
                lcd.RequestedVolume = 1 * l;
                lcd.VolumeOffset = 0.1 * l;

                db_int.InsertEntity(lcd);

                lcd.LiquidProfileId = l;
                lcd.RequestedVolume = 10 * l;
                lcd.VolumeOffset = 1.0 * l;

                db_int.InsertEntity(lcd);
            }

            List<LiquidProfile> lps = db_int.SelectEntities<LiquidProfile>("");
            foreach (LiquidProfile lp in lps)
            {
                Console.WriteLine(lp);
                if (lp.Id == 1)
                {
                    lp.SyringeSerialNumber = "Felix";
                    db_int.UpdateEntity(lp, "where id = 1");
                }
                // if( lp.Id == 2){
                // di.DeleteEntities( typeof( LiquidProfile), "where id = 2");
                // }
            }
            List<LiquidCalibrationDatum> lcds = db_int.SelectEntities<LiquidCalibrationDatum>("where liquid_profile_id = 1");
            foreach (LiquidCalibrationDatum lcd in lcds)
            {
                Console.WriteLine(lcd);
            }

            List<string> names = lp_lib.EnumerateLiquidProfileNames();
            foreach (string name in names)
            {
                Console.WriteLine(name);
                ILiquidProfile liquid_profile = lp_lib.LoadLiquidProfileByName(name);
                Console.WriteLine(liquid_profile);
            }

            LiquidProfile lp_test = (LiquidProfile)(lp_lib.LoadLiquidProfileByName("Water"));
            SortedList<double, double> data = new SortedList<double, double>();
            data[1] = 1.1;
            data[2] = 2.2;
            data[10] = 11;
            data[20] = 23;
            data[30] = 33;
            data[500] = 520;
            lp_test.SetCalibrationData(data);
            lp_lib.SaveLiquidProfileByName(lp_test);
        }

        [TestMethod]
        public void TestCreateReasonableLiquidProfile()
        {
            const string connection_string = "liquids.s3db";
            LiquidProfileLibrary lp_lib = new LiquidProfileLibrary(connection_string);
            DatabaseIntegration db_int = lp_lib.GetDatabaseIntegration();

            // start over with fresh "liquid_profiles" and "liquid_calibration_data" tables.
            Type[] ts = { typeof(LiquidProfile), typeof(LiquidCalibrationDatum) };
            foreach (Type t in ts)
            {
                db_int.DropTable(t);
                db_int.CreateTable(t);
            }

            // create a sorted list to use as calibration data.
            SortedList<double, double> data = new SortedList<double, double>();

            // create a LiquidProfile for "dmso".
            LiquidProfile dmso_profile = new LiquidProfile("dmso");
            dmso_profile.BaseId = 0;
            dmso_profile.IsFactoryProfile = true;
            dmso_profile.MaxAccelDuringAspirate = 0;
            dmso_profile.MaxAccelDuringDispense = 0;
            // dmso_profile.Name = "dmso";
            dmso_profile.PostAspirateDelay = 0.0;
            dmso_profile.PostDispenseDelay = 0.0;
            dmso_profile.PostDispenseVolume = 0.0;
            dmso_profile.PreAspirateVolume = 0.0;
            dmso_profile.RateToAspirate = 250.0;
            dmso_profile.RateToDispense = 250.0;
            dmso_profile.SyringeSerialNumber = "001";
            dmso_profile.SyringeTypeId = 0;
            dmso_profile.TimeToEnterLiquid = 1.0;
            dmso_profile.TimeToExitLiquid = 1.0;
            dmso_profile.TipTypeId = 0;
            dmso_profile.TrackFluidHeight = false;
            dmso_profile.ZMoveDuringAspirating = 0;
            dmso_profile.ZMoveDuringDispensing = 0;

            data[0] = 0;
            data[1] = 1.1;
            data[10] = 11;
            data[100] = 110;
            data[1000] = 1100;

            dmso_profile.SetCalibrationData(data);

            // create a LiquidProfile for "water".
            LiquidProfile water_profile = new LiquidProfile("water");
            water_profile.BaseId = 0;
            water_profile.IsFactoryProfile = true;
            water_profile.MaxAccelDuringAspirate = 0;
            water_profile.MaxAccelDuringDispense = 0;
            // water_profile.Name = "water";
            water_profile.PostAspirateDelay = 5.0;
            water_profile.PostDispenseDelay = 2.5;
            water_profile.PostDispenseVolume = 20.0;
            water_profile.PreAspirateVolume = 20.0;
            water_profile.RateToAspirate = 10.0;
            water_profile.RateToDispense = 25.0;
            water_profile.SyringeSerialNumber = "001";
            water_profile.SyringeTypeId = 0;
            water_profile.TimeToEnterLiquid = 2.5;
            water_profile.TimeToExitLiquid = 2.5;
            water_profile.TipTypeId = 0;
            water_profile.TrackFluidHeight = false;
            water_profile.ZMoveDuringAspirating = 5.0;
            water_profile.ZMoveDuringDispensing = 5.0;

            data[0] = 0;
            data[1] = 0.9;
            data[10] = 9;
            data[100] = 90;
            data[1000] = 900;

            water_profile.SetCalibrationData(data);

            lp_lib.SaveLiquidProfileByName(dmso_profile);
            lp_lib.SaveLiquidProfileByName(water_profile);
        }
    }
}
