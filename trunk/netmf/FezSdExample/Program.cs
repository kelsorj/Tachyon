using System;
using System.IO;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.IO;
 
using GHIElectronics.NETMF.IO;
 
namespace AutoMount
{
    public class Program
    {
        public static void Main()
        {
            RemovableMedia.Insert += new InsertEventHandler(RemovableMedia_Insert);
            RemovableMedia.Eject += new EjectEventHandler(RemovableMedia_Eject);
 
            // Start auto mounting thread
            new Thread(SDMountThread).Start();
 
            // Your program goes here
            // ...
 
            Thread.Sleep(Timeout.Infinite);
        }
 
        static void RemovableMedia_Eject(object sender, MediaEventArgs e)
        {
            Debug.Print("SD card ejected");
        }
 
        static void RemovableMedia_Insert(object sender, MediaEventArgs e)
        {
            Debug.Print("SD card inserted");
 
            if (e.Volume.IsFormatted)
            {
                Debug.Print("Available folders:");
                string[] strs = Directory.GetDirectories(e.Volume.RootDirectory);
                for (int i = 0; i < strs.Length; i++)
                    Debug.Print(strs[i]);
 
                Debug.Print("Available files:");
                strs = Directory.GetFiles(e.Volume.RootDirectory);
                for (int i = 0; i < strs.Length; i++)
                    Debug.Print(strs[i]);
            }
            else
            {
                Debug.Print("SD card is not formatted");
            }
        }
 
        public static void SDMountThread()
        {
            PersistentStorage sdPS = null;
            const int POLL_TIME = 500; // check every 500 millisecond
 
            bool sdExists;
            while (true)
            {
                try // If SD card was removed while mounting, it may throw exceptions
                {
                    sdExists = PersistentStorage.DetectSDCard();
 
                    // make sure it is fully inserted and stable
                    if (sdExists)
                    {
                        Thread.Sleep(50);
                        sdExists = PersistentStorage.DetectSDCard();
                    }
 
                    if (sdExists && sdPS == null)
                    {
                        sdPS = new PersistentStorage("SD");
                        sdPS.MountFileSystem();
                    }
                    else if (!sdExists && sdPS != null)
                    {
                        sdPS.UnmountFileSystem();
                        sdPS.Dispose();
                        sdPS = null;
                    }
                }
                catch
                {
                    if (sdPS != null)
                    {
                        sdPS.Dispose();
                        sdPS = null;
                    }
                }
 
                Thread.Sleep(POLL_TIME);
            }
        }
    }
}