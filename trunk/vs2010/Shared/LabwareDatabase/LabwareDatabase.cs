using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;
using log4net;

[assembly: InternalsVisibleTo("LabwareDatabaseMSTests")]

namespace BioNex.Shared.LabwareDatabase
{
    /// <summary>
    /// This class manages labware that is ultimately stored in a database.  The database gets
    /// loaded when  
    /// </summary>
    [Export(typeof(ILabwareDatabase))]
    [Export(typeof(ISystemSetupEditor))]
    public class LabwareDatabase : IDisposable, BioNex.Shared.LibraryInterfaces.ILabwareDatabase, BioNex.Shared.LibraryInterfaces.ISystemSetupEditor
    {
        private const string TeachingJigName = "teaching jig";
        public DatabaseIntegration DBIntegration { get; private set; }
        private static readonly ILog _log = LogManager.GetLogger(typeof( LabwareDatabase));
        private Dictionary<string,ILabware> Cache { get; set; }

        private LabwareEditor _editor { get; set; }
        public event EventHandler LabwareChanged;


        // needed to add this to pass to labware editor, which I had to remove from MEF because
        // it was causing issues with redisplaying the dialog
        [Import]
        public ExternalDataRequesterInterface DataRequesterInterface { get; set; }

        [ImportingConstructor]
        public LabwareDatabase( [Import("LabwareDatabase.filename")] string database_path)
        {
            try {
                DBIntegration = new DatabaseIntegration( "Data Source=" + database_path);
            } catch( Exception ex) {
                _log.Fatal( "Failed to load labware database", ex);
                throw;
            }
            Cache = new Dictionary<string,ILabware>();
        }

        public void ReloadLabware()
        {
            lock( this) {
                // the act of clearing out the cache should force the database to reload labware properties
                // at the point it receives the request from the application
                Cache.Clear();
                if( LabwareChanged != null) {
                    LabwareChanged( this, new EventArgs());
                }
            }
        }

        private void LogDatabaseConstraintErrors( DataTable dt)
        {
            const int MAX_ERRORS_TO_LOG = 1000;

            if( dt == null || !dt.HasErrors) 
                return;

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat( CultureInfo.CurrentCulture, "ConstraintException while filling {0}", dt.TableName);
            DataRow[] errorRows = dt.GetErrors();
            for( int i=0; (i < MAX_ERRORS_TO_LOG) && (i < errorRows.Length); i++) {
                sb.AppendLine();
                sb.Append(errorRows[i].RowError);
            }
            Console.WriteLine( sb.ToString());
        }

        public bool IsValidLabwareName( string labware_name)
        {
            return labware_name != TeachingJigName;
        }

        /// <summary>
        /// Only adds new labware, and throws an exception if the labware name already exists.
        /// This prevents a user from creating new labware that ends up overwriting something
        /// that already exists in the database.
        /// </summary>
        /// <exception cref="DuplicateLabwareException" />
        /// <param name="labware"></param>
        public long AddLabware( ILabware labware)
        {
            return AddLabware( labware, true);
        }

        private long AddLabware( ILabware labware, bool teaching_jig_check)
        {
            if( labware.Name == TeachingJigName)
                throw new ReservedLabwareException( labware.Name);

            return AddLabwareToDatabase( labware);
        }

        public long CloneLabware( ILabware labware, string new_name)
        {
            Labware new_labware = new Labware( labware as Labware, new_name);
            return AddLabware( new_labware);
        }

        public long AddLid( ILabware parent_plate)
        {
            lock( this) {
                // take the parent labware and make a copy of it, but change the name.  Basically just clone it.
                long lid_id = CloneLabware( parent_plate, parent_plate.Name + " (lid)");
                // now set the LidId property of the parent labware to the newly-created lid
                (parent_plate as Labware).LidId = lid_id;
                UpdateLabware( parent_plate);
                return lid_id;
            }
        }

        public long AddTipBox( ILabware tipbox, ITipProperties properties)
        {
            lock( this) {
                // first, add the tipbox like any ordinary labware
                //! \todo how do we make the DatabaseIntegration class see a TipBox as Labware automatically?
                AddLabware( tipbox as Labware, true);

                // adding the labware sets the labware id, so we use this to set the tip properties
                properties.LabwareId = tipbox.Id;
                DBIntegration.InsertEntity( properties);
                // now that we're caching labware, we need to reload the cache, so force removal of
                // the tip box so that the next request for it causes the tip properties to
                // be reloaded.  This looks like code smell to me...
                Cache.Remove( tipbox.Name);
            }

            // update modified time so cloud syncs can work
            SetLastModifiedTime(DateTime.Now);

            return tipbox.Id;
        }

        /// <summary>
        /// Updates existing labware parameters, or adds labware if it doesn't already exist.
        /// Does NOT rename labware -- if you want to do this, use the RenameLabware method instead.
        /// </summary>
        /// <param name="labware"></param>
        public long UpdateLabware( ILabware labware)
        {
            return AddLabwareToDatabase( labware);
        }

        public void UpdateLabwareNotes( ILabware labware, string notes)
        {
            lock( this) {
                List<Labware> labwares = DBIntegration.SelectEntities<Labware>( String.Format( "WHERE name='{0}'", labware.Name));
                // add if it doesn't exist, otherwise overwrite the existing labware definition
                if( labwares.Count != 0) {
                    labware.Notes = notes;
                    DBIntegration.UpdateEntity( labware, String.Format( "WHERE name='{0}'", labware.Name));
                }
            }

            // update modified time so cloud syncs can work
            SetLastModifiedTime(DateTime.Now);
        }

        public long UpdateTipBox( TipBox tipbox)
        {
            long id = 0;
            lock( this) {
                id = UpdateLabware( tipbox);
                //! \todo kind of unfortunate that the TipProperty has to have its Id synchronized
                // with the labware object like this -- look into this
                tipbox.TipProperties.LabwareId = id;
                // delete all of the tip properties
                DBIntegration.DeleteEntities( typeof(TipProperties), String.Format( "WHERE labware_id={0}", id));
                // save all of the properties
                // I use the delete-save approach instead of the UPDATE approach to try
                // to prevent buildup of cruft in the database
                DBIntegration.InsertEntity( tipbox.TipProperties);
            }

            // update modified time so cloud syncs can work
            SetLastModifiedTime(DateTime.Now);

            return id;
        }

        /// <summary>
        /// Inserts OR updates labware in the database
        /// </summary>
        /// <param name="labware"></param>
        private long AddLabwareToDatabase( ILabware labware)
        {
            long labware_id = 0;

            lock( this) {
                List<Labware> labwares = DBIntegration.SelectEntities<Labware>( String.Format( "WHERE name='{0}'", labware.Name));
                // add if it doesn't exist, otherwise overwrite the existing labware definition
                if( labwares.Count == 0) {
                    labware_id = DBIntegration.InsertEntity( labware);
                    (labware as Labware).Id = labware_id;
                    AddLabwarePropertiesForNewLabware( labware);
                } else {                
                    DBIntegration.UpdateEntity( labware, String.Format( "WHERE name='{0}'", labware.Name));
                    RemoveLabwareProperties( labware);
                    AddLabwarePropertiesForNewLabware( labware);
                }
                // DKM 2012-04-25 fixed database validation issues found and fixed by Giles in r5507.  Not adding to cache messed up
                //                the editing and display of multiple labware properties, so I now reload the labware object
                //                before adding to the cache.
                Cache.Remove( labware.Name);
                // by removing the labware from the cache, calling GetLabware immediately afterward results in a database query, which is what we want.
                var newly_added_labware = GetLabware( labware.Name);
            }

            // update modified time so cloud syncs can work
            SetLastModifiedTime(DateTime.Now);

            return labware_id;
        }

        private void RemoveLabwareProperties( ILabware labware)
        {
            lock( this) {
                DBIntegration.DeleteEntities( typeof(LabwarePropertyValue), String.Format( "WHERE labware_id={0}", labware.Id));
                Cache.Remove( labware.Name);
            }

            // update modified time so cloud syncs can work
            SetLastModifiedTime(DateTime.Now);
        }

        private void AddLabwarePropertiesForNewLabware( ILabware labware)
        {
            lock( this) {
                // loop over the properties and add to the database
                // DKM 2012-04-25 added this for better performance
                DBIntegration.BeginBatchTransaction();
                foreach( KeyValuePair<string, object> kvp in labware.Properties) {
                    // get the type of the property
                    string property_name = kvp.Key;
                    string property_value = kvp.Value.ToString();
                
                    // given the name of the property, we have to get the labware property id
                    // from the labware_properties table so we can save the right entry in the
                    // labware_property_values table
                    List<LabwareProperty> labware_property_templates = DBIntegration.SelectEntities<LabwareProperty>( String.Format( "WHERE name='{0}'", property_name));
                    // there should only be ONE property in the labware_properties table with this name
                    Debug.Assert( labware_property_templates.Count == 1);
                    if( labware_property_templates.Count == 0)
                        throw new UnknownLabwarePropertyTypeException( property_name);

                    LabwareProperty labware_property_template = labware_property_templates[0];

                    // now create the entry in the labware_property_values table
                    LabwarePropertyValue labware_property_value = new LabwarePropertyValue { LabwareId=labware.Id, PropertyId=labware_property_template.Id, PropertyValue=property_value };
                    DBIntegration.InsertEntity( labware_property_value);
                }
                DBIntegration.CommitBatchTransaction();
                Cache.Remove( labware.Name);
            }

            // update modified time so cloud syncs can work
            SetLastModifiedTime(DateTime.Now);
        }

        internal void AddLabwareProperty( ILabwareProperty property)
        {
            lock( this) {
                string name = property.Name;

                if( !IsLegalPropertyName( name))
                    throw new IllegalLabwarePropertyNameException( name);

                // see if we have this property in the database already
                List<LabwareProperty> labware_properties = DBIntegration.SelectEntities<LabwareProperty>( "WHERE name='" + name + "'");
                // shouldn't be more than one... isn't this not even allowed by the database schema???
                if( labware_properties.Count > 1)
                    throw new DuplicateLabwarePropertyException( name);
                // now we'll either add a new labware property, or we're going to overwrite a pre-existing one
                if( labware_properties.Count == 0) // add a new one
                    DBIntegration.InsertEntity( property);
                else
                    DBIntegration.UpdateEntity( property, "WHERE name='" + name + "'");
            }

            // update modified time so cloud syncs can work
            SetLastModifiedTime(DateTime.Now);
        }

        public void RenameLabware( ILabware labware, string new_name)
        {
            lock( this) {
                DBIntegration.UpdateEntityAttribute( labware, "name", new_name, "WHERE id=" + labware.Id);
                Cache.Remove( labware.Name);
                Cache.Add( new_name, labware);
            }

            // update modified time so cloud syncs can work
            SetLastModifiedTime(DateTime.Now);
        }

        public void RenameLabware( string old_name, string new_name)
        {
            ILabware old_labware = GetLabware( old_name); // throws LabwareNotFoundException
            RenameLabware( old_labware, new_name);
        }

        public void DeleteLabware( string name)
        {
            lock( this) {
                // get the labware id from the database before deleting
                ILabware labware = GetLabware( name);

                // if the labware has an associate lid, delete that first
                if( labware.LidId != 0) {
                    DBIntegration.DeleteEntities( typeof(Labware), String.Format( "WHERE id='{0}'", labware.LidId));
                    DBIntegration.DeleteEntities( typeof(LabwarePropertyValue), String.Format( "WHERE labware_id='{0}'", labware.LidId));
                }

                // get the tip property before deleting 
                DBIntegration.DeleteEntities( typeof(Labware), String.Format( "WHERE name='{0}'", name));
                // delete tipbox properties, if any, using the previously-cached labware/tipbox id
                DBIntegration.DeleteEntities( typeof(TipProperties), String.Format( "WHERE labware_id={0}", labware.Id));
                Cache.Remove( name);
            }

            // update modified time so cloud syncs can work
            SetLastModifiedTime(DateTime.Now);
        }

        public List<ILabwareProperty> GetLabwareProperties()
        {
            lock( this) {
                List<LabwareProperty> properties = DBIntegration.SelectEntities<LabwareProperty>( "");
                return properties.Select( x => x).Cast<ILabwareProperty>().ToList();
            }
        }

        internal ITipProperties GetTipBoxProperties( long id)
        {
            lock( this) {
                //! \todo use cache
                List<TipProperties> tip_properties = DBIntegration.SelectEntities<TipProperties>( String.Format( "WHERE labware_id='{0}'", id));
                if( tip_properties.Count() == 0)
                    return null;
                Debug.Assert( tip_properties.Count() <= 1);
                return tip_properties[0];
            }
        }

        public ILabware GetLabware( string labware_name)
        {
            lock( this) {
                // look in the cache first to speed up execution
                if( Cache.ContainsKey( labware_name))
                    return Cache[labware_name];

                // first find the labware
                List<Labware> labwares = DBIntegration.SelectEntities<Labware>( String.Format( "WHERE name='{0}'", labware_name));
                if( labwares.Count == 0)
                    throw new LabwareNotFoundException( labware_name);
                Labware labware = labwares[0];
                // unescape the labware notes
                labware.Notes = DatabaseIntegration.UnescapeString( labware.Notes);
                // now given the labware, we can use its ID to grab its properties and store them
                // in the labware object
                List<LabwarePropertyValue> property_values = DBIntegration.SelectEntities<LabwarePropertyValue>( String.Format( "WHERE labware_id={0}", labware.Id));

                foreach( LabwarePropertyValue lpv in property_values) {
                    // look through the labware_properties table to find the matching labware property
                    List<LabwareProperty> properties = DBIntegration.SelectEntities<LabwareProperty>( String.Format( "WHERE id={0}", lpv.PropertyId));
                
                    // DKM I think we should eliminate this sort of logic if the database schema is 
                    //     designed to prevent it in the first place.
                    // should be at most one
                    Debug.Assert( properties.Count <= 1);
                    if( properties.Count > 1)
                        _log.WarnFormat( "The labware named '{0}' has conflicting properties.  Check the labware_properties table for duplicate ids.", labware_name);

                    if( properties.Count == 0) {
                        _log.WarnFormat( "The labware named '{0}' has a value assigned to a property that no longer exists.", labware_name);
                        continue;
                    }

                    LabwareProperty property = properties[0];

                    // with the property info, we have to cast the object stored in the labware
                    // properties property to the proper type before saving it
                    switch( property.Type) {
                        case (Int64)LabwarePropertyType.INTEGER:
                            if( lpv.PropertyValue != "")
                                labware[property.Name] = int.Parse( lpv.PropertyValue);
                            break;
                        case (Int64)LabwarePropertyType.STRING:
                            labware[property.Name] = lpv.PropertyValue;
                            break;
                        case (Int64)LabwarePropertyType.DOUBLE:
                            if( lpv.PropertyValue != "")
                                labware[property.Name] = double.Parse( lpv.PropertyValue);
                            break;
                        case (Int64)LabwarePropertyType.BOOL:
                            if( lpv.PropertyValue != "")
                                labware[property.Name] = bool.Parse( lpv.PropertyValue);
                            break;
                    }
                }

                // now that we have the labware properties, check to see if there are tipbox properties
                // if there are, then we actually have a tipbox.  Create a new TipBox object and set
                // its properties accordingly
                List<TipProperties> tip_properties = DBIntegration.SelectEntities<TipProperties>( String.Format( "WHERE labware_id={0}", labware.Id));
                // return labware as-is if there aren't any tip properties
                if( tip_properties.Count() == 0) {
                    Cache.Add( labware_name, labware);
                    return labware;
                }
                Debug.Assert( tip_properties.Count() == 1);
                // otherwise, create TipBox and add properties
                TipBox tipbox = new TipBox( labware, tip_properties[0]);
                Cache.Add( labware_name, tipbox);
                return tipbox;
            }
        }

        /// <summary>
        /// Gets labware by ID. Currently only used for lidding operations
        /// </summary>
        /// <remarks>
        /// This is an inefficient call!  I have to query the database once to get the labware ID, which is
        /// in the returned labware object.  But then we technically need to get the properties as well (although
        /// not for lidding), so I call the GetLabware( name) method to fill everything.  Therefore, this
        /// implementation has one extra set of database operations.  Ideally, I would just do a direct database
        /// query to get the labware ID, but this is easiest for now.
        /// </remarks>
        /// <param name="labware_id"></param>
        /// <returns></returns>
        public ILabware GetLabware( long labware_id)
        {
            Labware labware = DBIntegration.SelectEntities<Labware>( String.Format( "WHERE id='{0}'", labware_id)).First();
            return GetLabware( labware.Name);
        }

        public ILabware this[string labware_name]
        {
            get {
                return GetLabware( labware_name);
            }
        }

        public List<string> GetLabwareNames()
        {
            lock( this) {
                return new List<string>( (from labware in DBIntegration.SelectEntities<Labware>("") select labware.Name).ToArray());
            }
        }

        public List<string> GetTipBoxNames()
        {
            return GetLabwareNames().Where( i => GetLabware( i) is TipBox).ToList();
        }

        public void ShowEditor()
        {
            _editor = new LabwareEditor( this, DataRequesterInterface);
            _editor.Initialize();
            _editor.ShowDialog();
            _editor.Close();
        }

        protected bool IsLegalPropertyName( string name) // move to shared util, when done!
        {
            return !string.IsNullOrEmpty( name);
        }

        protected bool IsLegalLabwareName( string name) // move to shared util, when done!
        {
            return ( !string.IsNullOrEmpty(name ) && ( name.ToLower() != "teaching jig"));
        }

        public DateTime GetLastSyncTime()
        {
            var labware_timestamps = DBIntegration.SelectEntities<LabwareTimestamps>("WHERE name='" + LAST_SYNC_NAME + "'");
            return labware_timestamps.Count == 0 ? new DateTime(0) : labware_timestamps[0].TimeStamp;
        }

        private const string LAST_SYNC_NAME = "last_sync_time";
        private const string LAST_MOD_NAME = "last_modified_time";

        public void SetLastSyncTime(DateTime time)
        {
            var stamp = new LabwareTimestamps() { Name = LAST_SYNC_NAME, TimeStamp = time };
            var labware_timestamps = DBIntegration.SelectEntities<LabwareTimestamps>("WHERE name='" + LAST_SYNC_NAME + "'");
            if (labware_timestamps.Count == 0)
            {
                DBIntegration.InsertEntity(stamp);
                return;
            }
            DBIntegration.UpdateEntity(stamp, "WHERE name='" + LAST_SYNC_NAME + "'");
        }
        
        public DateTime GetLastModifiedTime()
        {
            var labware_timestamps = DBIntegration.SelectEntities<LabwareTimestamps>("WHERE name='" + LAST_MOD_NAME + "'");
            return labware_timestamps.Count == 0 ? new DateTime(0) : labware_timestamps[0].TimeStamp;
        }

        public void SetLastModifiedTime(DateTime time)
        {
            var stamp = new LabwareTimestamps() { Name = LAST_MOD_NAME, TimeStamp = time };
            var labware_timestamps = DBIntegration.SelectEntities<LabwareTimestamps>("WHERE name='" + LAST_MOD_NAME +"'");
            if (labware_timestamps.Count == 0)
            {
                DBIntegration.InsertEntity(stamp);
                return;
            }
            DBIntegration.UpdateEntity(stamp, "WHERE name='" + LAST_MOD_NAME + "'");
        }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        #region ISystemTool Members

        public void ShowTool()
        {
            ShowEditor();
        }

        public string Name 
        {
            get
            {
                return "Labware editor";
            }
        }

        #endregion
    }
}
