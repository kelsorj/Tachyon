using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BioNex.SynapsisPrototype;
using System.Data.SQLite;
using DeviceManagerDatabase;

namespace DeviceManagerDatabaseMSTests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class DeviceManagerDatabaseTests
    {
        public DeviceManagerDatabaseTests()
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

        BioNex.SynapsisPrototype.DeviceManagerDatabase _db;
        const string database_filename = "test.s3db";
        SQLiteConnection _connection;

        // constants for database test properties
        const string serial_number = "serial number";
        const string database_location = "database location";

        [TestInitialize]
        public void Init()
        {
            CreateTestDatabase();
        }

        private void CreateTestDatabase()
        {
            try
            {
                _connection = new SQLiteConnection(String.Format("Data Source={0}", database_filename));
                SQLiteConnection.CreateFile(database_filename);
                _connection.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = _connection;

                // create the device_types table
                cmd.CommandText = "CREATE TABLE [device_types] ( [id] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT, [product_name] TEXT  NOT NULL, [company_name] TEXT  NOT NULL, CONSTRAINT my_constraint UNIQUE (product_name,company_name))";
                cmd.ExecuteNonQuery();

                // create the device table
                cmd.CommandText = "CREATE TABLE [devices] ([id] INTEGER  PRIMARY KEY AUTOINCREMENT NOT NULL,[name] TEXT  UNIQUE NOT NULL,[device_type_id] INTEGER NOT NULL,[disabled] BOOLEAN, FOREIGN KEY (device_type_id) REFERENCES device_types(id))";
                cmd.ExecuteNonQuery();

                // create the device_properties table
                cmd.CommandText = "CREATE TABLE [device_properties] ([device_id] INTEGER  NOT NULL,[key] TEXT  NOT NULL,[value] TEXT  NOT NULL,[type] TEXT  NOT NULL,CONSTRAINT property_constraint UNIQUE (device_id,key))";
                cmd.ExecuteNonQuery();

                // create the tip_properties table
                // create the tip_press_properties table
                cmd.Dispose();
                _db = new BioNex.SynapsisPrototype.DeviceManagerDatabase(database_filename);
            }
            catch (SQLiteException)
            {
                throw;
            }
            finally
            {
                _connection.Close();
            }
        }

        [TestCleanup]
        public void Dispose()
        {
            _connection.Dispose();
        }

        [TestMethod]
        public void TestAddAndGetDevice()
        {
            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add(serial_number, "test0001");
            properties.Add(database_location, "c:\\test.s3db");
            _db.AddDevice("BioNex", "RobotInterface", "add test", false, properties);
            IDictionary<string, string> check = _db.GetProperties("BioNex", "RobotInterface", "add test");
            Assert.AreEqual("test0001", check[serial_number]);
            Assert.AreEqual("c:\\test.s3db", check[database_location]);
            Assert.IsTrue(_db.GetDeviceId("BioNex", "RobotInterface", "add test") != 0);
        }

        [TestMethod]
        public void TestUpdateDevice()
        {
            // add the device first
            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add(serial_number, "test0001");
            properties.Add(database_location, "c:\\test.s3db");
            _db.AddDevice("BioNex", "RobotInterface", "update test", false, properties);
            // make changes
            properties[serial_number] = "test0002";
            properties.Add("another property", "test");
            _db.UpdateDevice("BioNex", "RobotInterface", "update test", properties);
            IDictionary<string, string> check = _db.GetProperties("BioNex", "RobotInterface", "update test");
            Assert.AreEqual("test0002", check[serial_number]);
            Assert.AreEqual("c:\\test.s3db", check[database_location]);
            Assert.AreEqual("test", check["another property"]);
        }

        [TestMethod]
        public void TestDisableDevice()
        {
            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add(serial_number, "test0001");
            properties.Add(database_location, "c:\\test.s3db");
            _db.AddDevice("BioNex", "RobotInterface", "add test", false, properties);
            _db.DisableDevice("BioNex", "RobotInterface", "add test", true);
        }

        [TestMethod]
        public void TestRenameDeviceProperty()
        {
            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("key", "test0001");
            _db.AddDevice("BioNex", "RobotInterface", "rename property test", false, properties);
            _db.RenameDeviceProperty("BioNex", "RobotInterface", "rename property test", "key", "key2");
            Assert.IsFalse(_db.GetProperties("BioNex", "RobotInterface", "rename property test").ContainsKey("key"));
            Assert.IsTrue(_db.GetProperties("BioNex", "RobotInterface", "rename property test").ContainsKey("key2"));
            Assert.AreEqual("test0001", _db.GetProperties("BioNex", "RobotInterface", "rename property test")["key2"]);
        }

        [TestMethod]
        public void TestUpdateDeviceProperty()
        {
            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("key", "test0001");
            _db.AddDevice("BioNex", "RobotInterface", "update property test", false, properties);
            _db.UpdateDeviceProperty("BioNex", "RobotInterface", "update property test", "key", "new value");
            Assert.AreEqual("new value", _db.GetProperties("BioNex", "RobotInterface", "update property test")["key"]);
        }

        [TestMethod]
        public void TestAddDeviceProperty()
        {
            IDictionary<string, string> properties = new Dictionary<string, string>();
            _db.AddDevice("BioNex", "Robot", "add property test", false, properties);
            _db.AddDeviceProperty("BioNex", "Robot", "add property test", "property name", "property value");
            properties = _db.GetProperties("BioNex", "Robot", "add property test");
            Assert.AreEqual(1, properties.Count());
            Assert.AreEqual("property name", properties.First().Key);
            Assert.AreEqual("property value", properties["property name"]);
        }

        [TestMethod]
        public void TestDeleteDeviceProperty()
        {
            IDictionary<string, string> properties = new Dictionary<string, string>();
            _db.AddDevice("BioNex", "Robot", "add property test", false, properties);
            _db.AddDeviceProperty("BioNex", "Robot", "add property test", "property name", "property value");
            _db.DeleteDeviceProperty("BioNex", "Robot", "add property test", "property name");
            properties = _db.GetProperties("BioNex", "Robot", "add property test");
            Assert.AreEqual(0, properties.Count());
        }

        [TestMethod]
        public void TestRenameDevice()
        {
            // add the device first
            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add(serial_number, "test0003");
            properties.Add(database_location, "c:\\test.s3db");
            _db.AddDevice("BioNex", "RobotInterface", "rename test", false, properties);
            // rename the device
            _db.RenameDevice("BioNex", "RobotInterface", "rename test", "renamed test");
            try
            {
                _db.GetProperties("BioNex", "RobotInterface", "rename test");
                Assert.Fail( "GetProperties should have thrown an exception");
            }
            catch (DeviceNameNotFoundException)
            {
                // good, we caught the right exception
            }
            catch (Exception)
            {
                // bad, we didn't catch the right kind of exception
                Assert.Fail( "GetProperties did not throw the right kind of exception");
            }
            IDictionary<string, string> check = _db.GetProperties("BioNex", "RobotInterface", "renamed test");
            Assert.AreEqual("test0003", check[serial_number]);
            Assert.AreEqual("c:\\test.s3db", check[database_location]);
        }

        [TestMethod]
        public void TestDeleteDevice()
        {
            // add the device first
            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add(serial_number, "test0003");
            properties.Add(database_location, "c:\\test.s3db");
            _db.AddDevice("BioNex", "RobotInterface", "delete test", false, properties);
            // delete the device
            _db.DeleteDevice("BioNex", "RobotInterface", "delete test");
            try
            {
                _db.GetProperties("BioNex", "RobotInterface", "delete test");
                Assert.Fail( "GetProperties didn't throw an exception when it should have");
            }
            catch (DeviceNameNotFoundException)
            {
                // good, we caught the right kind of exception
            }
            catch (Exception)
            {
                // bad, didn't throw the right kind of exception
                Assert.Fail("GetProperties didn't throw the right kind of exception");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(DeviceNameNotFoundException))]
        public void TestUpdateNonExistantDevice()
        {
            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add(serial_number, "test0004");
            _db.UpdateDevice("BioNex", "RobotInterface", "update non-existant test", properties);
        }

        [TestMethod]
        public void TestReloadDeviceDatabase()
        {
            // here, we want to add devices to the database, and then get a list of devices
            // back from the database to confirm that everything can be retrieved.
            IDictionary<string, string> properties = new Dictionary<string, string>();
            _db.AddDevice("BioNex", "Speedy Robot", "speedy1", false, properties);
            _db.AddDevice("BioNex", "Speedy Robot", "speedy2", false, properties);
            IEnumerable<DeviceInfo> devices = _db.GetAllDeviceInfo();
            Assert.AreEqual(2, devices.Count());
            Assert.AreEqual(2, (from d in devices where d.Disabled == false select d).ToArray().Count());

        }

        /// <summary>
        /// this is a pretty weak unit test.  If I want it to work, I'd have to copy some plugins
        /// to the output folder, which I don't want to do because I don't want mocks ending up
        /// in the final build.
        /// </summary>
        [TestMethod]
        public void TestGetAllDeviceInfo()
        {
            IEnumerable<DeviceInfo> devices = _db.GetAllDeviceInfo();
            Assert.AreEqual(0, devices.Count());
        }

        [TestMethod]
        public void TestAddDeviceType()
        {
            int id = _db.AddDeviceType("company", "product");
            Assert.AreEqual(id, _db.GetDeviceTypeId("company", "product"));
        }
    }
}
