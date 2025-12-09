using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;

namespace BioNex.SynapsisPrototype
{
    /// <summary>
    /// Interaction logic for VersionDialog.xaml
    /// </summary>
    public partial class VersionDialog : Window
    {
        public ObservableCollection<FileVersionInfo> FileVersions { get; set; }

        public VersionDialog()
        {
            InitializeComponent();
            FileVersions = new ObservableCollection<FileVersionInfo>();
            DataContext = this;
            // get all of the files in the app folder and plugins folder
            DirectoryInfo dir = new DirectoryInfo( BioNex.Shared.Utils.FileSystem.GetAppPath());
            FileInfo[] files = dir.GetFiles();
            foreach( FileInfo fi in files)
                FileVersions.Add( FileVersionInfo.GetVersionInfo( fi.FullName));
            // plugins folder
            dir = new DirectoryInfo( BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\plugins");
            files = dir.GetFiles();
            foreach( FileInfo fi in files)
                FileVersions.Add( FileVersionInfo.GetVersionInfo( fi.FullName));
        }

        /// <summary>
        /// Simple way to dump all of the filenames and their respective assembly version numbers to the main log
        /// </summary>
        /// <returns></returns>
        public string GetFileVersionsString()
        {
            StringBuilder sb = new StringBuilder();
            foreach( var fvi in FileVersions) {
                sb.AppendLine( String.Format( "{0} version {1}", fvi.FileName, fvi.FileVersion));
            }
            return sb.ToString();
        }
    }
}
