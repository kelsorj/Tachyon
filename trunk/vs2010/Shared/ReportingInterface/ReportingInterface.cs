using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using System.Diagnostics;

namespace BioNex.Shared.ReportingInterface
{
    public interface ReportingInterface
    {
        string Name { get; }
        void LogMessage( object properties);
        void LogError( object properties);
        /// <summary>
        /// Enables runtime disabling of message reporting
        /// </summary>
        /// <remarks>
        /// A possible reason for this is if there's an imaginary Twitter reporting plugin.
        /// Maybe the user doesn't want to get Tweets about all messages, and only wants to
        /// get error information.
        /// </remarks>
        /// <param name="enable"></param>
        void EnableMessages( bool enable);
        /// <summary>
        /// Enables runtime disabling of error reporting
        /// </summary>
        /// <param name="enable"></param>
        void EnableErrors( bool enable);
        void Open( System.Windows.Controls.Panel parent_element);
        void Close();
        void ShowSetup();
    }

    /// <summary>
    /// this is the main reporting interface that should be used by an application.
    /// It allows the user to register report plugins, and it handles the message
    /// and error subscriptions.
    /// </summary>
    /// <remarks>
    /// Reporter used to inherit from ReportingInterface, but this is unnecessary, plus
    /// it complicates the process of dynamically loading plugins (because it would
    /// then also look like a plugin, which it's not).
    /// </remarks>
    public class Reporter
    {
        List<ReportingInterface> _children = new List<ReportingInterface>();

        public void LogMessage( object properties)
        {
            foreach( ReportingInterface ri in _children)
                ri.LogMessage( properties);
        }

        public void LogError( object properties)
        {
            foreach( ReportingInterface ri in _children)
                ri.LogError( properties);
        }

        public void EnableMessages( bool enable)
        {
            // you probably don't want to ever do this on the main reporter, only children!
        }

        public void EnableErrors( bool enable)
        {
            // you probably don't want to ever do this on the main reporter, only children!
        }

        public void Subscribe( ReportingInterface ri)
        {
            _children.Add( ri);
        }

        public void Unsubscribe( ReportingInterface ri)
        {
            _children.Remove( ri);
        }

        public void Open( System.Windows.Controls.Panel parent_element)
        {
            foreach( ReportingInterface ri in _children)
                ri.Open( parent_element);
        }

        public void Close()
        {
            foreach( ReportingInterface ri in _children)
                ri.Close();
            _children.Clear();
        }

        public void ShowSetup() {}

        public void LoadReportingPlugins( string plugin_path)
        {
            List<ReportingInterface> plugins = new List<ReportingInterface>();
            Plugins.LoadPlugins<ReportingInterface>( plugin_path, plugins);
            foreach( object o in plugins) {
                //! \todo should be able to get rid of this verification step later because
                //!       LoadPlugins should deal with it.
                ReportingInterface ri = o as ReportingInterface;
                if( ri == null) {
                    Debug.Assert( false, "Plugin does not implement the ReportingInterface.  You can ignore this message.");
                    continue;
                }
                Subscribe( ri);
            }
        }
    }
}
