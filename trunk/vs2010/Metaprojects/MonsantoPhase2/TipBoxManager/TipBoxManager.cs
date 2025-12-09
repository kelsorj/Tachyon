using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;

namespace BioNex.TipBoxManager
{
    [ PartCreationPolicy( CreationPolicy.Shared)]
    [ Export( typeof( ITipBoxManager))]
    [ Export( typeof( ISystemSetupEditor))]
    public class TipBoxManager : ITipBoxManager, ISystemSetupEditor
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private string TipBoxDevice { get; set; }
        private HashSet< TipBoxLocationControl> TipBoxLocations { get; set; }
        private Window DiagnosticsWindow { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public TipBoxManager()
        {
            TipBoxManagerConfiguration configuration = ReadConfiguration();
            if( configuration != null){
                TipBoxDevice = configuration.TipBoxDevice;
                TipBoxLocations = new HashSet< TipBoxLocationControl>( configuration.TipBoxLocations.Select( tbln => new TipBoxLocationControl( tbln, TipBoxLocationControl.LocationStatus.Empty, this)));
            } else{
                TipBoxDevice = "<failed to read TipBoxManager configuration file>";
                TipBoxLocations = new HashSet< TipBoxLocationControl>();
            }
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private TipBoxManagerConfiguration ReadConfiguration()
        {
            try{
                string config_path = "\\config\\TipBoxManagerConfig.xml".ToAbsoluteAppPath();
                using( FileStream reader = new FileStream( config_path, FileMode.Open)){
                    XmlSerializer serializer = new XmlSerializer( typeof( TipBoxManagerConfiguration));
                    TipBoxManagerConfiguration configuration = ( TipBoxManagerConfiguration)serializer.Deserialize( reader);
                    reader.Close();
                    return configuration;
                }
            } catch( Exception){
                return null;
            }
        }
        // ----------------------------------------------------------------------
        #region ITipBoxManager Members
        // ----------------------------------------------------------------------
        Tuple< string, string> ITipBoxManager.AcquireTipBox()
        {
            lock( TipBoxLocations){
                TipBoxLocationControl acquired_tip_box = TipBoxLocations.FirstOrDefault( tbl => tbl.Status == TipBoxLocationControl.LocationStatus.New);
                if( acquired_tip_box != null){
                    acquired_tip_box.Status = TipBoxLocationControl.LocationStatus.InUse;
                    return Tuple.Create< string, string>( TipBoxDevice, acquired_tip_box.Location);
                } else{
                    return null;
                }
            }
        }
        // ----------------------------------------------------------------------
        void ITipBoxManager.ReleaseTipBox( Tuple< string, string> location)
        {
            lock( TipBoxLocations){
                TipBoxLocationControl released_tip_box = TipBoxLocations.FirstOrDefault( tbl => tbl.Location == location.Item2);
                if( released_tip_box != null){
                    released_tip_box.Status = TipBoxLocationControl.LocationStatus.Used;
                }
            }
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #region ISystemSetupEditor Members
        // ----------------------------------------------------------------------
        string ISystemSetupEditor.Name
        {
            get
            {
                return "Tip-box manager";
            }
        }
        // ----------------------------------------------------------------------
        void ISystemSetupEditor.ShowTool()
        {
            if( DiagnosticsWindow == null){
                DiagnosticsWindow = new Window();
                DiagnosticsWindow.Content = new TipBoxManagerEditor( TipBoxLocations);
                DiagnosticsWindow.Title = "Tip-Box Manager";
                DiagnosticsWindow.Closed += new EventHandler( DiagnosticsWindow_Closed);
                DiagnosticsWindow.Width = 480;
                DiagnosticsWindow.Height = 640;
            }
            DiagnosticsWindow.Show();
            DiagnosticsWindow.Activate();
        }
        // ----------------------------------------------------------------------
        void DiagnosticsWindow_Closed( object sender, EventArgs e)
        {
            DiagnosticsWindow.Content = null;
            DiagnosticsWindow = null;
        }
        // ----------------------------------------------------------------------
        #endregion
    }
}
