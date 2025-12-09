using System;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.IO;
using Microsoft.SPOT.IO;
using System.IO;

namespace FezSdXmlTest
{
    public class Program
    {
        public static void Main()
        {
            SdTest test = new SdTest();
            test.Run();

            // these events don't work as one might expect
            RemovableMedia.Insert += new InsertEventHandler(RemovableMedia_Insert);
            RemovableMedia.Eject += new EjectEventHandler(RemovableMedia_Eject);

            while( true) {
                Thread.Sleep( 100);
            }
        }

        /// <summary>
        /// Doesn't work!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void RemovableMedia_Eject(object sender, MediaEventArgs e)
        {
        }

        /// <summary>
        /// Doesn't work!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void RemovableMedia_Insert(object sender, MediaEventArgs e)
        {
        }

        public class SdTest
        {
            enum Behavior { FormatTest, DetectionTest, FileTest }
            Behavior behavior = Behavior.DetectionTest;
            PersistentStorage sd = null;

            public void Run()
            {
                if( behavior == Behavior.FormatTest) { 
                    sd.MountFileSystem();

                    // volume info
                    Debug.Print( "Number of volumes on SD card: " + VolumeInfo.GetVolumes().Length.ToString());

                    VolumeInfo volume = VolumeInfo.GetVolumes()[0];
                    Debug.Print( "Volume #0 SerialNumber: " + volume.SerialNumber.ToString());
                    Debug.Print( "Volume #0 Name: " + volume.Name);
                    Debug.Print( "Volume #0 VolumeID: " + volume.VolumeID.ToString());
                    Debug.Print( "Volume #0 VolumeLabel: " + volume.VolumeLabel.ToString());

                    for( uint i=1; i<1023; i+=2) {
                        try {
                            Debug.Print( "----------------------------------------------------------------");
                            bool formatted = volume.IsFormatted;
                            Debug.Print( formatted ? "Currently formatted" : "Currently unformatted");

                            // time the formatting
                            DateTime start = DateTime.Now;
                            Debug.Print( "Formatting with option " + i.ToString());
                            volume.Format(i);
                            Debug.Print( "Formatting took " + (DateTime.Now - start).Seconds.ToString() + "s");

                            // get information about the volume that was just formatted
                            Debug.Print( "DeviceFlags: " + volume.DeviceFlags.ToString());
                            Debug.Print( "FileSystem: " + volume.FileSystem.ToString());
                            Debug.Print( "FileSystemFlags: " + volume.FileSystemFlags.ToString());
                            Debug.Print( "TotalFreeSpace: " + volume.TotalFreeSpace.ToString());
                            Debug.Print( "TotalSize: " + volume.TotalSize.ToString());

                            // check formatting again
                            formatted = volume.IsFormatted;
                            Debug.Print( "After formatting, " + (formatted ? "still formatted" : "not formatted"));
                        } catch( Exception ex) {
                            Debug.Print( "Failed to format with option " + i.ToString() + ": " + ex.Message);
                        }
                    }
                } else if( behavior == Behavior.DetectionTest) {
                    RemovableMedia.Insert += new InsertEventHandler(RemovableMedia_Insert);
                    RemovableMedia.Eject += new EjectEventHandler(RemovableMedia_Eject);

                    new Thread( SdMountThread).Start();
                } else if( behavior == Behavior.FileTest) {

                }
            }

            void SdMountThread()
            {
                while( true) {
                    try {
                        bool sd_exists = PersistentStorage.DetectSDCard();
                        if( sd_exists) {
                            Thread.Sleep( 5000);
                            sd_exists = PersistentStorage.DetectSDCard();
                        }

                        if( sd_exists && sd == null) {
                            Debug.Print( "SD card inserted -- mounting SD filesystem");
                            sd = new PersistentStorage( "SD");
                            Debug.Print( "Mounting filesystem");
                            sd.MountFileSystem();
                        } else if( !sd_exists && sd != null) {
                            Debug.Print( "SD card removed -- unmounting filesystem");
                            try {
                                sd.UnmountFileSystem();
                            } catch( Exception) {
                                Debug.Print( "SD filesystem not unmounted cleanly");
                            }
                            sd.Dispose();
                            sd = null;
                            Thread.Sleep( 5000);
                        }
                    } catch( Exception ex) {
                        Debug.Print( ex.Message);
                        if( sd != null) {
                            sd.Dispose();
                            sd = null;
                        }
                    }

                    Thread.Sleep( 100);
                }
            }

            /// <summary>
            /// Doesn't work as expected -- gets fired after we unmount the filesystem
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            void RemovableMedia_Eject(object sender, MediaEventArgs e)
            {
                Debug.Print( "RemovableMedia Eject event fired");
                /*
                try {
                    sd.UnmountFileSystem();
                } catch( Exception ex) {
                    Debug.Print( "Could not unmount filesystem: " + ex.Message);
                }
                 */
            }

            /// <summary>
            /// Doesn't work as expected
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            void RemovableMedia_Insert(object sender, MediaEventArgs e)
            {
                Debug.Print( "RemovableMedia Insert event fired");
                try {
                    if( VolumeInfo.GetVolumes()[0].IsFormatted) {
                        Debug.Print("Available folders:");
                        string[] strs = Directory.GetDirectories(e.Volume.RootDirectory);
                        for (int i = 0; i < strs.Length; i++)
                            Debug.Print(strs[i]);
 
                        Debug.Print("Available files:");
                        strs = Directory.GetFiles(e.Volume.RootDirectory);
                        for (int i = 0; i < strs.Length; i++)
                            Debug.Print(strs[i]);
                    } else {
                        Debug.Print( "SD card is not formatted");
                    }
                } catch( Exception ex) {
                    Debug.Print( "Could not mount filesystem: " + ex.Message);
                }
            }
        }
    }
}
