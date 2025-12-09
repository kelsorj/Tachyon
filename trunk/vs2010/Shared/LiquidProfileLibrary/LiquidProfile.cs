using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;

namespace BioNex.Shared.LiquidProfileLibrary
{
    [ DatabaseTableAttribute( "liquid_profiles")]
    public class LiquidProfile : ILiquidProfile
    {
        [ DatabaseColumnAttribute( "id", DatabaseColumnType.INTEGER, DatabaseColumnFlags.PRIMARY_KEY | DatabaseColumnFlags.AUTOINCREMENT)]
        public long    Id { get; set; }
        [ DatabaseColumnAttribute( "name", DatabaseColumnType.TEXT, DatabaseColumnFlags.UNIQUE)]
        public string  Name { get; set; }
        [ DatabaseColumnAttribute( "is_factory_profile", DatabaseColumnType.BOOLEAN)]
        public bool    IsFactoryProfile { get; set; }
        [ DatabaseColumnAttribute( "is_mixing_profile", DatabaseColumnType.BOOLEAN)]
        public bool    IsMixingProfile { get; set; }
        [ DatabaseColumnAttribute( "base_id", DatabaseColumnType.INTEGER)]
        public long    BaseId { get; set; }
        [ DatabaseColumnAttribute( "syringe_type_id", DatabaseColumnType.INTEGER)]
        public long    SyringeTypeId { get; set; }
        [ DatabaseColumnAttribute( "syringe_serial_number", DatabaseColumnType.TEXT)]
        public string  SyringeSerialNumber { get; set; }
        [ DatabaseColumnAttribute( "tip_type_id", DatabaseColumnType.INTEGER)]
        public long    TipTypeId { get; set; }
        [ DatabaseColumnAttribute( "pre_aspirate_volume", DatabaseColumnType.FLOAT)]
        public double  PreAspirateVolume { get; set; }
        [ DatabaseColumnAttribute( "rate_to_aspirate", DatabaseColumnType.FLOAT)]
        public double  RateToAspirate { get; set; }
        [ DatabaseColumnAttribute( "max_accel_during_aspirate", DatabaseColumnType.FLOAT)]
        public double  MaxAccelDuringAspirate { get; set; }
        [ DatabaseColumnAttribute( "post_aspirate_delay", DatabaseColumnType.FLOAT)]
        public double  PostAspirateDelay { get; set; }
        [ DatabaseColumnAttribute( "post_dispense_volume", DatabaseColumnType.FLOAT)]
        public double  PostDispenseVolume { get; set; }
        [ DatabaseColumnAttribute( "rate_to_dispense", DatabaseColumnType.FLOAT)]
        public double  RateToDispense { get; set; }
        [ DatabaseColumnAttribute( "max_accel_during_dispense", DatabaseColumnType.FLOAT)]
        public double  MaxAccelDuringDispense { get; set; }
        [ DatabaseColumnAttribute( "post_dispense_delay", DatabaseColumnType.FLOAT)]
        public double  PostDispenseDelay { get; set; }
        [ DatabaseColumnAttribute( "track_fluid_height", DatabaseColumnType.BOOLEAN)]
        public bool    TrackFluidHeight { get; set; }
        [ DatabaseColumnAttribute( "z_move_during_aspirating", DatabaseColumnType.FLOAT)]
        public double  ZMoveDuringAspirating { get; set; }
        [ DatabaseColumnAttribute( "z_move_during_dispensing", DatabaseColumnType.FLOAT)]
        public double  ZMoveDuringDispensing { get; set; }
        [ DatabaseColumnAttribute( "time_to_enter_liquid", DatabaseColumnType.FLOAT)]
        public double  TimeToEnterLiquid { get; set; }
        [ DatabaseColumnAttribute( "time_to_exit_liquid", DatabaseColumnType.FLOAT)]
        public double  TimeToExitLiquid { get; set; }
        [ DatabaseColumnAttribute( "pre_aspirate_mix_liquid_profile", DatabaseColumnType.TEXT)]
        public string  PreAspirateMixLiquidProfile { get; set; }
        [ DatabaseColumnAttribute( "pre_aspirate_mix_cycles", DatabaseColumnType.INTEGER)]
        public long    PreAspirateMixCycles { get; set; }
        [ DatabaseColumnAttribute( "pre_aspirate_mix_volume", DatabaseColumnType.FLOAT)]
        public double  PreAspirateMixVolume { get; set; }
        [ DatabaseColumnAttribute( "post_dispense_mix_liquid_profile", DatabaseColumnType.TEXT)]
        public string  PostDispenseMixLiquidProfile { get; set; }
        [ DatabaseColumnAttribute( "post_dispense_mix_cycles", DatabaseColumnType.INTEGER)]
        public long    PostDispenseMixCycles { get; set; }
        [ DatabaseColumnAttribute( "post_dispense_mix_volume", DatabaseColumnType.FLOAT)]
        public double  PostDispenseMixVolume { get; set; }

        public LiquidProfile( string name)
        {
            Name = name;
            IsFactoryProfile = true;
            IsFactoryProfile = false;
            BaseId = 0;
            SyringeTypeId = 0;
            SyringeSerialNumber = "";
            TipTypeId = 0;
            PreAspirateVolume = 0;
            RateToAspirate = 125;
            MaxAccelDuringAspirate = 100;
            PostAspirateDelay = 0;
            PostDispenseVolume = 0;
            RateToDispense = 125;
            MaxAccelDuringDispense = 100;
            PostDispenseDelay = 0;
            TrackFluidHeight = false;
            ZMoveDuringAspirating = 0;
            ZMoveDuringDispensing = 0;
            TimeToEnterLiquid = 1;
            TimeToExitLiquid = 1;
            PreAspirateMixLiquidProfile = "";
            PreAspirateMixCycles = 0;
            PreAspirateMixVolume = 0.0;
            PostDispenseMixLiquidProfile = "";
            PostDispenseMixCycles = 0;
            PostDispenseMixVolume = 0.0;
            calibration_curve_ = new PiecewiseLinearFunction();
            calibration_curve_[ 0] = 0;
            calibration_curve_[ 1000] = 1000;
        }

        public LiquidProfile()
            : this( "")
        {
        }

        public SortedList< double, double> GetCalibrationData()
        {
            return new SortedList< double, double>( calibration_curve_);
        }

        public void SetCalibrationData( SortedList< double, double> calibration_data)
        {
            // DKM 2011-11-24 maybe do this instead?  I added a constructor to handle it in PiecewiseLinearFunction.cs
            // calibration_curve_ = new PiecewiseLinearFunction( calibration_data);

            calibration_curve_.Clear();
            foreach( KeyValuePair< double, double> calibration_datum in calibration_data){
                calibration_curve_.Add( calibration_datum.Key, calibration_datum.Value);
            }
        }

        public double GetAdjustedVolume( double requested_volume, out bool is_interpolated)
        {
            return calibration_curve_.GetOutput( requested_volume, out is_interpolated);
        }

        public override string ToString()
        {
            return DatabaseIntegration.EntityToString( this);
        }

        protected PiecewiseLinearFunction calibration_curve_;
    }

    [ DatabaseTableAttribute( "liquid_calibration_data", "PRIMARY KEY (liquid_profile_id, requested_volume)", "FOREIGN KEY (liquid_profile_id) REFERENCES liquid_profiles(id) ON DELETE CASCADE")]
    public class LiquidCalibrationDatum : ILiquidCalibrationDatum
    {
        // [ DatabaseColumnAttribute( "id", DatabaseColumnType.INTEGER, DatabaseColumnFlags.PRIMARY_KEY | DatabaseColumnFlags.AUTOINCREMENT)]
        // public long    Id              { get; set; }
        [ DatabaseColumnAttribute( "liquid_profile_id", DatabaseColumnType.INTEGER)]
        public long    LiquidProfileId { get; set; }
        [ DatabaseColumnAttribute( "requested_volume", DatabaseColumnType.FLOAT)]
        public double  RequestedVolume { get; set; }
        [ DatabaseColumnAttribute( "volume_offset", DatabaseColumnType.FLOAT)]
        public double  VolumeOffset    { get; set; }

        public LiquidCalibrationDatum( long liquid_profile_id, double requested_volume, double volume_offset)
        {
            LiquidProfileId = liquid_profile_id;
            RequestedVolume = requested_volume;
            VolumeOffset = volume_offset;
        }

        public LiquidCalibrationDatum()
            : this( 0, 0, 0)
        {
        }

        public override string ToString()
        {
            return DatabaseIntegration.EntityToString( this);
        }
    }

    [PartCreationPolicy(CreationPolicy.Shared)]
    [ Export( typeof( BioNex.Shared.LibraryInterfaces.ILiquidProfileLibrary))]
    [ Export( typeof( BioNex.Shared.LibraryInterfaces.ISystemSetupEditor))]
    public class LiquidProfileLibrary : ILiquidProfileLibrary, ISystemSetupEditor, IDisposable
    {
        public class LiquidProfileNotFoundException : Exception
        {
        }

        public class DuplicateLiquidProfilesFoundException : Exception
        {
        }

        public class IllegalLiquidProfileNameException : Exception
        {
        }

        [ ImportingConstructor]
        public LiquidProfileLibrary( [ Import( "LiquidProfileLibrary.filename")] string database_path)
        {
            database_integration_ = new DatabaseIntegration( "Data Source=" + database_path);
        }

        /// <remarks>
        /// Is this for unit testing only?  If so, try making the method internal, and then
        /// mark this file with [assembly:InternalsVisibleTo( "LiquidProfileLibraryTest")]
        /// </remarks>
        /// <returns></returns>
        public DatabaseIntegration GetDatabaseIntegration()
        {
            return database_integration_;
        }

        public List< string> EnumerateLiquidProfileNames()
        {
            List< LiquidProfile> liquid_profiles = database_integration_.SelectEntities< LiquidProfile>( "");
            List< string> liquid_profile_names = new List< string>();
            foreach( LiquidProfile liquid_profile in liquid_profiles){
                liquid_profile_names.Add( liquid_profile.Name);
            }
            return liquid_profile_names;
        }

        public List< LiquidProfile> LoadLiquidProfiles()
        {
            // get everything out of the "liquid_profiles" table.
            List< LiquidProfile> liquid_profiles = database_integration_.SelectEntities< LiquidProfile>( "");

            // get everything out of the "liquid_calibration_data" table.
            List< LiquidCalibrationDatum> calibration_data = database_integration_.SelectEntities< LiquidCalibrationDatum>( "");

            // reorganize the calibration data into sorted lists (calibration tables) -- one for each "liquid_profile_id".
            // create a dictionary of "liquid_profile_id"s to calibration tables (sorted lists of doubles to doubles).
            Dictionary< long, SortedList< double, double>> reorganized_calibration_data = new Dictionary< long, SortedList< double, double>>();
            // for each calibration datum:
            foreach( LiquidCalibrationDatum calibration_datum in calibration_data){
                // get the calibration datum's "liquid_profile_id".
                long profile_id = calibration_datum.LiquidProfileId;
                // if there isn't a calibration table in the dictionary for that "liquid_profile_id", then create a calibration table.
                if( !reorganized_calibration_data.ContainsKey( profile_id)){
                    reorganized_calibration_data[ profile_id] = new SortedList< double, double>();
                }
                // enter the calibration datum into the calibration table.
                double adjusted_volume = calibration_datum.RequestedVolume + calibration_datum.VolumeOffset;
                reorganized_calibration_data[ profile_id][ calibration_datum.RequestedVolume] = adjusted_volume;
            }

            // associate calibration tables to the liquid profiles.
            // for each liquid profile:
            foreach( LiquidProfile liquid_profile in liquid_profiles){
                // if there is a calibration table in the dictionary, then set the liquid profile's calibration data to that calibration table.
                if( reorganized_calibration_data.ContainsKey( liquid_profile.Id)){
                    liquid_profile.SetCalibrationData( reorganized_calibration_data[ liquid_profile.Id]);
                }
            }

            // return all the liquid profiles.
            return liquid_profiles;
        }

        public ILiquidProfile LoadLiquidProfileByName( string name)
        {
            if( !IsLegalName( name)){
                throw new IllegalLiquidProfileNameException();
            }

            List< LiquidProfile> liquid_profiles = database_integration_.SelectEntities< LiquidProfile>( "WHERE name = '" + name + "'");

            // DKM 2011-11-24 this shouldn't be possible, right?  there is a UNIQUE constraint on the name field.
            if( liquid_profiles.Count > 1){
                throw new DuplicateLiquidProfilesFoundException();
            }

            if( liquid_profiles.Count == 0){
                throw new LiquidProfileNotFoundException();
            }

            LiquidProfile liquid_profile = liquid_profiles[ 0];
            List< LiquidCalibrationDatum> calibration_data = database_integration_.SelectEntities< LiquidCalibrationDatum>( "WHERE liquid_profile_id = " + liquid_profile.Id);
            SortedList< double, double> data = new SortedList< double, double>();
            foreach( LiquidCalibrationDatum calibration_datum in calibration_data){
                double adjusted_volume = calibration_datum.RequestedVolume + calibration_datum.VolumeOffset;
                data[ calibration_datum.RequestedVolume] = adjusted_volume;
            }
            liquid_profile.SetCalibrationData( data);
            return liquid_profile;
        }

        public void SaveLiquidProfileByName( ILiquidProfile liquid_profile)
        {
            string name = liquid_profile.Name;

            if( !IsLegalName( name)){
                throw new IllegalLiquidProfileNameException();
            }

            List< LiquidProfile> liquid_profiles = database_integration_.SelectEntities< LiquidProfile>( "WHERE name = '" + name + "'");

            if( liquid_profiles.Count > 1){
                throw new DuplicateLiquidProfilesFoundException();
            }

            if( liquid_profiles.Count == 0){
                liquid_profile.Id = database_integration_.InsertEntity( liquid_profile);
            } else{
                liquid_profile.Id = liquid_profiles[ 0].Id;
                database_integration_.UpdateEntity( liquid_profile, "WHERE name = '" + name + "'");
            }

            database_integration_.DeleteEntities( typeof ( LiquidCalibrationDatum), "WHERE liquid_profile_id = " + liquid_profile.Id);

            foreach( KeyValuePair< double, double> kvp in liquid_profile.GetCalibrationData()){
                double requested_volume = kvp.Key;
                double adjusted_volume = kvp.Value;
                double volume_offset = adjusted_volume - requested_volume;
                // refs #401 - specify 3 sigfigs so DB doesn't store a bizillion zeroes
                LiquidCalibrationDatum calibration_datum = new LiquidCalibrationDatum( liquid_profile.Id, requested_volume, Math.Round( volume_offset, 3));
                database_integration_.InsertEntity( calibration_datum);
            }
        }

        public void DeleteLiquidProfileByName( string name)
        {
            List< LiquidProfile> liquid_profiles = database_integration_.SelectEntities< LiquidProfile>( "WHERE name = '" + name + "'");

            if( liquid_profiles.Count == 1){
                long liquid_profile_id = liquid_profiles[ 0].Id;
                database_integration_.DeleteEntities( typeof( LiquidCalibrationDatum), "WHERE liquid_profile_id = " + liquid_profile_id);
            }

            database_integration_.DeleteEntities( typeof( LiquidProfile), "WHERE name = '" + name + "'");
        }

        protected bool IsLegalName( string name) // move to shared util, when done!
        {
            return !string.IsNullOrEmpty( name);
        }

        public void ShowEditor()
        {
            LiquidProfileEditor editor = new LiquidProfileEditor( this);
            editor.ShowDialog();
            editor.Close();
        }

        public string Name
        {
            get
            {
                return "Liquid-profile editor";
            }
        }

        public void ShowTool()
        {
            ShowEditor();
        }

        public DatabaseIntegration database_integration_;
        protected Dictionary< string, LiquidProfile> liquid_profiles_;

        ///////////////////////////////////////////////////////////////////////
        // public void PrintDictionary< TABLE, U>( Dictionary< TABLE, U> d){
        //     foreach( KeyValuePair< TABLE, U> kvp in d){
        //         Console.WriteLine( kvp.Key + "=" + kvp.Value);
        //     }
        // }

        #region IDisposable Members

        public void Dispose()
        {
            database_integration_.Dispose();
        }

        #endregion
    }
}
