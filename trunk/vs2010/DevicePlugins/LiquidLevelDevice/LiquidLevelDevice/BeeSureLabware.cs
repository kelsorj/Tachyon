using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.LibraryInterfaces;
using BioNex.LiquidLevelDevice;

namespace BioNex.LiquidLevelDevice
{
    public class LightweightLabwareDatabase : ILabwareDatabase
    {
        // used a Tuple because I wanted to use a Dictionary for quick lookups, but need a long to simulate the database ID
        private Dictionary<string,Tuple<long,ILabware>> _labwares;

        public LightweightLabwareDatabase()
        {
            _labwares = new Dictionary<string,Tuple<long,ILabware>>();
        }

        #region ILabwareDatabase Members

        public bool IsValidLabwareName(string labware_name)
        {
            return true;
        }

        public List<string> GetLabwareNames()
        {
            return _labwares.Keys.ToList();
        }

        public List<string> GetTipBoxNames()
        {
            return new List<string>();
        }

        public ILabware GetLabware(string labware_name)
        {
            lock( this) {
                return _labwares[labware_name].Item2;
            }
        }

        public ILabware GetLabware(long labware_id)
        {
            throw new NotImplementedException();
        }

        public List<ILabwareProperty> GetLabwareProperties()
        {
            throw new NotImplementedException();
        }

        public void ShowEditor()
        {
            throw new NotImplementedException();
        }

        public void ReloadLabware()
        {
            throw new NotImplementedException();
        }

        public long UpdateLabware(ILabware labware)
        {
            throw new NotImplementedException();
        }

        public void UpdateLabwareNotes(ILabware labware, string notes)
        {
            throw new NotImplementedException();
        }

        public long AddLabware(ILabware labware)
        {
            lock( this) {
                long id = _labwares.Count;
                _labwares[labware.Name] = new Tuple<long,ILabware>( id, labware);
                return id;
            }
        }

        public long CloneLabware(ILabware labware, string new_name)
        {
            throw new NotImplementedException();
        }

        public void DeleteLabware(string labware_name)
        {
            // not supporting Delete right now, but if it needs to be supported, be aware of the ID generation since it is based on Count
            throw new NotImplementedException();
        }

        public void RenameLabware(string old_name, string new_name)
        {
            throw new NotImplementedException();
        }

        public long AddLid(ILabware parent_plate)
        {
            throw new NotImplementedException();
        }

        public DateTime GetLastSyncTime()
        {
            throw new NotImplementedException();
        }

        public void SetLastSyncTime(DateTime time)
        {
            throw new NotImplementedException();
        }

        public DateTime GetLastModifiedTime()
        {
            throw new NotImplementedException();
        }

        public void SetLastModifiedTime(DateTime time)
        {
            throw new NotImplementedException();
        }

        public event EventHandler LabwareChanged;

        #endregion
    }

    public class LightweightLabware : ILabware
    {
        private string _name;
        private Dictionary<string,object> _properties;

        public LightweightLabware( string name, IBeeSureLabwareProperties properties)
        {
            _name = name;
            _properties = new Dictionary<string,object>();
            // DKM 2012-03-01 this is bad because we can no longer support arbitrary plate types
            _properties[LabwarePropertyNames.NumberOfWells] = properties.NumberOfColumns * properties.NumberOfRows;
            _properties[LabwarePropertyNames.NumberOfRows] = properties.NumberOfRows;
            _properties[LabwarePropertyNames.NumberOfColumns] = properties.NumberOfColumns;
            _properties[LabwarePropertyNames.ColumnSpacing] = properties.ColumnSpacing;
            _properties[LabwarePropertyNames.RowSpacing] = properties.RowSpacing;
            _properties[LabwarePropertyNames.Thickness] = properties.Thickness;
            _properties[LabwarePropertyNames.WellRadius] = properties.WellRadius;
        }

        #region ILabware Members

        public long Id { get { throw new NotImplementedException(); } }

        public string Name { get { return _name; } }

        public string Notes { get; set; }

        public string Tags { get; set; }

        public long LidId { get { throw new NotImplementedException(); } }

        public object this[string property_name]
        {
            get { 
                if( !_properties.ContainsKey( property_name))
                    return null;
                return _properties[property_name];
            }
        }

        public Dictionary<string, object> Properties
        {
            get { return _properties; }
        }

        #endregion
    }

    public class BeeSureLabware : IBeeSureLabwareProperties
    {
        public BeeSureLabware( string name, short num_rows, short num_columns, double row_spacing, double column_spacing, double thickness, double well_radius)
        {
            Name = name;
            NumberOfColumns = num_columns;
            NumberOfRows = num_rows;
            RowSpacing = row_spacing;
            ColumnSpacing = column_spacing;
            Thickness = thickness;
            WellRadius = well_radius;
        }

        #region IBeeSureLabwareProperties Members

        public string Name { get; private set; }
        public short NumberOfRows { get; private set; }
        public short NumberOfColumns { get; private set; }
        public double RowSpacing { get; private set; }
        public double ColumnSpacing { get; private set; }
        public double Thickness { get; private set; }
        public double WellRadius { get; private set; }

        #endregion
    }
}
