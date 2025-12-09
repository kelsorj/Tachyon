using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public class DatabaseAttribute : Attribute
    {
    }

    public class DatabaseTableAttribute : DatabaseAttribute // TODO: fix hacked in constraints to support multiple-attribute primary keys.
    {
        public DatabaseTableAttribute( string name, params Object[] constraints)
        {
            name_ = name;
            constraints_ = new List< string>();
            if( constraints != null){
                foreach( Object o in constraints){
                    constraints_.Add(( string)o);
                }
            }
        }

        public DatabaseTableAttribute( string name)
            : this( name, null)
        {
        }

        public string Name { get { return name_; } }

        public string Constraints 
        {
            get
            {
                if (constraints_.Count == 0)
                    return "";
                return ",\n" + string.Join(",\n", constraints_.ToArray());
            }
        }

        public override string ToString()
        {
            return "[DatabaseTableAttribute(name=" + name_ + ")]";
        }

        protected string name_;
        protected List< string> constraints_;
    }

    public enum DatabaseColumnType
    {
        BOOLEAN,
        INTEGER, // this needs to bind to a long member of a class that uses the database attributes
        FLOAT,
        TEXT,
        DATETIME,
    }

    public enum DatabaseColumnFlags
    {
        PRIMARY_KEY             = 0x0001,
        UNIQUE                  = 0x0002,
        AUTOINCREMENT           = 0x0004,
    }

    public class DatabaseColumnAttribute : DatabaseAttribute
    {
        public DatabaseColumnAttribute( string name, DatabaseColumnType column_type, DatabaseColumnFlags column_flags)
        {
            name_ = name;
            column_type_ = column_type;
            column_flags_ = column_flags;
        }

        public DatabaseColumnAttribute( string name, DatabaseColumnType column_type)
            : this( name, column_type, 0)
        {
        }

        public string Name { get { return name_; } }
        public DatabaseColumnType ColumnType { get { return column_type_; } }
        public DatabaseColumnFlags ColumnFlags { get { return column_flags_; } }

        public override string ToString()
        {
            return "[DatabaseColumnAttribute(name=" + name_ + ";column_type=" + column_type_ + ";column_flags=" + column_flags_ + ")]";
        }

        protected string name_;
        protected DatabaseColumnType column_type_;
        protected DatabaseColumnFlags column_flags_;
    }

    /// <remarks>
    /// Dave's observations:
    /// - doesn't deal with class hierarchies with underlying database attributes.  For example,
    ///   it does not like TipBoxes because a TipBox is-a Labware, and therefore wants to
    ///   inherit Labware's database attributes.  I made a patch for this that needs reviewing
    ///   because it's not perfect.  It probably needs to traverse up the hierarchy until it 
    ///   hits System.Object.
    /// </remarks>
    public class DatabaseIntegration : IDisposable
    {
        private SQLiteTransaction _transaction;

        public DatabaseIntegration( string connection_string)
        {
            connection_ = new SQLiteConnection( connection_string);
            connection_.Open();
        }

        public void Dispose()
        {
            connection_.Close();
            connection_.Dispose();
        }

        protected static void ExtractTableDatabaseAttributes( Type table, out DatabaseTableAttribute table_attribute, out Dictionary< string, DatabaseColumnAttribute> column_attributes_dict)
        {
            // get DatabaseTableAttribute from table.
            Object[] table_attributes = table.GetCustomAttributes( typeof( DatabaseTableAttribute), false);
            // there should only be one!
            // DKM 2010-08-17 hack alert -- my TipBox class is-a Labware, but doesn't have any
            //                database attributes.  So if there aren't any table_attributes,
            //                try going up the heirarchy exactly once before failing
            if( table_attributes.Count() == 0)
                table_attributes = table.BaseType.GetCustomAttributes( typeof( DatabaseTableAttribute), false);
            Debug.Assert( table_attributes.Count() == 1);
            // cast it to a DatabaseTableAttribute and return it as the out param "table_attribute".
            table_attribute = ( DatabaseTableAttribute)( table_attributes[ 0]);

            // initialize out param "column_attributes_dict" to an empty dictionary.
            column_attributes_dict = new Dictionary< string, DatabaseColumnAttribute>();
            // each property of "table" is a column, so get all the column information"s".
            PropertyInfo[] column_infos = table.GetProperties();
            // for each column...
            foreach( PropertyInfo column_info in column_infos){
                // get DatabaseColumnAttribute from column.
                Object[] column_attributes = column_info.GetCustomAttributes( typeof( DatabaseColumnAttribute), false);
                // if the property is not attached to a database column, there won't be any column attributes.
                if( column_attributes.Count() == 0){
                    continue;
                }
                // there should only be one!
                // DKM 2010-08-05 or zero, if you add other members to the table class that aren't attributed
                Debug.Assert( column_attributes.Count() <= 1);
                // there could be members that aren't related to the database schema, so in that
                // case we ignore them and continue.
                if( column_attributes.Count() == 0)
                    continue;
                // cast it to a DatabaseColumnAttribute and add it to the dictionary out param "column_attributes_dict".
                column_attributes_dict[ column_info.Name] = ( DatabaseColumnAttribute)( column_attributes[ 0]);
            }
        }

        protected static Dictionary< string, Object> ExtractEntityAttributeValues( Object entity)
        {
            // get the entity's table.
            Type table = entity.GetType();

            // initialize out param "attribute_values" to an empty dictionary.
            Dictionary< string, Object> attribute_values = new Dictionary< string, Object>();
            // each property of "table" is a column, so get all the column information"s".
            PropertyInfo[] column_infos = table.GetProperties();
            // for each column...
            foreach( PropertyInfo column_info in column_infos){
                // get DatabaseColumnAttribute from column.
                Object[] column_attributes = column_info.GetCustomAttributes( typeof( DatabaseColumnAttribute), false);
                // there should only be one!
                Debug.Assert( column_attributes.Count() == 1);
                // get the attribute's attr_value and associate it with the attribute's name in "attribute_values".
                attribute_values[ (( DatabaseColumnAttribute)( column_attributes[ 0])).Name] = column_info.GetValue( entity, null);
            }
            return attribute_values;
        }

        public static string EntityToString( Object entity)
        {
            Dictionary< string, Object> attribute_values = ExtractEntityAttributeValues( entity);
            List< string> nvps = new List< string>();
            foreach( KeyValuePair< string, Object> attribute_value in attribute_values){
                nvps.Add( attribute_value.Key + "=" + attribute_value.Value);
            }
            return "[" + entity.GetType() + " AS ENTITY(" + string.Join( ";", nvps.ToArray()) + ")]";
        }

        protected static string GenerateCreateTableQueryString( Type table)
        {
            // declare out params for ExtractTableDatabaseAttributes().
            DatabaseTableAttribute table_attribute;
            Dictionary< string, DatabaseColumnAttribute> column_attributes_dict;
            // extract database table attributes.
            ExtractTableDatabaseAttributes( table, out table_attribute, out column_attributes_dict);

            List< string> column_decls_list = new List< string>();
            foreach( KeyValuePair< string, DatabaseColumnAttribute> column_attribute in column_attributes_dict){
                DatabaseColumnAttribute ca = column_attribute.Value;
                string column_decl = ca.Name + " " + ca.ColumnType;
                DatabaseColumnFlags cf = ca.ColumnFlags;
                if( cf != 0){
                    if( ( cf & DatabaseColumnFlags.PRIMARY_KEY) != 0) { column_decl +=  " PRIMARY KEY"; }
                    if( ( cf & DatabaseColumnFlags.UNIQUE) != 0) { column_decl += " UNIQUE"; }
                    if( ( cf & DatabaseColumnFlags.AUTOINCREMENT) != 0){ column_decl += " AUTOINCREMENT"; } // this has to be last??
                }
                column_decls_list.Add( column_decl);
            }
            string column_decls = string.Join( ",\n\t", column_decls_list.ToArray());
            return string.Format( "CREATE TABLE {0} (\n\t{1}{2}\n);", table_attribute.Name, column_decls, table_attribute.Constraints);
        }

        protected static string GenerateDropTableQueryString( Type table)
        {
            // declare out params for ExtractTableDatabaseAttributes().
            DatabaseTableAttribute table_attribute;
            Dictionary< string, DatabaseColumnAttribute> column_attributes_dict;
            // extract database table attributes.
            ExtractTableDatabaseAttributes( table, out table_attribute, out column_attributes_dict);

            return string.Format( "DROP TABLE {0};", table_attribute.Name);
        }

        protected static string GenerateInsertQueryString( Type table)
        {
            // declare out params for ExtractTableDatabaseAttributes().
            DatabaseTableAttribute table_attribute;
            Dictionary< string, DatabaseColumnAttribute> column_attributes_dict;
            // extract database table attributes.
            ExtractTableDatabaseAttributes( table, out table_attribute, out column_attributes_dict);

            bool has_autoincrement_primary_key = false;
            List< string> column_names_list = new List< string>();
            List< string> param_names_list = new List< string>();
            foreach( KeyValuePair< string, DatabaseColumnAttribute> column_attribute in column_attributes_dict){
                if(( column_attribute.Value.ColumnFlags & DatabaseColumnFlags.AUTOINCREMENT) != 0){
                    if(( column_attribute.Value.ColumnFlags & DatabaseColumnFlags.PRIMARY_KEY) != 0){
                        has_autoincrement_primary_key = true;
                    }
                    continue;
                }
                string column_name = column_attribute.Value.Name;
                column_names_list.Add( column_name);
                param_names_list.Add( "@" + column_name);
            }
            string column_names = string.Join( ",\n\t", column_names_list.ToArray());
            string param_names = string.Join( ",\n\t", param_names_list.ToArray());
            return string.Format( "INSERT INTO {0} (\n\t{1}\n) VALUES (\n\t{2}\n);{3}", table_attribute.Name, column_names, param_names, has_autoincrement_primary_key ? "\nSELECT last_insert_rowid() AS record_id;" : "");
        }

        protected static string GenerateSelectQueryString( Type table, string where_clause)
        {
            // declare out params for ExtractTableDatabaseAttributes().
            DatabaseTableAttribute table_attribute;
            Dictionary< string, DatabaseColumnAttribute> column_attributes_dict;
            // extract database table attributes.
            ExtractTableDatabaseAttributes( table, out table_attribute, out column_attributes_dict);

            // Felix, you might be able to all of the looping and join stuff in one line:
            // string column_names = string.Join( ",\n\t", (from ca in column_attributes_dict select ca.Value.Name).ToArray());
            List< string> column_names_list = new List< string>();
            foreach( KeyValuePair< string, DatabaseColumnAttribute> column_attribute in column_attributes_dict){
                string column_name = column_attribute.Value.Name;
                column_names_list.Add( column_name);
            }
            string column_names = string.Join( ",\n\t", column_names_list.ToArray());
            return string.Format( "SELECT\n\t{0}\nFROM {1}\n{2};", column_names, table_attribute.Name, where_clause);
        }

        protected static string GenerateSelectCountQueryString( Type table, string where_clause)
        {
            // declare out params for ExtractTableDatabaseAttributes().
            DatabaseTableAttribute table_attribute;
            Dictionary< string, DatabaseColumnAttribute> column_attributes_dict;
            // extract database table attributes.
            ExtractTableDatabaseAttributes( table, out table_attribute, out column_attributes_dict);

            return string.Format( "SELECT COUNT (*)\nFROM {0}\n{1};", table_attribute.Name, where_clause);
        }

        protected static string GenerateUpdateQueryString( Type table, string where_clause)
        {
            // declare out params for ExtractTableDatabaseAttributes().
            DatabaseTableAttribute table_attribute;
            Dictionary< string, DatabaseColumnAttribute> column_attributes_dict;
            // extract database table attributes.
            ExtractTableDatabaseAttributes( table, out table_attribute, out column_attributes_dict);

            List< string> assignments_list = new List< string>();
            foreach( KeyValuePair< string, DatabaseColumnAttribute> column_attribute in column_attributes_dict){
                if(( column_attribute.Value.ColumnFlags & DatabaseColumnFlags.AUTOINCREMENT) != 0){
                    continue;
                }
                string column_name = column_attribute.Value.Name;
                assignments_list.Add( column_name + "=@" + column_name);
            }
            string assignments = string.Join( ",\n\t", assignments_list.ToArray());
            return string.Format( "UPDATE {0}\nSET\n\t{1}\n{2};", table_attribute.Name, assignments, where_clause);
        }

        protected static string GenerateDeleteQueryString( Type table, string where_clause)
        {
            // declare out params for ExtractTableDatabaseAttributes().
            DatabaseTableAttribute table_attribute;
            Dictionary< string, DatabaseColumnAttribute> column_attributes_dict;
            // extract database table attributes.
            ExtractTableDatabaseAttributes( table, out table_attribute, out column_attributes_dict);

            return string.Format( "DELETE FROM {0}\n{1};", table_attribute.Name, where_clause);
        }

        public int CreateTable( Type table)
        {
            int retval = 0;
            try{
                SQLiteCommand command = connection_.CreateCommand();
                command.CommandText = GenerateCreateTableQueryString( table);
                retval = command.ExecuteNonQuery();
                command.Dispose();
            } catch( Exception){
                throw;
            }
            return retval;
        }


        public int DropTable( Type table)
        {
            int retval = 0;
            try{
                SQLiteCommand command = connection_.CreateCommand();
                command.CommandText = GenerateDropTableQueryString( table);
                retval = command.ExecuteNonQuery();
                command.Dispose();
            } catch( Exception){
                // throw;
            }
            return retval;
        }

        public long InsertEntity( Object entity)
        {
            Type table = entity.GetType();

            // declare out params for ExtractTableDatabaseAttributes().
            DatabaseTableAttribute table_attribute;
            Dictionary< string, DatabaseColumnAttribute> column_attributes_dict;
            // extract database table attributes.
            ExtractTableDatabaseAttributes( table, out table_attribute, out column_attributes_dict);

            SQLiteCommand command = connection_.CreateCommand();
            command.CommandText = GenerateInsertQueryString( table);

            command.Parameters.Clear();
            foreach( KeyValuePair< string, DatabaseColumnAttribute> column_attribute in column_attributes_dict){
                if(( column_attribute.Value.ColumnFlags & DatabaseColumnFlags.AUTOINCREMENT) != 0){
                    continue;
                }
                string param_name = "@" + column_attribute.Value.Name;
                object param_value = table.GetProperty( column_attribute.Key).GetValue( entity, null);
                command.Parameters.AddWithValue( param_name, param_value);
            }

            Object retval = command.ExecuteScalar();
            command.Dispose();
            return ( retval == null) ? 0 : (( long)retval);
        }

        private static List< TABLE> executeSelect< TABLE>(SQLiteCommand command, Dictionary< string, DatabaseColumnAttribute> column_attributes_dict)
        {
            Type table = typeof(TABLE);

            SQLiteDataReader reader = command.ExecuteReader();
            command.Dispose();
            DataTable dt = new DataTable();
            dt.Load(reader);
            reader.Close();


            List<TABLE> entities = new List<TABLE>();
            foreach (DataRow row in dt.Rows)
            {
                TABLE entity = (TABLE)(System.Activator.CreateInstance(table));
                foreach (KeyValuePair<string, DatabaseColumnAttribute> column_attribute in column_attributes_dict)
                {
                    string attr_name = column_attribute.Value.Name;
                    Object attr_value = row[attr_name];
                    try
                    {
                        table.GetProperty(column_attribute.Key).SetValue(entity, attr_value, null);
                    }
                    catch (Exception ex)
                    {
                        attr_value = null;
                        Console.WriteLine("Could not read entity from database: " + ex.Message);
                    }
                }
                entities.Add(entity);
            }
            return entities;
        }

        public List< TABLE> SelectEntities< TABLE>( string where_clause)
        {
            Type table = typeof( TABLE);

            // declare out params for ExtractTableDatabaseAttributes().
            DatabaseTableAttribute table_attribute;
            Dictionary< string, DatabaseColumnAttribute> column_attributes_dict;
            // extract database table attributes.
            ExtractTableDatabaseAttributes( table, out table_attribute, out column_attributes_dict);

            SQLiteCommand command = connection_.CreateCommand();
            command.CommandText = GenerateSelectQueryString( table, where_clause);

            try
            {
                return executeSelect<TABLE>(command, column_attributes_dict);
            }
            catch (SQLiteException e)
            {
                if( !e.Message.Contains("no such table"))
                    throw e;
            }

            // no such table, try creating it
            CreateTable(table);
            return executeSelect<TABLE>(command, column_attributes_dict);
        }

        public long CountEntities( Type table, string where_clause)
        {
            SQLiteCommand command = connection_.CreateCommand();
            command.CommandText = GenerateSelectCountQueryString( table, where_clause);
            Object retval = command.ExecuteScalar();
            command.Dispose();
            return ( retval == null) ? 0 : (( long)retval);
        }

        public void UpdateEntity( Object entity, string where_clause)
        {
            Type table = entity.GetType();

            // declare out params for ExtractTableDatabaseAttributes().
            DatabaseTableAttribute table_attribute;
            Dictionary< string, DatabaseColumnAttribute> column_attributes_dict;
            // extract database table attributes.
            ExtractTableDatabaseAttributes( table, out table_attribute, out column_attributes_dict);

            SQLiteCommand command = connection_.CreateCommand();
            command.CommandText = GenerateUpdateQueryString( table, where_clause);

            command.Parameters.Clear();
            foreach( KeyValuePair< string, DatabaseColumnAttribute> column_attribute in column_attributes_dict){
                if(( column_attribute.Value.ColumnFlags & DatabaseColumnFlags.AUTOINCREMENT) != 0){
                    continue;
                }
                string param_name = "@" + column_attribute.Value.Name;
                object param_value = table.GetProperty( column_attribute.Key).GetValue( entity, null);
                command.Parameters.AddWithValue( param_name, param_value is string ? EscapeString(param_value as string) : param_value);
            }

            command.ExecuteNonQuery();
            command.Dispose();
        }

        // DKM 2010-08-16 needs input from Felix.  I'm not sure this is the most generic
        //                way to change one attribute.
        public void UpdateEntityAttribute( Object entity, string attribute_name, string new_attribute_value, string where_clause)
        {
            Type table = entity.GetType();

            // declare out params for ExtractTableDatabaseAttributes().
            DatabaseTableAttribute table_attribute;
            Dictionary< string, DatabaseColumnAttribute> column_attributes_dict;
            // extract database table attributes and column attributes, but
            // we only need the table attribute to get at the table name
            ExtractTableDatabaseAttributes( table, out table_attribute, out column_attributes_dict);
            // create the update query
            SQLiteCommand command = connection_.CreateCommand();
            command.CommandText = String.Format( "UPDATE {0} SET {1}='{2}' {3}",
                                                 table_attribute.Name,
                                                 attribute_name, new_attribute_value,
                                                 where_clause);
            command.ExecuteNonQuery();
        }

        public void DeleteEntities( Type table, string where_clause)
        {
            SQLiteCommand command = connection_.CreateCommand();
            command.CommandText = GenerateDeleteQueryString( table, where_clause);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        protected SQLiteConnection connection_;

        public static string EscapeString( string original)
        {
            // replace ' with ''
            return original.Replace( "'", "''");
        }

        public static string UnescapeString( string escaped)
        {
            return escaped.Replace( "''", "'");
        }

        /// <summary>
        /// Allows multiple INSERT statements to be quickly executed, without having a separate transaction for each one.
        /// Remember to call CommitBatchTransaction() when done with the INSERTs.
        /// </summary>
        public void BeginBatchTransaction()
        {
            if( _transaction != null)
                throw new Exception( "A new transaction was started without committing the previous one");
            _transaction = connection_.BeginTransaction();
        }

        public void CommitBatchTransaction()
        {
            _transaction.Commit();
            _transaction = null;
        }
    }
#endif
}
