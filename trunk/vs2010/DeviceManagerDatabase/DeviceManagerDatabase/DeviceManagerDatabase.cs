using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.DeviceInterfaces;
using System.Data.SQLite;
using System.Data;
using DeviceManagerDatabase;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

// hey, this is nice...  kind of like friend classes in C++
[assembly:InternalsVisibleTo( "DeviceManagerDatabaseMSTests")]

namespace BioNex.SynapsisPrototype
{
    public class DeviceTypeNotFoundException : ApplicationException
    {
        public string CompanyName { get; private set; }
        public string ProductName { get; private set; }

        public DeviceTypeNotFoundException( string company_name, string product_name)
        {
            CompanyName = company_name;
            ProductName = product_name;
        }
    }

    public class DeviceNameNotFoundException : ApplicationException
    {
        public string CompanyName { get; private set; }
        public string ProductName { get; private set; }
        public string DeviceName { get; private set; }

        public DeviceNameNotFoundException( string company_name, string product_name, string device_name)
        {
            CompanyName = company_name;
            ProductName = product_name;
            DeviceName = device_name;
        }
    }

    public class DeviceManagerDatabase
    {
        private SQLiteConnection Connection { get; set; }

        // parameterized queries
        /// <summary>
        /// Adds a new device instance to the DEVICES table
        /// </summary>
        private SQLiteCommand AddDeviceQuery { get; set; }

        /// <summary>
        /// Sets / clears the disabled flag in the DEVICES table
        /// </summary>
        private SQLiteCommand DisableDeviceQuery { get; set; }
        /// <summary>
        /// Adds a new device/product type to the DEVICE_TYPES table
        /// </summary>
        private SQLiteCommand AddDeviceTypeQuery { get; set; }
        /// <summary>
        /// Gets the device type id from the DEVICE_TYPES table
        /// </summary>
        private SQLiteCommand GetDeviceTypeIdQuery { get; set; }
        /// <summary>
        /// Gets the device id from the DEVICES table
        /// </summary>
        private SQLiteCommand GetAllDevicesQuery { get; set; }
        private SQLiteCommand GetDeviceIdQuery { get; set; }
        private SQLiteCommand DeleteDeviceQuery { get; set; }
        private SQLiteCommand RenameDeviceQuery { get; set; }
        private SQLiteCommand UpdateDeviceQuery { get; set; }
        private SQLiteCommand GetDevicePropertiesQuery { get; set; }

        private SQLiteCommand RenameDevicePropertyQuery { get; set; }
        private SQLiteCommand UpdateDevicePropertyQuery { get; set; }
        private SQLiteCommand AddDevicePropertyQuery { get; set; }
        private SQLiteCommand DeleteDevicePropertyQuery { get; set; }

        private ObservableCollection<DeviceInfo> Devices { get; set; }

        public DeviceManagerDatabase()
            : this( BioNex.Shared.Utils.FileSystem.GetModulePath() + "\\devices.s3db")
        {
        }

        public DeviceManagerDatabase( string database_path)
        {
            Connection = new SQLiteConnection( String.Format( "Data Source={0}", database_path));
            Devices = new ObservableCollection<DeviceInfo>();
            Init();
        }

        private void Init()
        {
            InitializeQueries();
        }

        private void InitializeQueries()
        {
            const string SelectIdFromDeviceTypes = "SELECT id FROM device_types WHERE company_name=@company_name AND product_name=@product_name COLLATE NOCASE";

            GetAllDevicesQuery = new SQLiteCommand( "SELECT name,product_name,company_name,disabled FROM devices,device_types WHERE devices.device_type_id = device_types.id;");
            GetAllDevicesQuery.Connection = Connection;

            // parameters are @name and @device_type_id
            // call ExecuteScalar to get the device id back
            AddDeviceQuery = new SQLiteCommand( "INSERT INTO devices (name,device_type_id,disabled) VALUES (@name,@device_type_id,@disabled); SELECT last_insert_rowid() AS device_id;");
            AddDeviceQuery.Connection = Connection;

            // parameters are @product_name and @company_name
            // call ExecuteScalar to get the ID back
            GetDeviceTypeIdQuery = new SQLiteCommand( SelectIdFromDeviceTypes);
            GetDeviceTypeIdQuery.Connection = Connection;

            // parameters are @name, @company_name, and @product_name
            GetDeviceIdQuery = new SQLiteCommand( "SELECT id FROM devices where name=@name and device_type_id=(" + SelectIdFromDeviceTypes + ");");
            GetDeviceIdQuery.Connection = Connection;

            // parameters are @product_name and @company_name
            AddDeviceTypeQuery = new SQLiteCommand( "INSERT INTO device_types (product_name,company_name) VALUES (@product_name,@company_name); SELECT last_insert_rowid() AS device_id;");
            AddDeviceTypeQuery.Connection = Connection;

            // parameters are @disabled, @name, @company_name, and @product_name
            DisableDeviceQuery = new SQLiteCommand( "UPDATE devices SET disabled=@disabled where name=@name AND device_type_id=(" + SelectIdFromDeviceTypes + ");");
            DisableDeviceQuery.Connection = Connection;

            // parameters are @name, @company_name, and @product_name
            DeleteDeviceQuery = new SQLiteCommand( "DELETE FROM devices WHERE name=@name AND device_type_id=(" + SelectIdFromDeviceTypes + ");");
            DeleteDeviceQuery.Connection = Connection;

            // parameters are @new_name, @old_name, @company_name, and @product_name
            RenameDeviceQuery = new SQLiteCommand( "UPDATE devices SET name=@new_name where name=@old_name and device_type_id=(" + SelectIdFromDeviceTypes + ");");
            RenameDeviceQuery.Connection = Connection;

            // the actual command text for updating will be coded later because
            // the number of update arguments can vary wildly
            UpdateDeviceQuery = new SQLiteCommand();
            UpdateDeviceQuery.Connection = Connection;

            GetDevicePropertiesQuery = new SQLiteCommand( "SELECT key,value FROM device_properties WHERE device_id=@device_id;");
            GetDevicePropertiesQuery.Connection = Connection;

            RenameDevicePropertyQuery = new SQLiteCommand( "UPDATE device_properties SET key=@new_property_name WHERE key=@old_property_name AND device_id=@device_id;");
            RenameDevicePropertyQuery.Connection = Connection;

            UpdateDevicePropertyQuery = new SQLiteCommand( "UPDATE device_properties SET value=@value WHERE key=@key AND device_id=@device_id;");
            UpdateDevicePropertyQuery.Connection = Connection;

            AddDevicePropertyQuery = new SQLiteCommand( "INSERT INTO device_properties (device_id, key, value, type) VALUES (@device_id, @key, @value, 'string');");
            AddDevicePropertyQuery.Connection = Connection;

            DeleteDevicePropertyQuery = new SQLiteCommand( "DELETE FROM device_properties WHERE device_id=@device_id AND key=@key;");
            DeleteDevicePropertyQuery.Connection = Connection;
        }

        /// <summary>
        /// Queries the database for all plugin instance names.  Does NOT get each devices' properties.
        /// </summary>
        public ObservableCollection<DeviceInfo> GetAllDeviceInfo()
        {
            Devices.Clear();
            try {
                Connection.Open();
                SQLiteDataReader reader = GetAllDevicesQuery.ExecuteReader();
                DataTable dt = new DataTable();
                dt.Load( reader);
                reader.Close();
                Connection.Close();
                foreach( DataRow dr in dt.Rows) {
                    string company_name = dr["company_name"].ToString();
                    string product_name = dr["product_name"].ToString();
                    string instance_name = dr["name"].ToString();
                    bool disabled = bool.Parse( dr["disabled"].ToString());
                    // get the device's properties
                    IDictionary<string,string> properties = GetProperties( company_name, product_name, instance_name);
                    Devices.Add( new DeviceInfo( company_name, product_name, instance_name, disabled, properties));
                }
            } finally {
                Connection.Close();
            }
            return Devices;
        }

        internal int AddDeviceType( string company_name, string product_name)
        {
            int device_type_id = 0;
            SQLiteTransaction transaction = null;
            try {
                AddDeviceQuery.Parameters.Clear();
                Connection.Open();

                transaction = Connection.BeginTransaction();
                // populate the parameters for the query
                AddDeviceTypeQuery.Parameters.AddWithValue( "@product_name", product_name);
                AddDeviceTypeQuery.Parameters.AddWithValue( "@company_name", company_name);
                device_type_id = int.Parse( AddDeviceTypeQuery.ExecuteScalar().ToString());
                transaction.Commit();
            } catch( SQLiteException) {
                transaction.Rollback();
                throw; // figure out the right way to deal with this later
            } finally {
                Connection.Close();
            }
            return device_type_id;
        }

        /// <summary>
        /// Get the device type id from the DEVICE_TYPES table.  Needed any time we want
        /// to add a device to the DEVICES table, since we have to specify a device type id.
        /// </summary>
        /// <param name="company_name"></param>
        /// <param name="product_name"></param>
        /// <returns></returns>
        public int GetDeviceTypeId( string company_name, string product_name)
        {
            int device_type_id = 0;
            try {
                GetDeviceTypeIdQuery.Parameters.Clear();
                Connection.Open();

                // get the device type id
                GetDeviceTypeIdQuery.Parameters.AddWithValue( "@company_name", company_name);
                GetDeviceTypeIdQuery.Parameters.AddWithValue( "@product_name", product_name);
                object o = GetDeviceTypeIdQuery.ExecuteScalar();
                if( o == null)
                    throw new DeviceTypeNotFoundException( company_name, product_name);
                device_type_id = int.Parse( o.ToString());
            } catch( SQLiteException) {
                throw;
            } finally {
                Connection.Close();
            }
            return device_type_id;
        }

        public void AddDevice( string company_name, string product_name, string device_name, bool disabled, IDictionary<string,string> properties)
        {
            // if the device doesn't exist, add it
            int device_type_id = 0;
            try {
                device_type_id = GetDeviceTypeId( company_name, product_name);
            } catch( DeviceTypeNotFoundException) {
                device_type_id = AddDeviceType( company_name, product_name);
            }

            SQLiteTransaction transaction = null;
            try {
                AddDeviceQuery.Parameters.Clear();
                Connection.Open();

                transaction = Connection.BeginTransaction();
                // first add the device to the DEVICES table
                AddDeviceQuery.Parameters.AddWithValue( "@name", device_name);
                AddDeviceQuery.Parameters.AddWithValue( "@device_type_id", device_type_id);
                AddDeviceQuery.Parameters.AddWithValue( "@disabled", disabled);
                int device_id = int.Parse( AddDeviceQuery.ExecuteScalar().ToString());
                // now create and execute the command to add the parameters to the DEVICE_PROPERTIES table
                foreach( KeyValuePair<string,string> kvp in properties) {
                    string sql = String.Format( "INSERT OR REPLACE INTO device_properties (device_id,key,value,type) VALUES ({0},\"{1}\",\"{2}\",\"string\");", device_id, kvp.Key, kvp.Value);
                    UpdateDeviceQuery.CommandText = sql;
                    UpdateDeviceQuery.ExecuteNonQuery();
                }
                transaction.Commit();
            } catch( SQLiteException) {
                transaction.Rollback();
                throw;
            } finally {
                Connection.Close();
            }
        }

        public void DisableDevice( string company_name, string product_name, string device_name, bool disable=true)
        {
            SQLiteTransaction transaction = null;
            try {
                DisableDeviceQuery.Parameters.Clear();
                Connection.Open();

                transaction = Connection.BeginTransaction();
                DisableDeviceQuery.Parameters.AddWithValue( "@name", device_name);
                DisableDeviceQuery.Parameters.AddWithValue( "@company_name", company_name);
                DisableDeviceQuery.Parameters.AddWithValue( "@product_name", product_name);
                DisableDeviceQuery.Parameters.AddWithValue( "@disabled", disable);
                DisableDeviceQuery.ExecuteNonQuery();
                transaction.Commit();
            } catch( SQLiteException) {
                transaction.Rollback();
                throw;
            } finally {
                Connection.Close();
            }
        }

        internal int GetDeviceId( string company_name, string product_name, string device_name)
        {
            int device_id = 0;

            try {
                GetDeviceIdQuery.Parameters.Clear();
                Connection.Open();
                // get the device type id
                GetDeviceIdQuery.Parameters.AddWithValue( "@name", device_name);
                GetDeviceIdQuery.Parameters.AddWithValue( "@company_name", company_name);
                GetDeviceIdQuery.Parameters.AddWithValue( "@product_name", product_name);
                object o = GetDeviceIdQuery.ExecuteScalar();
                if( o == null)
                    throw new DeviceNameNotFoundException( company_name, product_name, device_name);
                device_id = int.Parse( GetDeviceIdQuery.ExecuteScalar().ToString());
            } catch( SQLiteException) {
                throw;
            } finally {
                Connection.Close();
            }

            return device_id;
        }

        public void UpdateDevice( string company_name, string product_name, string device_name, IDictionary<string,string> properties)
        {
            SQLiteTransaction transaction = null;
            try {
                int device_id = GetDeviceId( company_name, product_name, device_name);
                Connection.Open();
                transaction = Connection.BeginTransaction();

                foreach( KeyValuePair<string,string> kvp in properties) {
                    string sql = String.Format( "INSERT OR REPLACE INTO device_properties (device_id,key,value,type) VALUES ({0},\"{1}\",\"{2}\",\"string\")", device_id, kvp.Key, kvp.Value);
                    UpdateDeviceQuery.CommandText = sql;
                    UpdateDeviceQuery.ExecuteNonQuery();
                }
                transaction.Commit();
            } catch( SQLiteException) {
                transaction.Rollback();
                throw;
            } finally {
                Connection.Close();
            }
        }

        public void DeleteDevice( string company_name, string product_name, string device_name)
        {
            SQLiteTransaction transaction = null;
            
            try {
                DeleteDeviceQuery.Parameters.Clear();
                Connection.Open();
                transaction = Connection.BeginTransaction();
                DeleteDeviceQuery.Parameters.AddWithValue( "@name", device_name);
                DeleteDeviceQuery.Parameters.AddWithValue( "@company_name", company_name);
                DeleteDeviceQuery.Parameters.AddWithValue( "@product_name", product_name);
                DeleteDeviceQuery.ExecuteNonQuery();
                transaction.Commit();
            } catch( SQLiteException) {
                transaction.Rollback();
                throw;
            } finally {
                Connection.Close();
            }
        }

        public void RenameDevice( string company_name, string product_name, string old_name, string new_name)
        {
            SQLiteTransaction transaction = null;

            try {
                RenameDeviceQuery.Parameters.Clear();
                Connection.Open();
                transaction = Connection.BeginTransaction();
                RenameDeviceQuery.Parameters.AddWithValue( "@company_name", company_name);
                RenameDeviceQuery.Parameters.AddWithValue( "@product_name", product_name);
                RenameDeviceQuery.Parameters.AddWithValue( "@old_name", old_name);
                RenameDeviceQuery.Parameters.AddWithValue( "@new_name", new_name);
                RenameDeviceQuery.ExecuteNonQuery();
                transaction.Commit();
            } catch( SQLiteException) {
                transaction.Rollback();
                throw;
            } finally {
                Connection.Close();
            }
        }

        public IDictionary<string,string> GetProperties( string company_name, string product_name, string instance_name)
        {
            int device_id = GetDeviceId( company_name, product_name, instance_name);
            IDictionary<string,string> properties = new Dictionary<string,string>();

            try {
                GetDevicePropertiesQuery.Parameters.Clear();

                Connection.Open();
                GetDevicePropertiesQuery.Parameters.AddWithValue( "@company_name", company_name);
                GetDevicePropertiesQuery.Parameters.AddWithValue( "@product_name", product_name);
                GetDevicePropertiesQuery.Parameters.AddWithValue( "@device_id", device_id);
                SQLiteDataReader reader = GetDevicePropertiesQuery.ExecuteReader();
                DataTable dt = new DataTable();
                dt.Load( reader);
                reader.Close();
                foreach( DataRow dr in dt.Rows)
                    properties.Add( dr["key"].ToString(), dr["value"].ToString());
            } catch( SQLiteException) {
                throw;
            } finally { 
                Connection.Close();
            }
            return properties;
        }

        public void RenameDeviceProperty( string company_name, string product_name, string instance_name, string old_property_name, string new_property_name)
        {
            SQLiteTransaction transaction = null;

            try {
                int device_id = GetDeviceId( company_name, product_name, instance_name);
                RenameDevicePropertyQuery.Parameters.Clear();
                Connection.Open();
                transaction = Connection.BeginTransaction();
                RenameDevicePropertyQuery.Parameters.AddWithValue( "@device_id", device_id);
                RenameDevicePropertyQuery.Parameters.AddWithValue( "@old_property_name", old_property_name);
                RenameDevicePropertyQuery.Parameters.AddWithValue( "@new_property_name", new_property_name);
                RenameDevicePropertyQuery.ExecuteNonQuery();
                transaction.Commit();
            } catch( SQLiteException) {
                transaction.Rollback();
                throw;
            } finally {
                Connection.Close();
            }
        }

        public void AddDeviceProperty( string company_name, string product_name, string instance_name, string property_name, string property_value)
        {
            SQLiteTransaction transaction = null;

            try {
                int device_id = GetDeviceId( company_name, product_name, instance_name);
                AddDevicePropertyQuery.Parameters.Clear();
                Connection.Open();
                transaction = Connection.BeginTransaction();
                AddDevicePropertyQuery.Parameters.AddWithValue( "@device_id", device_id);
                AddDevicePropertyQuery.Parameters.AddWithValue( "@key", property_name);
                AddDevicePropertyQuery.Parameters.AddWithValue( "@value", property_value);
                AddDevicePropertyQuery.ExecuteNonQuery();
                transaction.Commit();
            } catch( SQLiteException) {
                transaction.Rollback();
                throw;
            } finally {
                Connection.Close();
            }
        }

        public void DeleteDeviceProperty( string company_name, string product_name, string instance_name, string property_name)
        {
            SQLiteTransaction transaction = null;

            try {
                int device_id = GetDeviceId( company_name, product_name, instance_name);
                DeleteDevicePropertyQuery.Parameters.Clear();
                Connection.Open();
                transaction = Connection.BeginTransaction();
                DeleteDevicePropertyQuery.Parameters.AddWithValue( "@device_id", device_id);
                DeleteDevicePropertyQuery.Parameters.AddWithValue( "@key", property_name);
                DeleteDevicePropertyQuery.ExecuteNonQuery();
                transaction.Commit();
            } catch( SQLiteException) {
                transaction.Rollback();
                throw;
            } finally {
                Connection.Close();
            }
        }

        public void UpdateDeviceProperty( string company_name, string product_name, string instance_name, string property_name, string property_value)
        {
            SQLiteTransaction transaction = null;

            try {
                int device_id = GetDeviceId( company_name, product_name, instance_name);
                UpdateDevicePropertyQuery.Parameters.Clear();
                Connection.Open();
                transaction = Connection.BeginTransaction();
                UpdateDevicePropertyQuery.Parameters.AddWithValue( "@device_id", device_id);
                UpdateDevicePropertyQuery.Parameters.AddWithValue( "@key", property_name);
                UpdateDevicePropertyQuery.Parameters.AddWithValue( "@value", property_value);
                UpdateDevicePropertyQuery.ExecuteNonQuery();
                transaction.Commit();
            } catch( SQLiteException) {
                transaction.Rollback();
                throw;
            } finally {
                Connection.Close();
            }
        }
    }
}
