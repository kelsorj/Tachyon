using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace BioNex.LiquidLevelDevice
{
    public interface ILiquidLevelVolumeMapDatabase
    {
        Int32 CreateVolumeMap(string labware_name);
        void DeleteVolumeMap(Int32 map_id);
        void AddCaptureDetail(Int32 map_id, Int32 column, double reference_volume, double measurement);

        IList<Int32> GetMapIDsForLabware(string labware_name);
        IList<Int32> GetEnabledMapIDsForLabware(string labware_name);
        double GetMinVolumeForMap(Int32 map_id);
        double GetMaxVolumeForMap(Int32 map_id);
        bool GetEnabledForMap(Int32 map_id);
        int GetFitOrderForMap(Int32 map_id);
        IList<MapDetails> GetCaptureDetailsForMap(Int32 map_id);
        IList<double> GetCoefficientsForMap(Int32 map_id);
        LabwareDetails GetLabwareDetails(string labware_name);

        void WriteMapCoefficients(Int32 map_id, IList<double> coefficients);
        void WriteMapEnabled(Int32 map_id, bool enabled);
        void WriteMapMinVolume(Int32 map_id, double min_volume);
        void WriteMapMaxVolume(Int32 map_id, double max_volume);
        void WriteLabwareDetails(string labware_name, LabwareDetails details);
    }

    public class MapDetails
    {
        public int Column { get; set; }
        public double Volume { get; set; }
        public double Measurement { get; set; }
    }

    public class LabwareDetails
    {
        public string Sensitivity { get; set; }
        public double CaptureOffset { get; set; }

        public LabwareDetails(IDictionary<string, string> details)
        {
            Sensitivity = details.ContainsKey(LLProperties.Sensitivity) ? details[LLProperties.Sensitivity] : "D";
            CaptureOffset = details.ContainsKey(LLProperties.CaptureOffset) ? double.Parse(details[LLProperties.CaptureOffset]) : 0.0;
        }

        public IDictionary<string, string> ToDictionary()
        {
            var foo = new Dictionary<string,string>();
            return new Dictionary<string,string>()
            { 
                {LLProperties.Sensitivity, Sensitivity},
                {LLProperties.CaptureOffset, CaptureOffset.ToString()}
            };
        }
    }

    public class LiquidLevelVolumeMapDatabase : ILiquidLevelVolumeMapDatabase
    {
        public LiquidLevelVolumeMapDatabase( string database_path)
        {
            // if the db file doesn't exist, create it
            var need_create = !File.Exists(database_path);                
            _connection = new SQLiteConnection(string.Format("Data Source={0}", database_path));
            if (need_create)
                CreateDatabase();
        }

        public Int32 CreateVolumeMap(string labware_name)
        {
            var sql = string.Format("INSERT INTO volume_map_detail (labware_name, enabled, min_volume, max_volume) VALUES ('{0}','{1}','{2}','{3}');",
                labware_name, true, 0.0, 100.0);
            ExecuteTransaction(new SQLiteCommand(sql, _connection));

            // get the map_id from the row just inserted
            sql = "SELECT MAX(map_id) FROM volume_map_detail;";
            var result = Convert.ToInt32(ExecuteQuery(new SQLiteCommand(sql, _connection)));
            return result;
        }

        public void DeleteVolumeMap(Int32 map_id)
        {
            var sql = string.Format("DELETE from volume_map_detail WHERE map_id='{0}';", map_id);
            ExecuteTransaction(new SQLiteCommand(sql, _connection));

            // NOTE : cascading delete foreign key constraint appears to require manual enforcement, so we need to delete the capture detail records
            sql = string.Format("DELETE from capture_detail WHERE map_id='{0}';", map_id);
            ExecuteTransaction(new SQLiteCommand(sql, _connection));

            // NOTE : cascading delete foreign key constraint appears to require manual enforcement, so we need to delete the coefficient detail records
            sql = string.Format("DELETE from coefficient_detail WHERE map_id='{0}';", map_id);
            ExecuteTransaction(new SQLiteCommand(sql, _connection));        
        }

        public void AddCaptureDetail(Int32 map_id, Int32 column, double reference_volume, double measurement)
        {
            var sql = string.Format("INSERT INTO capture_detail (map_id, column, reference_volume, measurement) VALUES ('{0}','{1}','{2}','{3}');",
                map_id, column, reference_volume, measurement);
            ExecuteTransaction(new SQLiteCommand(sql, _connection));
        }

        public IList<Int32> GetMapIDsForLabware(string labware_name)
        {
            var sql = string.Format("SELECT map_id FROM volume_map_detail WHERE labware_name='{0}';", labware_name);
            var results = ExecuteSelect(new SQLiteCommand(sql, _connection));

            var map_ids = new List<Int32>();
            foreach (DataRow row in results.Rows)
                map_ids.Add(Convert.ToInt32(row[0]));
            return map_ids;
        }

        public IList<Int32> GetEnabledMapIDsForLabware(string labware_name)
        {
            var sql = string.Format("SELECT map_id FROM volume_map_detail WHERE labware_name='{0}' AND enabled='1';", labware_name);
            var results = ExecuteSelect(new SQLiteCommand(sql, _connection));

            var map_ids = new List<Int32>();
            foreach (DataRow row in results.Rows)
                map_ids.Add(Convert.ToInt32(row[0]));
            return map_ids;
        }

        public double GetMinVolumeForMap(Int32 map_id)
        {
            var sql = string.Format("SELECT min_volume FROM volume_map_detail WHERE map_id='{0}';", map_id);
            var result = Convert.ToDouble(ExecuteQuery(new SQLiteCommand(sql, _connection)));
            return result;
        }

        public double GetMaxVolumeForMap(Int32 map_id)
        {
            var sql = string.Format("SELECT max_volume FROM volume_map_detail WHERE map_id='{0}';", map_id);
            var result = Convert.ToDouble(ExecuteQuery(new SQLiteCommand(sql, _connection)));
            return result;
        }

        public bool GetEnabledForMap(Int32 map_id)
        {
            var sql = string.Format("SELECT enabled FROM volume_map_detail WHERE map_id='{0}';", map_id);
            var result = Convert.ToBoolean(ExecuteQuery(new SQLiteCommand(sql, _connection)));
            return result;
        }

        public int GetFitOrderForMap(Int32 map_id)
        {
            var sql = string.Format("SELECT COUNT(ordinal) FROM coefficient_detail WHERE map_id='{0}';", map_id);
            var max_ordinal = Convert.ToInt32(ExecuteQuery(new SQLiteCommand(sql, _connection)));
            var result = Math.Max(1, max_ordinal - 1);
            return result;
        }

        public IList<MapDetails> GetCaptureDetailsForMap(Int32 map_id)
        {
            var sql = string.Format("SELECT column,reference_volume,measurement FROM capture_detail WHERE map_id='{0}' ORDER BY column;", map_id);
            var results = ExecuteSelect(new SQLiteCommand(sql, _connection));

            var details = new List<MapDetails>();
            foreach (DataRow row in results.Rows)
                details.Add(new MapDetails() { Column = Convert.ToInt32(row[0]), Volume = Convert.ToDouble(row[1]), Measurement = Convert.ToDouble(row[2]) });
            return details;
        }

        public IList<double> GetCoefficientsForMap(Int32 map_id)
        {
            var sql = string.Format("SELECT coefficient FROM coefficient_detail WHERE map_id='{0}' ORDER BY ordinal", map_id);
            var results = ExecuteSelect(new SQLiteCommand(sql, _connection));

            var coefficients = new List<double>();
            foreach (DataRow row in results.Rows)
                coefficients.Add(Convert.ToDouble(row[0]));
            return coefficients;
        }

        public LabwareDetails GetLabwareDetails(string labware_name)
        {
            var sql = string.Format("SELECT key,value FROM labware_detail WHERE labware_name='{0}';", labware_name);
            var results = ExecuteSelect(new SQLiteCommand(sql, _connection));

            var details = new Dictionary<string, string>();
            foreach (DataRow row in results.Rows)
                details[Convert.ToString(row[0])] = Convert.ToString(row[1]);

            var labware_details = new LabwareDetails(details);
            return labware_details;
        }

        public void WriteMapCoefficients(Int32 map_id, IList<double> coefficients)
        {
            // erase old coefficients, insert new set
            var sql = string.Format("DELETE from coefficient_detail WHERE map_id='{0}';", map_id);
            var cmds = new List<SQLiteCommand>(){new SQLiteCommand(sql, _connection)};

            for (int i = 0; i < coefficients.Count; ++i)
            {
                sql = string.Format("INSERT INTO coefficient_detail (map_id, ordinal, coefficient) VALUES ('{0}', '{1}', '{2}');",
                    map_id, i, coefficients[i]);
                cmds.Add(new SQLiteCommand(sql, _connection));
            }
            ExecuteMultipartTransaction(cmds);
        }

        public void WriteMapEnabled(Int32 map_id, bool enabled)
        {
            var sql = string.Format("UPDATE volume_map_detail SET enabled='{0}' where map_id='{1}';",
                enabled ? 1 : 0, map_id);
            ExecuteTransaction(new SQLiteCommand(sql, _connection));
        }

        public void WriteMapMinVolume(Int32 map_id, double min_volume)
        {
            var sql = string.Format("UPDATE volume_map_detail SET min_volume='{0}' where map_id='{1}';",
                min_volume, map_id);
            ExecuteTransaction(new SQLiteCommand(sql, _connection));
        }

        public void WriteMapMaxVolume(Int32 map_id, double max_volume)
        {
            var sql = string.Format("UPDATE volume_map_detail SET max_volume='{0}' where map_id='{1}';",
                max_volume, map_id);
            ExecuteTransaction(new SQLiteCommand(sql, _connection));

        }

        public void WriteLabwareDetails(string labware_name, LabwareDetails labware_details)
        {
            var details = labware_details.ToDictionary();

            // erase old details, insert new set
            var sql = string.Format("DELETE from labware_detail WHERE labware_name='{0}';", labware_name);
            var cmds = new List<SQLiteCommand>(){new SQLiteCommand(sql, _connection)};

            foreach( var key in details.Keys)
            {
                sql = string.Format("INSERT INTO labware_detail (labware_name, key, value) VALUES ('{0}', '{1}', '{2}');",
                    labware_name, key, details[key]);
                cmds.Add(new SQLiteCommand(sql, _connection));
            }
            ExecuteMultipartTransaction(cmds);
        }

        void CreateDatabase()
        {
            var sql = @"CREATE TABLE [volume_map_detail] ( 
                    [map_id] INTEGER PRIMARY KEY AUTOINCREMENT, 
                    [labware_name] TEXT,
                    [enabled] BOOLEAN, 
                    [min_volume] DOUBLE,  
                    [max_volume] DOUBLE 
                    );";

            ExecuteTransaction(new SQLiteCommand(sql, _connection));

            sql = @"CREATE TABLE [capture_detail] ( 
                    [map_id] INTEGER CONSTRAINT [map_id] REFERENCES [volume_map_detail]([map_id]) ON DELETE CASCADE, 
                    [column] INTEGER, 
                    [reference_volume] DOUBLE, 
                    [measurement] DOUBLE 
                    );";
            ExecuteTransaction(new SQLiteCommand(sql, _connection));

            sql = @"CREATE TABLE [coefficient_detail] ( 
                    [map_id] INTEGER CONSTRAINT [map_id] REFERENCES [volume_map_detail]([map_id]) ON DELETE CASCADE, 
                    [ordinal] INTEGER, 
                    [coefficient] DOUBLE 
                    );";

            ExecuteTransaction(new SQLiteCommand(sql, _connection));

            sql = @"CREATE TABLE [labware_detail] ( 
                    [map_id] INTEGER PRIMARY KEY AUTOINCREMENT, 
                    [labware_name] TEXT,
                    [key] TEXT,
                    [value] TEXT
                    );";

            ExecuteTransaction(new SQLiteCommand(sql, _connection));
        }

        SQLiteConnection _connection;
        object ExecuteQuery(SQLiteCommand command)
        {
            try
            {
                command.Connection.Open();
                return command.ExecuteScalar();
            }
            finally
            {
                command.Connection.Close();
            }
        }

        void ExecuteTransaction(SQLiteCommand command)
        {
            SQLiteTransaction transaction = null;
            try
            {
                command.Connection.Open();
                transaction = command.Connection.BeginTransaction();
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            catch (Exception)
            {
                if( transaction != null)
                    transaction.Rollback();
                throw;
            }
            finally
            {
                command.Connection.Close();
            }
        }

        DataTable ExecuteSelect(SQLiteCommand command)
        {
            SQLiteDataReader reader = null;
            try
            {
                command.Connection.Open();
                reader = command.ExecuteReader();
                var data = new DataTable();
                data.Load(reader);
                return data;
            }
            finally
            {
                if( reader != null)
                    reader.Close();
                command.Connection.Close();
            }
        }

        void ExecuteMultipartTransaction(IList<SQLiteCommand> commands)
        {
            SQLiteTransaction transaction = null;
            try
            {
                commands[0].Connection.Open();
                transaction = commands[0].Connection.BeginTransaction();
                foreach( var command in commands)
                    command.ExecuteNonQuery();
                transaction.Commit();
            }
            catch (Exception)
            {
                if (transaction != null)
                    transaction.Rollback();
                throw;
            }
            finally
            {
                commands[0].Connection.Close();
            }
        }
    }
}
