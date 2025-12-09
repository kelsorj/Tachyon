using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Twitterizer.Framework;
using BioNex.Shared.ReportingInterface;

namespace BioNex.Shared.TwitterReportingPlugin
{
    public class TwitterPlugin : BioNex.Shared.ReportingInterface.ReportingInterface
    {
        bool _enable_messages = true;
        bool _enable_errors = true;
        bool _logged_in = false;
        Twitter _twitter = null;

        public string Name
        {
            get
            {
                return "Twitter Notification Plugin";
            }
        }

        /// <summary>
        /// Posts tweets of arbitrary length (can handler > 140 characters)
        /// </summary>
        /// <remarks>
        /// Currently, the log methods block, and since Twitter can be a little slow at times,
        /// it is recommended to improve this code by caching tweets and relying on a thread
        /// to send them over the network.
        /// </remarks>
        /// <param name="properties"></param>
        public void LogMessage( object properties)
        {
            if( !_enable_messages || !_logged_in)
                return;
            StringBuilder update = new StringBuilder();
            // introspect the properties to figure out what to display
            Dictionary<string,string> props = BioNex.Shared.Utils.Reflection.GetPropertiesAndValues( properties);
            foreach( KeyValuePair<string,string> prop in props)
                update.Append( prop.Key + ": " + prop.Value + "; ");
            // now we have to make sure we don't go above the 140 characters
            string tweet = update.ToString();
            // create a list of tweets
            List<string> tweets = new List<string>();
            do {
                string temp;
                if( tweet.Length > 140) {
                    temp = tweet.Substring( 0, 140);
                    tweet = tweet.Remove( 0, 140);
                } else {
                    temp = tweet;
                    tweet = tweet.Remove( 0);
                }
                tweets.Add( temp);
            } while( tweet.Length > 0);
            // now tweet in the reverse order so that when you read it on a computer / phone, it flows better :)
            for( int i=tweets.Count - 1; i>=0; i--)
                _twitter.Status.Update( tweets[i]);
        }

        public void LogError( object properties)
        {
            if( !_enable_errors || !_logged_in)
                return;
        }

        public void EnableMessages( bool enable)
        {
            _enable_messages = enable;
        }

        public void EnableErrors( bool enable)
        {
            _enable_errors = enable;
        }

        /// <summary>
        /// Logs into Twitter account with available credentials.  If no credentials
        /// available (saved somehow and set up via plugin dialog), then don't error,
        /// but obviously don't allow logging to execute, either.
        /// </summary>
        public void Open( System.Windows.Controls.Panel parent_element)
        {
            try {
                _twitter = new Twitter( "bionex", "JqNjEQ2iCulvjlV7SkYl");
            } catch( Exception) {
                return;    
            }
            _logged_in = true;
        }

        public void Close()
        {
            // Twitterizer doesn't have any way to logout of the system???
        }

        public void ShowSetup()
        {
            
        }
    }
}
