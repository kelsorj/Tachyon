using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace BioNex.Shared.LibraryInterfaces
{
    static public class PluginUtilities
    {
        static public void SelectHitpickFile( IEnumerable<ILimsTextConverter> converters, out string selected_hitpick_file, out ILimsTextConverter selected_converter, out bool user_plugin_selected)
        {
            selected_hitpick_file = "";
            selected_converter = null;
            user_plugin_selected = false;

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            // get all filters from available hitpick conversion plugins
            dlg.Filter = "XML files (*.xml)|*.xml";
            foreach( ILimsTextConverter converter in converters)
                dlg.Filter += "|" + converter.Filter;
            // probably not the most efficient way to do this, but it works
            List<ILimsTextConverter> converter_list = new List<ILimsTextConverter>();

            converter_list.AddRange( converters);

            if( dlg.ShowDialog() == true) {
                // got a filename, so set it in the GUI
                selected_hitpick_file = dlg.FileName;
                // get the filter index so we can decide what to do with the file
                // need to make sure that the file extension of the selected file matches that of the plugin
                // why did MS make FilterIndex 1-based?
                if( dlg.FilterIndex != 1) {
                    selected_converter = converter_list[dlg.FilterIndex - 2]; // -2 because FilterIndex is 1-based!
                    user_plugin_selected = true;
                } else {
                    user_plugin_selected = false;
                }
            }
        }
    }

    public abstract class ILimsTextConverter
    {
        public abstract string Name { get; }
        public abstract string Filter { get; }
        public abstract string FileExtension { get; }
        public ILabware DefaultSourceLabware { get; set; }
        public ILabware DefaultDestinationLabware { get; set; }
        public string DefaultLiquidProfile { get; set; }
        public double DefaultTransferVolume { get; set; }
        public double DefaultAspirateDistanceFromWellBottom { get; set; }
        public double DefaultDispenseDistanceFromWellBottom { get; set; }
        public virtual string GetConvertedHitpickFile( string customer_filepath)
        {
            throw new NotImplementedException();
        }
        public virtual string GetConvertedHitpickFile( string customer_filepath, IEnumerable<string> available_destination_barcodes,
                                                       ILabware destination_labware)
        {
            throw new NotImplementedException();
        }
    }

    public interface ILimsOutputTransferLog
    {
        void Open( string filepath);
        void LogTransfer( string source_barcode, string source_well, string destination_barcode, string destination_well,
                          double volume_uL, DateTime timestamp);
        void LogLiquidLevel( string plate_barcode, IDictionary< string, double> well_to_volume_map, DateTime timestamp);
        void Close();
    }
}
