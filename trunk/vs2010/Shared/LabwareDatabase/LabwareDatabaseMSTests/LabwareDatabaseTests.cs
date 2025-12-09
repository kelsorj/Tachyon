using System;
using System.Collections.Generic;
using BioNex.Shared.LabwareDatabase;
using BioNex.Shared.LibraryInterfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LabwareDatabaseMSTests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class LabwareDatabaseTests
    {
        public LabwareDatabaseTests()
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

        private LabwareDatabase _labware_database { get; set; }
        const string database_filename = "test.s3db";

        [TestInitialize]
        public void Init()
        {
            _labware_database = new LabwareDatabase( database_filename);
            // create the new database file
            CreateTestDatabase();
        }

        private void CreateTestDatabase()
        {
            try {
                Type[] tables = { typeof( Labware), typeof( TipProperties), typeof( SpeedSetting), typeof( LabwarePropertyValue), typeof( LabwareProperty) };
                foreach( Type t in tables) {
                    _labware_database.DBIntegration.DropTable( t);
                    _labware_database.DBIntegration.CreateTable( t);
                }
                // add the labware properties that we have selected
                CreateMasterLabwareProperties();
            } catch( MissingMethodException) {
                throw( new Exception( "You probably tried to create the same labware property twice.  See DatabaseIntegration.SelectEntities."));
            } catch( Exception) {
                throw;
            }
        }

        private void CreateMasterLabwareProperties()
        {
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.Thickness, (long)LabwarePropertyType.DOUBLE, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.WellDepth, (long)LabwarePropertyType.DOUBLE, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.NumberOfWells, (long)LabwarePropertyType.INTEGER, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.RowSpacing, (long)LabwarePropertyType.DOUBLE, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.ColumnSpacing, (long)LabwarePropertyType.DOUBLE, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.GripperOffset, (long)LabwarePropertyType.DOUBLE, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.MinPortraitGripperPos, (long)LabwarePropertyType.DOUBLE, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.MaxPortraitGripperPos, (long)LabwarePropertyType.DOUBLE, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.MinLandscapeGripperPos, (long)LabwarePropertyType.DOUBLE, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.MaxLandscapeGripperPos, (long)LabwarePropertyType.DOUBLE, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.GripperTorque, (long)LabwarePropertyType.DOUBLE, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.MaxAllowableVolume, (long)LabwarePropertyType.DOUBLE, 0, 0));
            // I don't like using INTEGER for well shape and well bottom shape.  It should be something
            // like LabwarePropertyType.OPTION, but then we need to tell it what the options are!  And
            // I think that would then mean we need to have more tables -- one for each labware property
            // that's an option, so we can display the option values.
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.WellShape, (long)LabwarePropertyType.INTEGER, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.WellBottomShape, (long)LabwarePropertyType.INTEGER, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.SkirtHeight, (long)LabwarePropertyType.DOUBLE, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.EmptyMass, (long)LabwarePropertyType.DOUBLE, 0, 0));
            _labware_database.AddLabwareProperty( new LabwareProperty( LabwarePropertyNames.BarcodeSides, (long)LabwarePropertyType.STRING, 0, 0));
        }

        [TestCleanup]
        public void Dispose()
        {
        }

        [TestMethod]
        public void TestGetLabwareNames()
        {
            _labware_database.AddLabware( new Labware( "unit test 1", "test1", "test1"));
            _labware_database.AddLabware( new Labware( "unit test 2", "test2", "test2"));
            _labware_database.AddLabware( new Labware( "unit test 3", "test3", "test3"));
            List<string> labware_names = _labware_database.GetLabwareNames();
            Assert.IsTrue( labware_names.Contains( "unit test 1"));
            Assert.IsTrue( labware_names.Contains( "unit test 2"));
            Assert.IsTrue( labware_names.Contains( "unit test 3"));
        }

        [TestMethod]
        public void TestAddLabware()
        {
            #region For P1 upgrade only
            /*
            string name = "NUNC 96 clear round well flat bottom";
            Labware labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.Thickness] = 14.47;
            labware[LabwarePropertyNames.WellDepth] = 10.75;
            labware[LabwarePropertyNames.NumberOfWells] = 96;
            labware[LabwarePropertyNames.RowSpacing] = 9;
            labware[LabwarePropertyNames.ColumnSpacing] = 9;
            labware[LabwarePropertyNames.GripperOffset] = 9;
            labware[LabwarePropertyNames.MinPortraitGripperPos] = 81;
            labware[LabwarePropertyNames.MaxPortraitGripperPos] = 96;
            labware[LabwarePropertyNames.MinLandscapeGripperPos] = 121;
            labware[LabwarePropertyNames.MaxLandscapeGripperPos] = 128;
            labware[LabwarePropertyNames.GripperTorque] = 1;
            labware[LabwarePropertyNames.WellShape] = 0;
            labware[LabwarePropertyNames.SkirtHeight] = 0;
            labware[LabwarePropertyNames.EmptyMass] = 0;
            labware[LabwarePropertyNames.MaxAllowableVolume] = 0;
            labware[LabwarePropertyNames.WellBottomShape] = 0;
            _labware_database.AddLabware( labware);

            name = "NUNC 96 clear round well conical bottom";
            labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.Thickness] = 14.4;
            labware[LabwarePropertyNames.WellDepth] = 9.46;
            labware[LabwarePropertyNames.NumberOfWells] = 96;
            labware[LabwarePropertyNames.RowSpacing] = 9;
            labware[LabwarePropertyNames.ColumnSpacing] = 9;
            labware[LabwarePropertyNames.GripperOffset] = 8;
            labware[LabwarePropertyNames.MinPortraitGripperPos] = 79.5;
            labware[LabwarePropertyNames.MaxPortraitGripperPos] = 96;
            labware[LabwarePropertyNames.MinLandscapeGripperPos] = 121;
            labware[LabwarePropertyNames.MaxLandscapeGripperPos] = 128;
            labware[LabwarePropertyNames.GripperTorque] = 1;
            labware[LabwarePropertyNames.WellShape] = 0;
            labware[LabwarePropertyNames.SkirtHeight] = 0;
            labware[LabwarePropertyNames.EmptyMass] = 0;
            labware[LabwarePropertyNames.MaxAllowableVolume] = 0;
            labware[LabwarePropertyNames.WellBottomShape] = 0;
            _labware_database.AddLabware( labware);

            name = "Greiner 384 781101 clear drafted square well";
            labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.Thickness] = 14.39;
            labware[LabwarePropertyNames.WellDepth] = 11.4;
            labware[LabwarePropertyNames.NumberOfWells] = 384;
            labware[LabwarePropertyNames.RowSpacing] = 4.5;
            labware[LabwarePropertyNames.ColumnSpacing] = 4.5;
            labware[LabwarePropertyNames.GripperOffset] = 11;
            labware[LabwarePropertyNames.MinPortraitGripperPos] = 80;
            labware[LabwarePropertyNames.MaxPortraitGripperPos] = 96;
            labware[LabwarePropertyNames.MinLandscapeGripperPos] = 121;
            labware[LabwarePropertyNames.MaxLandscapeGripperPos] = 128;
            labware[LabwarePropertyNames.GripperTorque] = 1;
            labware[LabwarePropertyNames.WellShape] = 0;
            labware[LabwarePropertyNames.SkirtHeight] = 0;
            labware[LabwarePropertyNames.EmptyMass] = 0;
            labware[LabwarePropertyNames.MaxAllowableVolume] = 0;
            labware[LabwarePropertyNames.WellBottomShape] = 0;
            _labware_database.AddLabware( labware);

            name = "4x deep well plate";
            labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.Thickness] = 25;
            labware[LabwarePropertyNames.WellDepth] = 19;
            labware[LabwarePropertyNames.NumberOfWells] = 384;
            labware[LabwarePropertyNames.RowSpacing] = 4.5;
            labware[LabwarePropertyNames.ColumnSpacing] = 4.5;
            labware[LabwarePropertyNames.GripperOffset] = 11;
            labware[LabwarePropertyNames.MinPortraitGripperPos] = 80.4;
            labware[LabwarePropertyNames.MaxPortraitGripperPos] = 96;
            labware[LabwarePropertyNames.MinLandscapeGripperPos] = 121;
            labware[LabwarePropertyNames.MaxLandscapeGripperPos] = 128;
            labware[LabwarePropertyNames.GripperTorque] = 1;
            labware[LabwarePropertyNames.WellShape] = 0;
            labware[LabwarePropertyNames.SkirtHeight] = 0;
            labware[LabwarePropertyNames.EmptyMass] = 0;
            labware[LabwarePropertyNames.MaxAllowableVolume] = 0;
            labware[LabwarePropertyNames.WellBottomShape] = 0;
            _labware_database.AddLabware( labware);

            name = "tipbox";
            labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.Thickness] = 55.98;
            labware[LabwarePropertyNames.WellDepth] = 0;
            labware[LabwarePropertyNames.NumberOfWells] = 96;
            labware[LabwarePropertyNames.RowSpacing] = 9;
            labware[LabwarePropertyNames.ColumnSpacing] = 9;
            labware[LabwarePropertyNames.GripperOffset] = 21;
            labware[LabwarePropertyNames.MinPortraitGripperPos] = 80.05;
            labware[LabwarePropertyNames.MaxPortraitGripperPos] = 96;
            labware[LabwarePropertyNames.MinLandscapeGripperPos] = 121;
            labware[LabwarePropertyNames.MaxLandscapeGripperPos] = 128;
            labware[LabwarePropertyNames.GripperTorque] = 1;
            labware[LabwarePropertyNames.WellShape] = 0;
            labware[LabwarePropertyNames.SkirtHeight] = 0;
            labware[LabwarePropertyNames.EmptyMass] = 0;
            labware[LabwarePropertyNames.MaxAllowableVolume] = 0;
            labware[LabwarePropertyNames.WellBottomShape] = 0;
            TipProperties tip_properties = new TipProperties {
                CurrentLimit = 1.5, LabwareId = 0, LengthInMm = 50, PressAcceleration = 732, PressMaxAcceptablePosition = 0,
                PressMinAcceptablePosition = 0, PressStartTorquePosition = 0, PressTargetPosition = 0, PressTimeMs = 250,
                PressTorquePercentage = 0, PressVelocity = 500, SealOffset = 10.4, VolumeInUl = 200, XOffset = 0, YOffset = 0
            };

            _labware_database.AddTipBox( labware, tip_properties);

            name = "teaching jig 2";
            labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.Thickness] = 16;
            labware[LabwarePropertyNames.WellDepth] = 0;
            labware[LabwarePropertyNames.NumberOfWells] = 96;
            labware[LabwarePropertyNames.RowSpacing] = 9;
            labware[LabwarePropertyNames.ColumnSpacing] = 9;
            labware[LabwarePropertyNames.GripperOffset] = 10;
            labware[LabwarePropertyNames.MinPortraitGripperPos] = 83.3;
            labware[LabwarePropertyNames.MaxPortraitGripperPos] = 96;
            labware[LabwarePropertyNames.MinLandscapeGripperPos] = 121;
            labware[LabwarePropertyNames.MaxLandscapeGripperPos] = 128;
            labware[LabwarePropertyNames.GripperTorque] = 1;
            labware[LabwarePropertyNames.WellShape] = 0;
            labware[LabwarePropertyNames.SkirtHeight] = 0;
            labware[LabwarePropertyNames.EmptyMass] = 0;
            labware[LabwarePropertyNames.MaxAllowableVolume] = 0;
            labware[LabwarePropertyNames.WellBottomShape] = 0;
            _labware_database.AddLabware( labware);

            name = "Field Plate";
            labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.Thickness] = 43.8;
            labware[LabwarePropertyNames.WellDepth] = 39.5;
            labware[LabwarePropertyNames.NumberOfWells] = 96;
            labware[LabwarePropertyNames.RowSpacing] = 9;
            labware[LabwarePropertyNames.ColumnSpacing] = 9;
            labware[LabwarePropertyNames.GripperOffset] = 10;
            labware[LabwarePropertyNames.MinPortraitGripperPos] = 80.3;
            labware[LabwarePropertyNames.MaxPortraitGripperPos] = 96;
            labware[LabwarePropertyNames.MinLandscapeGripperPos] = 121;
            labware[LabwarePropertyNames.MaxLandscapeGripperPos] = 128;
            labware[LabwarePropertyNames.GripperTorque] = 1;
            labware[LabwarePropertyNames.WellShape] = 0;
            labware[LabwarePropertyNames.SkirtHeight] = 0;
            labware[LabwarePropertyNames.EmptyMass] = 0;
            labware[LabwarePropertyNames.MaxAllowableVolume] = 0;
            labware[LabwarePropertyNames.WellBottomShape] = 0;
            _labware_database.AddLabware( labware);

            name = "96 half height";
            labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.Thickness] = 30;
            labware[LabwarePropertyNames.WellDepth] = 28;
            labware[LabwarePropertyNames.NumberOfWells] = 96;
            labware[LabwarePropertyNames.RowSpacing] = 9;
            labware[LabwarePropertyNames.ColumnSpacing] = 9;
            labware[LabwarePropertyNames.GripperOffset] = 11;
            labware[LabwarePropertyNames.MinPortraitGripperPos] = 80.3;
            labware[LabwarePropertyNames.MaxPortraitGripperPos] = 96;
            labware[LabwarePropertyNames.MinLandscapeGripperPos] = 121;
            labware[LabwarePropertyNames.MaxLandscapeGripperPos] = 128;
            labware[LabwarePropertyNames.GripperTorque] = 1;
            labware[LabwarePropertyNames.WellShape] = 0;
            labware[LabwarePropertyNames.SkirtHeight] = 0;
            labware[LabwarePropertyNames.EmptyMass] = 0;
            labware[LabwarePropertyNames.MaxAllowableVolume] = 0;
            labware[LabwarePropertyNames.WellBottomShape] = 0;
            _labware_database.AddLabware( labware);
            
            name = "1x labnet plate";
            labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.Thickness] = 30;
            labware[LabwarePropertyNames.WellDepth] = 28;
            labware[LabwarePropertyNames.NumberOfWells] = 96;
            labware[LabwarePropertyNames.RowSpacing] = 9;
            labware[LabwarePropertyNames.ColumnSpacing] = 9;
            labware[LabwarePropertyNames.GripperOffset] = 11;
            labware[LabwarePropertyNames.MinPortraitGripperPos] = 80.3;
            labware[LabwarePropertyNames.MaxPortraitGripperPos] = 96;
            labware[LabwarePropertyNames.MinLandscapeGripperPos] = 121;
            labware[LabwarePropertyNames.MaxLandscapeGripperPos] = 128;
            labware[LabwarePropertyNames.GripperTorque] = 1;
            labware[LabwarePropertyNames.WellShape] = 0;
            labware[LabwarePropertyNames.SkirtHeight] = 0;
            labware[LabwarePropertyNames.EmptyMass] = 0;
            labware[LabwarePropertyNames.MaxAllowableVolume] = 0;
            labware[LabwarePropertyNames.WellBottomShape] = 0;
            _labware_database.AddLabware( labware);

            name = "4x normal plate";
            labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.Thickness] = 14.36;
            labware[LabwarePropertyNames.WellDepth] = 12.1;
            labware[LabwarePropertyNames.NumberOfWells] = 384;
            labware[LabwarePropertyNames.RowSpacing] = 4.5;
            labware[LabwarePropertyNames.ColumnSpacing] = 4.5;
            labware[LabwarePropertyNames.GripperOffset] = 7;
            labware[LabwarePropertyNames.MinPortraitGripperPos] = 80.3;
            labware[LabwarePropertyNames.MaxPortraitGripperPos] = 96;
            labware[LabwarePropertyNames.MinLandscapeGripperPos] = 121.95;
            labware[LabwarePropertyNames.MaxLandscapeGripperPos] = 128;
            labware[LabwarePropertyNames.GripperTorque] = 1;
            labware[LabwarePropertyNames.WellShape] = 0;
            labware[LabwarePropertyNames.SkirtHeight] = 0;
            labware[LabwarePropertyNames.EmptyMass] = 0;
            labware[LabwarePropertyNames.MaxAllowableVolume] = 0;
            labware[LabwarePropertyNames.WellBottomShape] = 0;
            _labware_database.AddLabware( labware);

            name = "1x QC plate";
            labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.Thickness] = 14.64;
            labware[LabwarePropertyNames.WellDepth] = 11.15;
            labware[LabwarePropertyNames.NumberOfWells] = 96;
            labware[LabwarePropertyNames.RowSpacing] = 9;
            labware[LabwarePropertyNames.ColumnSpacing] = 9;
            labware[LabwarePropertyNames.GripperOffset] = 9.5;
            labware[LabwarePropertyNames.MinPortraitGripperPos] = 80;
            labware[LabwarePropertyNames.MaxPortraitGripperPos] = 96;
            labware[LabwarePropertyNames.MinLandscapeGripperPos] = 121;
            labware[LabwarePropertyNames.MaxLandscapeGripperPos] = 128;
            labware[LabwarePropertyNames.GripperTorque] = 1;
            labware[LabwarePropertyNames.WellShape] = 0;
            labware[LabwarePropertyNames.SkirtHeight] = 0;
            labware[LabwarePropertyNames.EmptyMass] = 0;
            labware[LabwarePropertyNames.MaxAllowableVolume] = 0;
            labware[LabwarePropertyNames.WellBottomShape] = 0;
            _labware_database.AddLabware( labware);

            name = "4x QC plate";
            labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.Thickness] = 14.39;
            labware[LabwarePropertyNames.WellDepth] = 11.4;
            labware[LabwarePropertyNames.NumberOfWells] = 384;
            labware[LabwarePropertyNames.RowSpacing] = 4.5;
            labware[LabwarePropertyNames.ColumnSpacing] = 4.5;
            labware[LabwarePropertyNames.GripperOffset] = 10;
            labware[LabwarePropertyNames.MinPortraitGripperPos] = 80.1;
            labware[LabwarePropertyNames.MaxPortraitGripperPos] = 96;
            labware[LabwarePropertyNames.MinLandscapeGripperPos] = 121.95;
            labware[LabwarePropertyNames.MaxLandscapeGripperPos] = 128;
            labware[LabwarePropertyNames.GripperTorque] = 1;
            labware[LabwarePropertyNames.WellShape] = 0;
            labware[LabwarePropertyNames.SkirtHeight] = 0;
            labware[LabwarePropertyNames.EmptyMass] = 0;
            labware[LabwarePropertyNames.MaxAllowableVolume] = 0;
            labware[LabwarePropertyNames.WellBottomShape] = 0;
            _labware_database.AddLabware( labware);
            return;
             */
            #endregion

            const string name = "unit test";
            // create the labware and add it to the database
            Labware labware = new Labware( name, "test1", "test1");
            labware[LabwarePropertyNames.NumberOfWells] = 96;
            labware[LabwarePropertyNames.Thickness] = 14.1;
            labware[LabwarePropertyNames.WellDepth] = 12.5;
            _labware_database.AddLabware( labware);
            // pull the labware from the database and verify that the values are correct
            ILabware check = _labware_database["unit test"];
            Assert.AreEqual( 96, int.Parse( check[LabwarePropertyNames.NumberOfWells].ToString()));
            Assert.AreEqual( 14.1, double.Parse( check[LabwarePropertyNames.Thickness].ToString()));
            Assert.AreEqual( 12.5, double.Parse( check[LabwarePropertyNames.WellDepth].ToString()));
        }

        [TestMethod]
        public void TestCloneLabware()
        {
            const string name = "clone test";
            Labware labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.NumberOfWells] = 96;
            labware[LabwarePropertyNames.Thickness] = 14.1;
            labware[LabwarePropertyNames.WellDepth] = 12.5;
            _labware_database.AddLabware( labware);
            _labware_database.CloneLabware( labware, "clone test clone");
            ILabware check = _labware_database["clone test clone"];
            Assert.AreEqual( 96, int.Parse( check[LabwarePropertyNames.NumberOfWells].ToString()));
            Assert.AreEqual( 14.1, double.Parse( check[LabwarePropertyNames.Thickness].ToString()));
            Assert.AreEqual( 12.5, double.Parse( check[LabwarePropertyNames.WellDepth].ToString()));
        }

        [TestMethod]
        public void TestAddLid()
        {
            const string name = "lid test";
            Labware labware = new Labware( name, "", "");
            labware[LabwarePropertyNames.NumberOfWells] = 96;
            labware[LabwarePropertyNames.Thickness] = 14.1;
            labware[LabwarePropertyNames.WellDepth] = 12.5;

            long plate_id = _labware_database.AddLabware( labware);
            long lid_id = _labware_database.AddLid( labware);
            labware = _labware_database["lid test"] as Labware;
            Assert.AreEqual( plate_id, labware.Id);

            ILabware lid = _labware_database["lid test (lid)"];
            Assert.AreEqual( lid_id, lid.Id);
            Assert.AreEqual( labware.LidId, lid_id);
        }

        /// <summary>
        /// Tests adding the same labware twice.  Should update the existing labware.
        /// </summary>
        [TestMethod]
        public void TestAddDuplicateName()
        {
            _labware_database.AddLabware( new Labware( "duplicate test", "", ""));
            try {
                _labware_database.AddLabware( new Labware( "duplicate test", "", ""));
            } catch( Exception) {
                Assert.Fail( "Should not have thrown an exception");
            }
        }

        [TestMethod]
        public void TestRenameLabware()
        {
            // create some labware and tip boxes
            _labware_database.AddLabware( new Labware( "unit test 4", "unit test 4", "unit test 4"));
            Labware labware5 = new Labware( "unit test 5", "unit test 5", "unit test 5");
            _labware_database.AddLabware( labware5);
            _labware_database.AddLabware( new Labware( "unit test 6", "unit test 6", "unit test 6"));

            // rename labware
            _labware_database.RenameLabware( labware5, "renamed unit test 5");

            try {
                ILabware l = _labware_database["unit test 5"];
                Assert.Fail( "Should have thrown an exception");
            } catch( LabwareNotFoundException) {
                // good
            } catch( Exception) {
                Assert.Fail( "Threw the wrong kind of exception");
            }

            try {
                _labware_database.GetLabware("renamed unit test 5");
            } catch( Exception) {
                Assert.Fail( "Should not have thrown an exception");
            }
            ILabware check = _labware_database.GetLabware( "renamed unit test 5");
            Assert.AreEqual( "unit test 5", check.Notes);
            Assert.AreEqual( "unit test 5", check.Tags);
        }

        [TestMethod]
        public void TestRenameTipBox()
        {
            // create some labware and tip boxes
            _labware_database.AddLabware( new TipBox( "unit test tipbox 4", "unit test tipbox 4", "unit test tipbox 4"));
            TipBox tipbox = new TipBox( "unit test tipbox 5", "unit test tipbox 5", "unit test tipbox 5");
            TipProperties tip_properties = new TipProperties { CurrentLimit = 0, LengthInMm = 20, PressAcceleration = 1,
                                                           PressMaxAcceptablePosition = 50, PressMinAcceptablePosition = 60,
                                                           PressStartTorquePosition = 65, PressTargetPosition = 55,
                                                           PressTimeMs = 500, PressTorquePercentage = 100, PressVelocity = 10,
                                                           VolumeInUl = 200, XOffset = 0, YOffset = 0 };
            _labware_database.AddTipBox( tipbox, tip_properties);
            _labware_database.AddLabware( new TipBox( "unit test tipbox 6", "unit test tipbox 6", "unit test tipbox 6"));

            // rename tipbox
            _labware_database.RenameLabware( tipbox, "renamed unit test tipbox 5");
            try {
                ILabware l = _labware_database["unit test tipbox 5"];
                Assert.Fail( "Should have thrown an exception");
            } catch( LabwareNotFoundException) {
                // good
            } catch( Exception) {
                Assert.Fail( "Threw the wrong kind of exception");
            }
            TipBox tb4 = _labware_database["renamed unit test tipbox 5"] as TipBox;
            Assert.AreEqual( "unit test tipbox 5", tb4.Notes);
            Assert.AreEqual( "unit test tipbox 5", tb4.Tags);
        }

        [TestMethod]
        public void TestDeleteTipBox()
        {
            // create a tipbox
            TipBox tipbox = new TipBox( "delete me", "", "");
            TipProperties tip_properties = new TipProperties { CurrentLimit = 0, LengthInMm = 20, PressAcceleration = 1,
                                                           PressMaxAcceptablePosition = 50, PressMinAcceptablePosition = 60,
                                                           PressStartTorquePosition = 65, PressTargetPosition = 55,
                                                           PressTimeMs = 500, PressTorquePercentage = 100, PressVelocity = 10,
                                                           VolumeInUl = 200, XOffset = 0, YOffset = 0 };
            long id = _labware_database.AddTipBox( tipbox, tip_properties);
            // delete labware
            _labware_database.DeleteLabware( "delete me");
            // tipbox should be gone
            try {
                ILabware l = _labware_database["delete me"];
                Assert.Fail( "Should have thrown an exception");
            } catch( LabwareNotFoundException) {
                // good
            } catch( Exception) {
                Assert.Fail( "Threw the wrong kind of exception");
            }
            // since it was a tipbox, we also want to make sure that we didn't orphan any tipbox properties
            ITipProperties check_props = _labware_database.GetTipBoxProperties( id);
            Assert.IsNull( check_props);
        }

        [TestMethod]
        public void TestDeleteLabware()
        {
            // create some labware and tip boxes
            _labware_database.AddLabware( new Labware( "delete me", "", ""));
            // delete labware
            _labware_database.DeleteLabware( "delete me");
            // check cache first
            try {
                ILabware l = _labware_database["delete me"];
                Assert.Fail( "Should have thrown an exception");
            } catch( LabwareNotFoundException) {
                // good
            } catch( Exception) {
                Assert.Fail( "Threw the wrong kind of exception");
            }
        }

        [TestMethod]
        public void TestUpdateLabware()
        {
            // create some labware and tip boxes
            Labware labware = new Labware( "update test", "", "");
            labware[LabwarePropertyNames.NumberOfWells] = 96;
            _labware_database.AddLabware( labware);
            // change labware properties
            labware[LabwarePropertyNames.NumberOfWells] = 384;
            _labware_database.UpdateLabware( labware);
            // check cache
            ILabware labware_copy = _labware_database["update test"];
            Assert.AreEqual( 384, int.Parse( labware_copy[LabwarePropertyNames.NumberOfWells].ToString()));
        }

        [TestMethod]
        public void TestUpdateTipBox()
        {
            // add a new tipbox with the default TipPressParameters
            TipBox tipbox = new TipBox( "update tipbox", "", "");
            TipProperties tip_properties = new TipProperties { CurrentLimit = 0, LengthInMm = 20, PressAcceleration = 1,
                                                           PressMaxAcceptablePosition = 50, PressMinAcceptablePosition = 60,
                                                           PressStartTorquePosition = 65, PressTargetPosition = 55,
                                                           PressTimeMs = 500, PressTorquePercentage = 100, PressVelocity = 10,
                                                           VolumeInUl = 200, XOffset = 0, YOffset = 0 };
            _labware_database.AddTipBox( tipbox, tip_properties);
            TipProperties updated_properties = new TipProperties { CurrentLimit = 1, LengthInMm = 21, PressAcceleration = 2,
                                                               PressMaxAcceptablePosition = 51, PressMinAcceptablePosition = 61,
                                                               PressStartTorquePosition = 66, PressTargetPosition = 56,
                                                               PressTimeMs = 501, PressTorquePercentage = 101, PressVelocity = 11,
                                                               VolumeInUl = 201, XOffset = 1, YOffset = 1 };
            tipbox.TipProperties = updated_properties;
            _labware_database.UpdateTipBox( tipbox);
            // check tip press parameters
            TipBox check = _labware_database["update tipbox"] as TipBox;
            Assert.IsNotNull( check);
            Assert.AreEqual( 1, check.TipProperties.CurrentLimit);
            Assert.AreEqual( tipbox.Id, check.Id);
            Assert.AreEqual( 21, check.TipProperties.LengthInMm);
            Assert.AreEqual( 2, check.TipProperties.PressAcceleration);
            Assert.AreEqual( 51, check.TipProperties.PressMaxAcceptablePosition);
            Assert.AreEqual( 61, check.TipProperties.PressMinAcceptablePosition);
            Assert.AreEqual( 66, check.TipProperties.PressStartTorquePosition);
            Assert.AreEqual( 56, check.TipProperties.PressTargetPosition);
            Assert.AreEqual( 501, check.TipProperties.PressTimeMs);
            Assert.AreEqual( 101, check.TipProperties.PressTorquePercentage);
            Assert.AreEqual( 11, check.TipProperties.PressVelocity);
            Assert.AreEqual( 201, check.TipProperties.VolumeInUl);
            Assert.AreEqual( 1, check.TipProperties.XOffset);
            Assert.AreEqual( 1, check.TipProperties.YOffset);
        }

        [TestMethod]
        public void TestAddTipBox()
        {
            TipBox tipbox = new TipBox( "unit test tipbox", "", "");
            tipbox.Properties[LabwarePropertyNames.NumberOfWells] = 96;
            tipbox.Properties[LabwarePropertyNames.Thickness] = 14.1;
            TipProperties tip_properties = new TipProperties { CurrentLimit = 0, LengthInMm = 20, PressAcceleration = 1,
                                                           PressMaxAcceptablePosition = 50, PressMinAcceptablePosition = 60,
                                                           PressStartTorquePosition = 65, PressTargetPosition = 55,
                                                           PressTimeMs = 500, PressTorquePercentage = 100, PressVelocity = 10,
                                                           VolumeInUl = 200, XOffset = 0, YOffset = 0 };
            _labware_database.AddTipBox( tipbox, tip_properties);
            // check tip press parameters
            TipBox check = _labware_database["unit test tipbox"] as TipBox;
            Assert.IsNotNull( check);
            Assert.AreEqual( tipbox.Id, check.Id);
            Assert.AreEqual( 20, check.TipProperties.LengthInMm);
            Assert.AreEqual( 1, check.TipProperties.PressAcceleration);
            Assert.AreEqual( 50, check.TipProperties.PressMaxAcceptablePosition);
            Assert.AreEqual( 60, check.TipProperties.PressMinAcceptablePosition);
            Assert.AreEqual( 65, check.TipProperties.PressStartTorquePosition);
            Assert.AreEqual( 55, check.TipProperties.PressTargetPosition);
            Assert.AreEqual( 500, check.TipProperties.PressTimeMs);
            Assert.AreEqual( 100, check.TipProperties.PressTorquePercentage);
            Assert.AreEqual( 10, check.TipProperties.PressVelocity);
            Assert.AreEqual( 200, check.TipProperties.VolumeInUl);
            Assert.AreEqual( 0, check.TipProperties.XOffset);
            Assert.AreEqual( 0, check.TipProperties.YOffset);
            Assert.AreEqual( 0, check.TipProperties.CurrentLimit);
        }

        [TestMethod]
        public void TestAddTeachingJigLabwareName()
        {
            // create the labware
            Labware labware = new Labware( "teaching jig", "", "");
            try {
                _labware_database.AddLabware( labware);
                Assert.Fail( "AddLabware should have thrown an exception");
            } catch( ReservedLabwareException) {
                // good
            } catch( Exception) {
                Assert.Fail( "AddLabware threw the wrong type of exception");
            }
        }

        [TestMethod]
        public void TestTipBoxPropertiesCopyConstructor()
        {
            ITipBoxProperties.ITipPressParameters master_tip_press_parameters = new ITipBoxProperties.ITipPressParameters( 5, 6, 7, 8, 9);
            ITipBoxProperties master = new ITipBoxProperties( 0, 1, 2, 3, 4, master_tip_press_parameters);
            // make a copy
            ITipBoxProperties copy = new ITipBoxProperties( master);
            Assert.AreEqual( master, copy);
        }
    }
}
