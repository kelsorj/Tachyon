using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;

[assembly:InternalsVisibleTo("InventoryManagementMSTests")]

namespace BioNex.Shared.InventoryManagement
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IInventoryManagement))]
    public class Inventory : IInventoryManagement
    {
        private readonly object _connection_lock = new object();
        private SQLiteConnection _connection { get; set; }
        /// <summary>
        /// Different devices have their own plate layouts, so we need to let the
        /// plugin inform the InventoryManagement library of these details.  For
        /// examples, the Hive uses (rack, slot) for its plate "coordinates", but
        /// the lazy susan could use (rack, slot, side).
        /// </summary>
        private List<string> StorageLocationsSchema { get; set; }

        // parameterized queries
        private SQLiteCommand GetPlateIdQuery { get; set; }
        private SQLiteCommand GetLocationQuery { get; set; }
        private SQLiteCommand GetInventoryQuery { get; set; }
        private SQLiteCommand LoadQuery { get; set; }
        private SQLiteCommand AddToStorageLocationsQuery { get; set; }
        private SQLiteCommand RemoveQuery { get; set; }
        private SQLiteCommand RemoveFromStorageLocationsQuery { get; set; }
        private SQLiteCommand DeleteQuery { get; set; }
        private SQLiteCommand GetVolumeQuery { get; set; }
        private SQLiteCommand GetAllVolumesQuery { get; set; }
        private SQLiteCommand GetAllVolumeDeltasQuery { get; set; }
        private SQLiteCommand SetInitialVolumeQuery { get; set; }
        private SQLiteCommand SetDeltaVolumeQuery { get; set; }

        //---------------------------------------------------------------------
        private void InitializeQueries()
        {
            // pass in @barcode
            GetPlateIdQuery = new SQLiteCommand( "SELECT id from plates WHERE barcode=@barcode;");
            GetPlateIdQuery.Connection = _connection;
            // pass in @barcode
            GetLocationQuery = new SQLiteCommand( "SELECT * FROM plates,storage_locations WHERE plates.barcode=@barcode AND plates.id=storage_locations.plate_id;");
            GetLocationQuery.Connection = _connection;
            // no parameters
            GetInventoryQuery = new SQLiteCommand( "SELECT * FROM plates,storage_locations WHERE plates.id=storage_locations.plate_id;");
            GetInventoryQuery.Connection = _connection;
            // pass in @barcode
            // NOTE: do NOT use INSERT OR REPLACE, because this actually will write a new
            //       plate ID, which will screw up volume tables when we eventually support them.
            LoadQuery = new SQLiteCommand( "INSERT INTO plates (barcode) VALUES (@barcode); SELECT last_insert_rowid() AS record_id;");
            LoadQuery.Connection = _connection;
            // pass in @plate_id, but also whatever is specified by the device-specific schema
            AddToStorageLocationsQuery = new SQLiteCommand();
            AddToStorageLocationsQuery.Connection = _connection;
            // pass in @plate_id
            RemoveQuery = new SQLiteCommand( "DELETE FROM storage_locations WHERE plate_id=@plate_id;");
            RemoveQuery.Connection = _connection;
            // pass in @plate_id
            RemoveFromStorageLocationsQuery = new SQLiteCommand( "UPDATE storage_locations SET loaded=\"false\" WHERE plate_id=@plate_id;");
            RemoveFromStorageLocationsQuery.Connection = _connection;
            // pass in @barcode
            DeleteQuery = new SQLiteCommand( "DELETE FROM plates WHERE barcode=@barcode;");
            DeleteQuery.Connection = _connection;
            // pass in @barcode, @reservoir_name
            GetVolumeQuery = new SQLiteCommand( @"SELECT plates.barcode,volumes.reservoir_name,volumes.baseline_volume_uL,volumes.delta_volume_uL
                                                  FROM plates LEFT OUTER JOIN volumes
                                                  ON plates.id=volumes.plate_id
                                                  WHERE volumes.reservoir_name=@reservoir_name
                                                  AND plates.barcode=@barcode;");
            GetVolumeQuery.Connection = _connection;
            // pass in @barcode
            GetAllVolumesQuery = new SQLiteCommand( @"SELECT reservoir_name,baseline_volume_uL,delta_volume_uL 
                                                      FROM plates,volumes 
                                                      WHERE barcode=@barcode AND plates.id=volumes.plate_id;");
            GetAllVolumesQuery.Connection = _connection;
            // pass in @barcode
            GetAllVolumeDeltasQuery = new SQLiteCommand( @"SELECT plates.barcode,volumes.reservoir_name,volumes.baseline_volume_uL,volumes.delta_volume_uL
                                                           FROM plates LEFT OUTER JOIN volumes
                                                           ON plates.id=volumes.plate_id
                                                           WHERE plates.barcode=@barcode;");
            GetAllVolumeDeltasQuery.Connection = _connection;
            // pass in @initial_volume, @plate_id, @reservoir_name
            /*
            SetInitialVolumeQuery = new SQLiteCommand( @"UPDATE volumes SET baseline_volume_uL=@baseline_volume_uL,delta_volume_uL=0
                                                         WHERE plate_id=@plate_id
                                                         AND reservoir_name=@reservoir_name;");
             */
            SetInitialVolumeQuery = new SQLiteCommand( @"INSERT OR REPLACE INTO volumes (plate_id,reservoir_name,baseline_volume_uL,delta_volume_uL)
                                                         VALUES (@plate_id,@reservoir_name,@baseline_volume_uL,0);");
            SetInitialVolumeQuery.Connection = _connection;
            // pass in @volume_delta, @plate_id, @reservoir_name
            SetDeltaVolumeQuery = new SQLiteCommand( @"UPDATE volumes SET delta_volume_uL=@delta_volume_uL
                                                       WHERE plate_id=@plate_id 
                                                       AND reservoir_name=@reservoir_name;");
            SetDeltaVolumeQuery.Connection = _connection;
        }
        //---------------------------------------------------------------------
        #region IInventoryManagement Members

        public void CreateDatabase( string database_path, List<string> storage_locations_schema)
        {
            lock(_connection_lock){
                Debug.Assert( _connection == null);
                _connection = new SQLiteConnection( String.Format( "Data Source={0}", database_path));
                string directory_name = database_path.GetDirectoryFromFilePath();
                if( !Directory.Exists( directory_name))
                    Directory.CreateDirectory( directory_name);
                SQLiteConnection.CreateFile( database_path);
                _connection.Open();

                // first, let's create the plates table
                SQLiteCommand create_query = new SQLiteCommand( @"CREATE TABLE [plates] ([id] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
                                                                           [barcode] TEXT  NULL,
                                                                           CONSTRAINT [unique_barcode] UNIQUE (barcode)
                                                                          );");
                create_query.Connection = _connection;
                create_query.ExecuteNonQuery();
                // next, create the volumes table
                create_query.CommandText = @"CREATE TABLE [volumes] ([plate_id] INTEGER  NULL,
                                                                     [reservoir_name] TEXT  NULL,
                                                                     [baseline_volume_uL] FLOAT  NULL,
                                                                     [delta_volume_uL] FLOAT  NULL,
                                                                     CONSTRAINT [plate] UNIQUE (plate_id,reservoir_name)
                                                                    );";
                create_query.ExecuteNonQuery();
                // finally, create the storage_locations, which depends upon the StorageLocationsSchema
                // that comes from the plugin
                string sql = "CREATE TABLE [storage_locations] ([plate_id] INTEGER  NULL, [loaded] BOOLEAN NOT NULL";
                foreach( string s in storage_locations_schema)
                    sql += String.Format( ",[{0}] TEXT NULL", s);
                sql += ", CONSTRAINT [unique_plate_location] UNIQUE (plate_id));";
                create_query.CommandText = sql;
                create_query.ExecuteNonQuery();
                _connection.Close();
            }
            LoadDatabase( database_path, storage_locations_schema);
        }
        //---------------------------------------------------------------------
        public void LoadDatabase(string database_path, List<string> storage_locations_schema)
        {
            // check if the database exists first... if it doesn't, then the app should
            // prompt the user to create it
            if( !File.Exists( database_path))
                throw new InventoryFileDoesNotExistException( database_path);
            lock(_connection_lock){
                if( _connection == null)
                    _connection = new SQLiteConnection( String.Format( "Data Source={0}", database_path));
            
                if( _connection.State != ConnectionState.Open)
                    _connection.Open();

                try {
                    // verify device's storage_location schema
                    SQLiteCommand command = new SQLiteCommand( "PRAGMA table_info(storage_locations);");
                    command.Connection = _connection;
                    SQLiteDataReader reader = command.ExecuteReader();
                    DataTable dt = new DataTable();
                    dt.Load( reader);
                    reader.Close();
                    List<string> column_names = new List<string>();
                    foreach( DataRow row in dt.Rows) {
                        // index 1 in the DataRow corresponds to the field name
                        string field_name = row[1].ToString();
                        if( field_name != "plate_id")
                            column_names.Add( field_name);
                    }
                    IEnumerable<string> invalid_columns = storage_locations_schema.Except( column_names);
                    if( invalid_columns.Count() != 0)
                        throw new InventorySchemaMismatchException( invalid_columns);

                    // initialize the queries
                    InitializeQueries();
                    StorageLocationsSchema = new List<string>(storage_locations_schema);
                } catch (SQLiteException ) {
                    throw;
                } catch( Exception ) {
                    throw;
                } finally {
                    _connection.Close();
                }
            }
        }
        //---------------------------------------------------------------------
        public Dictionary<string, string> GetLocation(string barcode)
        {
            lock(_connection_lock){
                try {
                    DataTable dt = new DataTable();
                    _connection.Open();
                    GetLocationQuery.Parameters.Clear();
                    GetLocationQuery.Parameters.AddWithValue( "@barcode", barcode);
                    SQLiteDataReader reader = GetLocationQuery.ExecuteReader();
                    dt.Load( reader);
                    reader.Close();
                    if( dt.Rows.Count == 0)
                        throw new InventoryBarcodeNotFoundException( barcode);
                    else {
                        DataRow dr = dt.Rows[0];
                        // loop over the schema definition passed in on database load/creation
                        // and use it to only return those items that the plugin has said that
                        // it cares about for inventory purposes.
                        Dictionary<string,string> location = new Dictionary<string,string>();
                        foreach( string s in StorageLocationsSchema) {
                            location.Add( s, dr[s].ToString());
                        }
                        // and now, we actually care about the "loaded" field, so add that, too
                        location.Add( "loaded", dr["loaded"].ToString());
                        return location;
                    }
                } catch (SQLiteException ) {
                    throw;
                } catch( Exception ) {
                    throw;
                } finally {
                    _connection.Close();
                }
            }
        }
        //---------------------------------------------------------------------
        public string GetInventoryXml()
        {
            throw new NotImplementedException();
        }
        //---------------------------------------------------------------------
        public Dictionary<string, Dictionary<string, string>> GetInventoryData()
        {
            lock(_connection_lock){
                Dictionary<string, Dictionary<string,string>> inventory = new Dictionary<string,Dictionary<string,string>>();
                try {
                    _connection.Open();
                    DataTable dt = new DataTable();
                    SQLiteDataReader reader =  GetInventoryQuery.ExecuteReader();
                    dt.Load( reader);
                    reader.Close();
                    foreach( 
                        DataRow dr in dt.Rows) {
                        string barcode = dr["barcode"].ToString();
                        Dictionary<string,string> location = new Dictionary<string,string>();
                        // loop over the device-specific schema to get the location info
                        foreach( string s in StorageLocationsSchema)
                            location.Add( s, dr[s].ToString());
                        location.Add( "loaded", dr["loaded"].ToString());
                        inventory.Add( barcode, location);
                    }
                    return inventory;
                } catch ( SQLiteException ) {
                    throw;
                } catch( Exception ) {
                    throw;
                } finally {
                    _connection.Close();
                }
            }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// Removes the entry for a plate location from the storage_locations table ONLY.
        /// </summary>
        /// <remarks>
        /// This is used for when a plate gets loaded into a location that could be occupied
        /// by another plate, and the plate that was there was manually moved elsewhere.  We
        /// just want to clear the storage location for the previous plate and not delete it
        /// completely.
        /// </remarks>
        /// <param name="location"></param>
        /// <param name="connection"></param>
        private static void ClearStorageLocation( Dictionary<string,string> location, SQLiteConnection connection)
        {
            string sql = "DELETE FROM storage_locations WHERE ";
            // create the parameter list
            var params_list = (from kvp in location select (String.Format( "{0}='{1}'", kvp.Key, kvp.Value))).ToArray();
            sql += String.Join( " AND ", params_list);
            SQLiteCommand command = new SQLiteCommand( sql, connection);
            command.ExecuteNonQuery();
        }
        //---------------------------------------------------------------------
        public void ClearStorageLocation( Dictionary<string,string> location)
        {
            SQLiteConnection connection = new SQLiteConnection( _connection);
            ClearStorageLocation( location, connection);
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// Adds a plate with the specified barcode to the specified location
        /// </summary>
        /// <exception cref="SQLiteException">
        /// thrown if this barcode already exists in the database
        /// </exception>
        /// <param name="barcode"></param>
        /// <param name="location"></param>
        public void Load(string barcode, Dictionary<string, string> location)
        {
            lock(_connection_lock){
                SQLiteTransaction transaction = null;
                try {
                    // ONLY try to add the plate to the plates table if its barcode doesn't already exist
                    // I am calling it here so I don't mess with the existing code's transaction or
                    // connection.  Just getting the plate ID is reasonable to do in a separate operation.
                    int plate_id = GetPlateIdFromBarcode( barcode);
                    _connection.Open();
                    transaction = _connection.BeginTransaction();
            
                    // look for another plate in the same location, and if there is one
                    // DELETE the storage record (but not the plate)
                    ClearStorageLocation( location, _connection);
                    // if the barcode is empty, that means there wasn't a plate.  So after
                    // clearing out the storage_locations record, bail so we don't end
                    // up adding a empty-barcoded plate
                    if( BioNex.Shared.LibraryInterfaces.Constants.IsEmpty( barcode) || barcode == "") {
                        transaction.Commit();
                        return;
                    }

                    if( plate_id == 0) {
                        // first, add to the plates table
                        LoadQuery.Parameters.Clear();
                        LoadQuery.Parameters.AddWithValue( "@barcode", barcode);
                        plate_id = int.Parse( LoadQuery.ExecuteScalar().ToString());
                    }

                    // now add to the storage_locations table, based on the information passed in as [location]
                    // NOTE: I am adding the plate_id first
                    string sql = "INSERT OR REPLACE INTO storage_locations (plate_id, loaded,";
                    sql += string.Join( ",", location.Keys.ToArray());
                    sql += ") VALUES (";
                    // add the plate_id firsta
                    sql += plate_id.ToString() + ", 1";
                    if( location.Values.Count > 0)
                        sql += ",";
                    // then add the rest of the values
                    sql += string.Join( ",", location.Values.ToArray());
                    sql += ");";
                    AddToStorageLocationsQuery.CommandText = sql;
                    AddToStorageLocationsQuery.ExecuteNonQuery();
                    // if everything is okay, commit
                    transaction.Commit();
                } catch ( SQLiteException ) {
                    transaction.Rollback();
                    throw;
                } catch( Exception ) {
                    transaction.Rollback();
                    throw;
                } finally {
                    _connection.Close();
                }
            }
        }
        //---------------------------------------------------------------------
        private bool IsPlateUnloaded( string barcode)
        {
            Dictionary<string,string> location_info = GetLocation( barcode);
            return !bool.Parse( location_info["loaded"]);
        }
        //---------------------------------------------------------------------
        public void Unload(string barcode)
        {
            lock(_connection_lock){
                try {
                    // make sure the plate isn't already unloaded
                    if( IsPlateUnloaded( barcode))
                        throw new InventoryPlateAlreadyUnloadedException( barcode);
                    // get the plate ID for this barcode
                    int plate_id = GetPlateIdFromBarcode( barcode);
                    // now unload
                    _connection.Open();
                    RemoveFromStorageLocationsQuery.Parameters.Clear();
                    RemoveFromStorageLocationsQuery.Parameters.AddWithValue( "@plate_id", plate_id);
                    RemoveFromStorageLocationsQuery.ExecuteNonQuery();
                } catch (SQLiteException ) {
                    throw;
                } catch( KeyNotFoundException ) {
                    throw;
                } catch( Exception ) {
                    throw;
                } finally {
                    _connection.Close();
                }
            }
        }
        //---------------------------------------------------------------------
        internal int GetPlateIdFromBarcode( string barcode)
        {
            lock(_connection_lock){
                try {
                    _connection.Open();
                    GetPlateIdQuery.Parameters.Clear();
                    GetPlateIdQuery.Parameters.AddWithValue( "@barcode", barcode);
                    var plate_id = GetPlateIdQuery.ExecuteScalar();
                    // check to see if the plate exists
                    if( plate_id == null)
                        return 0;
                    return int.Parse( plate_id.ToString());
                } catch (SQLiteException ) {
                    throw;
                } catch( Exception) {
                    throw;
                } finally {
                    _connection.Close();
                }
            }
        }
        //---------------------------------------------------------------------
        public void Delete(string barcode)
        {
            lock(_connection_lock){
                try {
                    _connection.Open();
                    DeleteQuery.Parameters.Clear();
                    DeleteQuery.Parameters.AddWithValue( "@barcode", barcode);
                    DeleteQuery.ExecuteNonQuery();
                } catch ( SQLiteException) {
                    throw;
                } catch( Exception ) {
                    throw;
                } finally {
                    _connection.Close();
                }
            }
        }
        //---------------------------------------------------------------------
        public double GetVolume( string barcode, string reservoir_name)
        {
            lock(_connection_lock){
                try {
                    _connection.Open();
                    GetVolumeQuery.Parameters.Clear();
                    GetVolumeQuery.Parameters.AddWithValue( "@barcode", barcode);
                    GetVolumeQuery.Parameters.AddWithValue( "@reservoir_name", reservoir_name);
                    SQLiteDataReader reader = GetVolumeQuery.ExecuteReader();
                    DataTable dt = new DataTable();
                    dt.Load( reader);
                    reader.Close();
                    // we should get a record back that includes the baseline_volume_uL and delta_volume_uL
                    if( dt.Rows.Count == 0)
                        throw new InventoryBarcodeNotFoundException( barcode);
                    DataRow row = dt.Rows[0];
                    double baseline_volume_uL = double.Parse( row["baseline_volume_uL"].ToString());
                    double delta_volume_uL = double.Parse( row["delta_volume_uL"].ToString());
                    return baseline_volume_uL + delta_volume_uL;
                } catch (SQLiteException ) {
                    throw;
                } catch( Exception ) {
                    throw;
                } finally {
                    _connection.Close();
                }
            }
        }
        //---------------------------------------------------------------------
        public Dictionary<string, double> GetAllVolumes( string barcode)
        {
            lock(_connection_lock){
                try {
                    _connection.Open();
                    GetAllVolumesQuery.Parameters.Clear();
                    GetAllVolumesQuery.Parameters.AddWithValue( "@barcode", barcode);
                    SQLiteDataReader reader = GetAllVolumesQuery.ExecuteReader();
                    DataTable dt = new DataTable();
                    dt.Load( reader);
                    reader.Close();
                    // iterate over the rows in the table and add to the dictionary
                    Dictionary<string, double> volumes = new Dictionary<string,double>();
                    foreach( DataRow row in dt.Rows) {
                        string reservoir_name = row["reservoir_name"].ToString();
                        double initial_volume = double.Parse( row["baseline_volume_uL"].ToString());
                        double delta_volume = double.Parse( row["delta_volume_uL"].ToString());
                        volumes.Add( reservoir_name, initial_volume + delta_volume);
                    }
                    return volumes;
                } catch (SQLiteException ) {
                    throw;
                } catch( Exception ) {
                    throw;
                } finally {
                    _connection.Close();
                }
            }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Also zeros out the volume delta, since the assumption here is 
        /// </remarks>
        /// <param name="barcode"></param>
        /// <param name="reservoir_names"></param>
        /// <param name="volume_uL"></param>
        public void SetInitialVolume( string barcode, List<string> reservoir_names, double volume_uL)
        {
            lock(_connection_lock){
                SQLiteTransaction transaction = null;
                try {
                    // get plate barcode first
                    int plate_id = GetPlateIdFromBarcode( barcode);
                    // now set up the base query information.  barcode and volume won't change,
                    // so set the parameters with those first.  I added the reservoir name as well,
                    // because I need to clear it out in the loop anyway.
                    _connection.Open();
                    transaction = _connection.BeginTransaction();
                    SetInitialVolumeQuery.Parameters.Clear();
                    SetInitialVolumeQuery.Parameters.AddWithValue( "@plate_id", plate_id);
                    SetInitialVolumeQuery.Parameters.AddWithValue( "@baseline_volume_uL", volume_uL);
                    SetInitialVolumeQuery.Parameters.AddWithValue( "@reservoir_name", "");
                    // now we can loop over each of the reservoir names and only change
                    // the relative parameter -- the reservoir name.
                    foreach( string name in reservoir_names) {
                        SetInitialVolumeQuery.Parameters.RemoveAt( "@reservoir_name");
                        SetInitialVolumeQuery.Parameters.AddWithValue( "@reservoir_name", name);
                        SetInitialVolumeQuery.ExecuteNonQuery();
                    }
                    transaction.Commit();
                } catch (SQLiteException ) {
                    transaction.Rollback();
                    throw;
                } catch( Exception ) {
                    transaction.Rollback();
                    throw;
                } finally {
                    _connection.Close();
                }
            }
        }
        //---------------------------------------------------------------------
        public void AdjustVolume( string barcode, List<string> reservoir_names, double volume_delta_uL)
        {
            lock(_connection_lock){
                SQLiteTransaction transaction = null;
                try {
                    // get plate barcode first
                    int plate_id = GetPlateIdFromBarcode( barcode);
                    // get all of the reservoirs' volume deltas
                    Dictionary<string,double> current_volume_deltas = GetAllVolumeDeltas( barcode);
                    _connection.Open();
                    transaction = _connection.BeginTransaction();
                    SetDeltaVolumeQuery.Parameters.Clear();
                    SetDeltaVolumeQuery.Parameters.AddWithValue( "@plate_id", plate_id);
                    SetDeltaVolumeQuery.Parameters.AddWithValue( "@delta_volume_uL", 0);
                    SetDeltaVolumeQuery.Parameters.AddWithValue( "@reservoir_name", "");
                    // now we can loop over each of the reservoir names and only change
                    // the relative parameter -- the reservoir name.
                    foreach( string name in reservoir_names) {
                        SetDeltaVolumeQuery.Parameters.RemoveAt( "@reservoir_name");
                        double current_volume_delta = current_volume_deltas.ContainsKey( name) ? current_volume_deltas[name] : 0;
                        SetDeltaVolumeQuery.Parameters.AddWithValue( "@delta_volume_uL", current_volume_delta + volume_delta_uL);
                        SetDeltaVolumeQuery.Parameters.AddWithValue( "@reservoir_name", name);
                        SetDeltaVolumeQuery.ExecuteNonQuery();
                    }
                    transaction.Commit();
                } catch (SQLiteException ) {
                    transaction.Rollback();
                    throw;
                } catch( Exception ) {
                    transaction.Rollback();
                    throw;
                } finally {
                    _connection.Close();
                }
            }
        }
        //---------------------------------------------------------------------
        private Dictionary<string,double> GetAllVolumeDeltas( string barcode)
        {
            lock(_connection_lock){
                Dictionary<string,double> volume_deltas = new Dictionary<string,double>();

                try {
                    _connection.Open();
                    GetAllVolumeDeltasQuery.Parameters.Clear();
                    GetAllVolumeDeltasQuery.Parameters.AddWithValue( "@barcode", barcode);
                    SQLiteDataReader reader = GetAllVolumeDeltasQuery.ExecuteReader();
                    DataTable dt = new DataTable();
                    dt.Load( reader);
                    reader.Close();
                    if( dt.Rows.Count == 0)
                        return volume_deltas;
                    foreach( DataRow row in dt.Rows) {
                        string reservoir_name = row["reservoir_name"].ToString();
                        double volume_delta = double.Parse( row["delta_volume_uL"].ToString());
                        volume_deltas.Add( reservoir_name, volume_delta);
                    }
                    return volume_deltas;
                } catch (SQLiteException ) {
                    throw;
                } catch(Exception ) {
                    throw;
                } finally {
                    _connection.Close();
                }
            }
        }
        #endregion
    }
}
