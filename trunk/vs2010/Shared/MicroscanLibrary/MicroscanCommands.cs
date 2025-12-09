using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.Microscan
{
    public class MicroscanParameterOutOfRangeException : ApplicationException
    {
        public MicroscanParameterOutOfRangeException( String command_name, String parameter_name,
                                                      String parameter_value, String min, String max)
        {

        }
    }

    public class MicroscanCommands
    {
        // I was wrestling with this a little -- I could use public const strings, but
        // I want the usage of this class to be consistent, and not all command are
        // parameterless.  So I opted to use methods to get the commands.

        // parameterless commands
        public static string Read() { return "< >"; }
        public static string StartDecodeRateTest() { return "<C>"; }
        public static string StartPercentRateTest() { return "<Cp>"; }
        public static string StopTest() { return "<J>"; }

        // commands with parameters
#region Calibration Types
        /// <summary>
        /// <para>Disabled: shutter speed is fixed and not part of the calibration process</para>
        /// <para>Enabled: shutter speed will be calibrated to provide the best possible image quality and performance</para>
        /// <para>FastShutter: calibration process will concentrate on achieving the fastest possible shutter setting that
        /// will still provide good performance.  The image quality or contrast may not be as good as what would be
        /// achieved with the Enabled setting.  The calibration process is not designed to choose the fastest shutter
        /// speed that can decode a symbol, but rather to optimize for the fastest shutter speed that still provides good
        /// image quality.</para>
        /// </summary>
        public enum ShutterSpeed { Disabled = 0, Enabled = 1, FastShutter = 2 };
        /// <summary>
        /// <para>Disabled: focus position is fixed and is not part of the calibration process</para>
        /// <para>Enabled: focus position will be calibrated to provide the best possible image quality and performance</para>
        /// <para>QuickFocus: designed to quickly locate the focus setting for an object in the field of view</para>
        /// </summary>
        public enum FocusPosition { Disabled = 0, Enabled = 1, QuickFocus = 2 };
        /// <summary>
        /// <para>Disabled: use current WOI framing until a symbol has been decoded</para>
        /// <para>RowAndColumn: if calibration is successful, frames symbol with the [margin] value</para>
        /// <para>Column: if calibration is successful, the WOI becomes a full column that includes the symbol,
        /// with a margin on the sides of size [margin]</para>
        /// <para>Row: if calibration is successful, the WOI becomes a full row that includes the symbol,
        /// with a margin on the top and bottom of size [margin]</para>
        /// <para>StraightLine: see Quadrus Mini PDF, page 87</para>
        /// <para>StraightLineFramed: see Quadrus Mini PDF, page 87</para>
        /// </summary>
        public enum WOIFraming { Disabled = 0, RowAndColumn = 1, Row = 2, Column = 3, StraightLine = 4, StraightLineFramed = 5 };
        /// <summary>
        /// <para>Low: imager will spend a little effort attempting to decode the given symbol for each parameter configuration</para>
        /// <para>Medium: imager will spend a moderate time attempting to decode the given symbol for each parameter configuration</para>
        /// <para>High: imager will spend a lot of time attempting to decode the given symbol for each parameter configuration</para>
        /// <para>Definable: the processing time for each image frame is defined by the Image Processing Timeout parameter K245</para>
        /// </summary>
        public enum Processing { Low = 0, Medium = 1, High = 2, Definable = 3 }
#endregion
        /// <summary>
        /// Calibrates the reader
        /// </summary>
        /// <param name="enable_gain">
        /// <para>When enabled, gain will be calibrated to provide the best available image quality and performance.
        /// When disabled, gain is fixed and is not part of the calibration process</para></param>
        /// <param name="shutter_speed">See ShutterSpeed type</param>
        /// <param name="focus_position">See FocusPosition type</param>
        /// <param name="enable_symbol_type">
        /// <para>When enabled, autodiscrimination is used during calibration.  All symbologies except for PDF417
        /// and Pharmacode will be considered during calibration.  Any new symbologies successfully decoded during
        /// calibration will remain enabled at the end of the process.  All enabled symbologies will remain enabled.</para>
        /// <para>When disabled, only the current-enabled symbologies will be considered during the calibration process.</para>
        /// </param>
        /// <param name="woi_framing">See WOIFraming type</param>
        /// <param name="woi_margin">Sets the margin size around the calibrated symbol, in pixels [20, 2048]</param>
        /// <param name="processing">See Processing type</param>
        /// <remarks>The line scan height parameter that comes before [processing] is NOT used for imagers!</remarks>
        /// <returns></returns>
        public static string Calibrate( Boolean enable_gain, ShutterSpeed shutter_speed, FocusPosition focus_position,
                                        Boolean enable_symbol_type, WOIFraming woi_framing, Int16 woi_margin,
                                        Processing processing)
        {
            // remember that the line scan height parameter that comes before [processing] is NOT used for imagers!
            return String.Format( "<K529,{0},{1},{2},{3},{4},{5},,{6}><@CAL>", (enable_gain ? 1 : 0), (int)shutter_speed,
                                  (int)focus_position, (enable_symbol_type ? 1 : 0), (int)woi_framing, woi_margin,
                                  (int)processing);
        }

#region ImageFormat Types
        public enum ImageFormat { Bitmap = 0, JPEG = 1, Binary = 2 };
#endregion
        /// <summary>
        /// Requests image from the reader
        /// </summary>
        /// <param name="image_format">The image file format.  See the ImageFormat type.</param>
        /// <param name="image_quality">The image quality for JPEGs only. [1, 100]</param>
        /// <returns></returns>
        public static string EnableImagePush( ImageFormat image_format, Byte image_quality)
        {
            // range validation


            //                           +-set output mode to good read OR bad read so that we 
            //                           |      always get an image when the method is called.
            //                           | +- send data to host
            //                           | |
            //                           V V
            return String.Format( "<K739,3,0,{0},{1}>", (int)image_format, image_quality);
        }

        public static string DisableImagePush()
        {
            return String.Format( "<K739,0>");
        }
    }
}