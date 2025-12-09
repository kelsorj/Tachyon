using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;

namespace BioNex.Shared.PluginManager
{
    public class PluginLoader
    {
        /// <summary>
        /// Allows Synapsis and firmware updater application to use common plugin loading code.  Rather than displaying messages,
        /// the new PluginLoader throws an AggregateException that should be handled by the application
        /// </summary>
        /// <param name="composition_requester"></param>
        /// <param name="application_assembly"></param>
        /// <param name="directory_catalog_paths"></param>
        /// <param name="log"></param>
        /// <exception cref="AggregateException" />
        /// <returns></returns>
        static public void LoadPlugins( Object composition_requester, Type application_assembly, out CompositionContainer container,
                                        IList<string> directory_catalog_paths, string plugin_relative_path, ILog log)
        {
            // MEF
            List<Exception> exceptions = new List<Exception>();
            container = null;
            try {
                string exe_path = BioNex.Shared.Utils.FileSystem.GetAppPath();
                AggregateCatalog catalog = new AggregateCatalog();
                try {
                    foreach( var path in directory_catalog_paths)
                        catalog.Catalogs.Add( new DirectoryCatalog( path));
                } catch( Exception e) {
                    log.Info( e.Message);
                    exceptions.Add( e);
                }
                try {
                    catalog.Catalogs.Add( new DirectoryCatalog( exe_path + "\\" + plugin_relative_path));
                } catch( System.IO.DirectoryNotFoundException e) {
                    System.IO.Directory.CreateDirectory( exe_path + "\\" + plugin_relative_path);
                    string message = String.Format( "The plugins folder was not present in {0}, and has been automatically created for you.  Although plugins are not necessary to run Synapsis, you should create this folder and populate it with device plugins if you want to be able to simulate protocols.", exe_path);
                    log.Info( message);
                    exceptions.Add( e);
                }
                // need to also add this assembly to the catalog, or we won't be able to import the ViewModel
                catalog.Catalogs.Add( new AssemblyCatalog( application_assembly.Assembly));
                container = new CompositionContainer( catalog);
                try {
                    container.ComposeParts( composition_requester);
                } catch( CompositionException ex) {
                    foreach( CompositionError e in ex.Errors) {
                        string description = e.Description;
                        string details = e.Exception.Message;
                        log.Error( description + ": " + details);
                    }
                    exceptions.Add( ex);
                    // DKM 2012-02-15 don't throw, let's see if we can get Synapsis, etc. to load more plugins and return error later
                    //throw;            
                } catch( System.Reflection.ReflectionTypeLoadException ex) {
                    foreach( Exception e in ex.LoaderExceptions) {
                        log.Error( e.Message, e);
                        exceptions.Add( e);
                    }
                } catch( Exception ex) {
                    log.Error( ex.Message, ex);
                    exceptions.Add( ex);
                }

                if( exceptions.Count() != 0)
                    throw new AggregateException( exceptions);
            } catch( System.IO.DirectoryNotFoundException) {
                // couldn't find a plugins folder, so nothing else to do in this method
            } catch( AggregateException) {
            } catch( Exception ex) {
                log.Error( ex.Message);
                exceptions.Add( ex);
            }

            if( exceptions.Count() != 0)
                throw new AggregateException( exceptions);
        }
    }
}
