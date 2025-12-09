using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace BioNex.Shared.Utils
{
    public static class FileSystem
    {
        /// <summary>
        /// returns the executing assembly's path, NOT terminated with a '\'
        /// </summary>
        /// <returns></returns>
        static public string GetAppPath()
        {
            string exe_path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            return exe_path.Substring( 0, exe_path.LastIndexOf( '\\'));
            //return Environment.GetFolderPath( Environment.SpecialFolder.CommonApplicationData) + "\\BioNex\\Synapsis";
        }

        /// <summary>
        /// returns the full path of the exe
        /// </summary>
        /// <returns></returns>
        static public string GetExePath()
        {
            return GetAppPath() + "\\" + System.IO.Path.GetFileName( System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        }

        /// <summary>
        /// returns the current module's path, NOT terminated with a '\'
        /// </summary>lo
        /// <returns></returns>
        static public string GetModulePath()
        {
            string module_path = System.Reflection.Assembly.GetCallingAssembly().Location;
            return module_path.Substring( 0, module_path.LastIndexOf( '\\'));
        }

        static public bool IsAbsolutePath( string path)
        {
            return System.IO.Path.IsPathRooted( path);
        }

        static public string ConvertToAbsolutePath( string relative_or_absolute_path)
        {
            // GB 23-1-2012 return app path rather than app path + \\ for empty path to make empty path consistent with non-empty path
            if (string.IsNullOrEmpty(relative_or_absolute_path))
                return GetAppPath();
            if (string.IsNullOrWhiteSpace(relative_or_absolute_path))
                return GetAppPath();

            // DKM 2010-09-06 not sure why "\filename.txt" is considered rooted, but in any case I
            //                don't want this -- I want to treat is as a relative path
            //                to the exe.
            //
            // GB 23-1-2012 "\filename.txt" is considered rooted because "\" is the absolute root directory.
            //              You should be using ".\" for a relative to app path.  I suppose we're stuck with your way now though...
            //
            if( relative_or_absolute_path.IsAbsolutePath() && !relative_or_absolute_path.StartsWith( "\\"))
                return relative_or_absolute_path;
            
            return GetAppPath() + "\\" + relative_or_absolute_path;
        }

        static public T LoadXmlConfiguration<T>( string filepath) where T : new()
        {
            if (!File.Exists(filepath))
            {
                // no config file, so just use default values
                T obj = new T();
                SaveXmlConfiguration<T>(obj, filepath);
                return obj;
            }
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                FileStream reader = new FileStream(filepath.ToAbsoluteAppPath(), FileMode.Open);
                T temp = (T)serializer.Deserialize(reader);
                reader.Close();
                return temp;
            } catch( FileNotFoundException) {
                // no config file, so just use default values
                T obj = new T();
                SaveXmlConfiguration<T>( obj, filepath);
                return obj;
            }
        }

        static public T LoadXmlConfiguration<T>( string filepath, T default_obj) where T : new()
        {
            if (default_obj == null)
                default_obj = new T();
            if (!File.Exists(filepath))
            {
                SaveXmlConfiguration<T>(default_obj, filepath);
                return default_obj;
            }
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                FileStream reader = new FileStream(filepath.ToAbsoluteAppPath(), FileMode.Open);
                T temp = (T)serializer.Deserialize(reader);
                reader.Close();
                return temp;
            } catch( FileNotFoundException) {
                SaveXmlConfiguration<T>( default_obj, filepath);
                return default_obj;
            } catch( Exception) {
                return default_obj;
            }
        }

        static public void SaveXmlConfiguration<T>( T config, string filepath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            // Ensure all directories in the specified path exist
            var directory_name = filepath.GetDirectoryFromFilePath();
            if (!Directory.Exists(directory_name))
                Directory.CreateDirectory(directory_name);

            // need to erase previous file, because just writing to the same file will leave
            // existing data intact if it doesn't get overwritten
            if( File.Exists( filepath))
                File.Delete( filepath);
            FileStream writer = new FileStream( filepath , FileMode.Create);
            serializer.Serialize( writer, config);
            writer.Close();
        }

        static public string ReplaceInvalidFilenameCharacters( this string filename)
        {
            return filename.Replace( '\\', '-').Replace( '/', '-').Replace( ':', '-').Replace( '*', '-')
                           .Replace( '?', '-').Replace( '"', '-').Replace( '<', '-').Replace( '>', '-').Replace( '|', '-');
        }
    }
}
