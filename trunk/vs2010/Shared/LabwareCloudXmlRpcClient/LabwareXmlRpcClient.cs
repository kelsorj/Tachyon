using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BioNex.Shared.LabwareCloudXmlRpcServer;
using BioNex.Shared.LabwareDatabase;
using BioNex.Shared.LibraryInterfaces;
using CookComputing.XmlRpc;


namespace BioNex.Shared.LabwareCloudXmlRpcClient
{
    public interface LabwareCloudXmlRpcServiceProxy : ILabwareCloudXmlRpcService, IXmlRpcProxy
    { }

    public class LabwareXmlRpcClient 
    {
        private readonly LabwareCloudXmlRpcServiceProxy _remote_server;
        private readonly ILabwareDatabase _labware_db;

        private const int DEFAULT_TIMEOUT = 5000; // 5 seconds for most operations
        private const int PUBLISH_TIMEOUT = 30000; // 30 seconds for publish, it can take a long time to run through a publish

        public string url { get { return _remote_server.Url; } }

        public LabwareXmlRpcClient(ILabwareDatabase labware_db, string host, int port)
        {
            _labware_db = labware_db;
            _remote_server = (LabwareCloudXmlRpcServiceProxy)XmlRpcProxyGen.Create(typeof(LabwareCloudXmlRpcServiceProxy));
            _remote_server.Url = String.Format("http://{0}:{1}/labware", host, port);
            _remote_server.Timeout = DEFAULT_TIMEOUT;
        }

        public void DoSync()
        {
            try
            {
                _remote_server.Timeout = DEFAULT_TIMEOUT;

                // get the remote labware
                DateTime _last_sync = _labware_db.GetLastSyncTime();
                var xlabware = _remote_server.Sync(_last_sync);
                if (xlabware.Count() == 0)
                {
                    _labware_db.SetLastSyncTime(DateTime.Now);
                    MessageBox.Show("Sync successful, no changes since last sync.");
                    return;
                }

                IEnumerable<string> remote_labware_names;
                Dictionary<string, ILabware> remote_dictionary;
                IEnumerable<string> local_labware_names;
                Dictionary<string, ILabware> local_dictionary;
                var conflicted_names = GetConflictedNames(xlabware, out remote_labware_names, out remote_dictionary, out local_labware_names, out local_dictionary);

                // for each conflict, prompt to keep or discard
                var resolved_labware = new List<ILabware>();
                foreach (var name in conflicted_names)
                {
                    var conflicts = GetConflicts(local_dictionary[name], remote_dictionary[name]);
                    var dlg = new ResolveLabwareConflict(name, conflicts);
                    var accept_remote = dlg.ShowDialog() ?? true;
                    // make sure to use the local id since we didn't pass a database id across xml-rpc
                    ((Labware)remote_dictionary[name]).Id = local_dictionary[name].Id;
                    resolved_labware.Add(accept_remote ? remote_dictionary[name] : local_dictionary[name]);
                }
            
                // for the remaining unique names, pull from local or remote as necessary
                var remote_unique_names = from x in remote_labware_names where !local_labware_names.Contains(x) select x;
                foreach (var name in remote_unique_names)
                    resolved_labware.Add(remote_dictionary[name]);
            
                // don't need to re-add local uniques, since they're already in local db by definition
                //var local_unique_names = from x in local_labware_names where !remote_labware_names.Contains(x) select x;
                //foreach (var name in local_unique_names)
                //    resolved_labware.Add(local_dictionary[name]);

                // update the local database with accepted differences
                foreach (var labware in resolved_labware)
                    _labware_db.UpdateLabware(labware);

                // make sure we're using the updated version everywhere
                _labware_db.ReloadLabware();

                _labware_db.SetLastSyncTime(DateTime.Now);
                MessageBox.Show("Sync successful!", "Sync Operation");
            }
            catch(Exception e)
            {
                MessageBox.Show(string.Format("Error during Sync:\n\n{0}",e.Message), "Sync Operation");
            }
        }

        public void DoPublish()
        {
            try
            {
                _remote_server.Timeout = DEFAULT_TIMEOUT;

                // step 1. initiate transaction by requesting a guid
                var guid = BeginPublish();
                if( guid == "") return;

                // step 2. Call sync, if master returns anything, abort publish operation and notify user that they need to sync
                // the master will return an empty set if there have been no changes
                DateTime _last_sync = _labware_db.GetLastSyncTime();
                var xlabware = _remote_server.Sync(_last_sync);
                if( xlabware.Count() > 0)
                {
                    var err = EndPublish(guid);
                    err = err == "" ? "" : string.Format("\n\nAn error was encountered while closing the transaction:\n\n{0}", err);
                    MessageBox.Show(string.Format("Cloud reports changes since last sync.\nPlease Sync before publishing.{0}", err), "Publish Operation");
                    return;
                }

                // step 3. get local labware db 
                // TODO -- when we have timestamped rows, client can record a lastPublishTime and submit a diff based on that time.
                //      -- for now, we will rely on a server side diff
                _labware_db.ReloadLabware();
                var local_labware_names = _labware_db.GetLabwareNames();
                var lxlabware = new XmlRpcLabware[local_labware_names.Count];
                for (int i = 0; i < local_labware_names.Count; ++i)
                    lxlabware[i] = new XmlRpcLabware(_labware_db.GetLabware(local_labware_names[i]));

                // step 4.  publish to server
                _remote_server.Timeout = PUBLISH_TIMEOUT; // bump up the timeout value here since this is going to be a longish server-side op
                var publish_err = Publish(guid, lxlabware);
                if ( publish_err != "")
                {
                    MessageBox.Show(string.Format("An error was encountered while publishing changes:\n\n{0}", publish_err), "Publish Operation");
                    return;
                }

                // step 5. Mark the db's as synchronized by recording last sync time
                _labware_db.SetLastSyncTime(DateTime.Now);

                // step 6. End transaction
                _remote_server.Timeout = DEFAULT_TIMEOUT;
                var end_publish_err = EndPublish(guid);
                end_publish_err = end_publish_err == "" ? "" : string.Format("\n\nAn error was encountered while closing the transaction:\n\n{0}", end_publish_err);
                MessageBox.Show(string.Format("Publish successful!{0}", end_publish_err), "Publish Operation");
            }
            catch(Exception e)
            {
                MessageBox.Show(string.Format("Error during Publish:\n\n{0}",e.Message), "Publish Operation");
            }

        }

        private string BeginPublish()
        {
            try
            {
                return _remote_server.BeginPublish();
            }
            catch (LabwareXmlRpcServiceException e)
            {
                MessageBox.Show(e.Message, "Publish Operation");
                return "";
            }
        }

        private string EndPublish(string guid)
        {
            try
            {
                _remote_server.EndPublish(guid);
                return "";
            }
            catch (LabwareXmlRpcServiceException e)
            {
                return e.Message;
            }
        }

        private string Publish(string guid, XmlRpcLabware[] xlabware)
        {
            try
            {
                _remote_server.Publish(guid, xlabware);
                return "";
            }
            catch(LabwareXmlRpcServiceException e)
            {
                return e.Message;
            }
        }

        private IEnumerable<string> GetConflictedNames(
              XmlRpcLabware[] xlabware
            , out IEnumerable<string> remote_names, out Dictionary<string, ILabware> remote_dict
            , out IEnumerable<string> local_names, out Dictionary<string, ILabware> local_dict
            )
        {
            // convert xmlrpc labware into labware so we can compare apples to apples
            var remote_labware = from x in xlabware select (Labware)x;
            var remote_labware_names = from x in remote_labware select x.Name;
            var remote_dictionary = remote_labware_names.ToDictionary(x => x, x => (from e in remote_labware where e.Name == x select (ILabware)e).FirstOrDefault());

            // get local labware 
            _labware_db.ReloadLabware();
            var local_labware_names = _labware_db.GetLabwareNames();
            var local_labware = from x in local_labware_names select _labware_db.GetLabware(x);
            var local_dictionary = local_labware_names.ToDictionary(x => x, x => (from e in local_labware where e.Name == x select e).FirstOrDefault());

            // detect conflicts
            var shared_names = from x in remote_labware_names where local_labware_names.Contains(x) select x;
            var conflicted_names = from x in shared_names where Conflicted(local_dictionary[x], remote_dictionary[x]) select x;

            remote_names = remote_labware_names;
            remote_dict = remote_dictionary;
            local_names = local_labware_names;
            local_dict = local_dictionary;

            return conflicted_names;
        }
        
        private List<ResolveLabwareConflict.Conflict> GetConflicts(ILabware local, ILabware remote)
        {
            var conflicts = new List<ResolveLabwareConflict.Conflict>();
            if (local.Name != remote.Name)
                conflicts.Add(new ResolveLabwareConflict.Conflict() { Name = "Name", LocalValue = local.Name ?? "", RemoteValue = remote.Name ?? "" });
            if (local.Notes != remote.Notes)
                conflicts.Add(new ResolveLabwareConflict.Conflict() { Name = "Notes", LocalValue = local.Notes ?? "", RemoteValue = remote.Notes ?? ""});
            if (local.Tags != remote.Tags)
                conflicts.Add(new ResolveLabwareConflict.Conflict() { Name = "Tags", LocalValue = local.Tags ?? "", RemoteValue = remote.Tags ?? "" });

            foreach (var key in local.Properties.Keys)
            {
                var local_prop = local.Properties[key];
                var remote_prop = remote.Properties.Keys.Contains(key) ? remote.Properties[key] : null;

                if (local_prop == null && remote_prop == null)
                    continue;

                // awesome.  a case where == gives the wrong value but .equals works
                if (local_prop.Equals(remote_prop))
                    continue;

                var local_string = local_prop == null ? "" : local_prop.ToString();
                var remote_string = remote_prop == null ? "" : remote_prop.ToString();
                conflicts.Add(new ResolveLabwareConflict.Conflict() { Name = key, LocalValue = local_string, RemoteValue = remote_string });
            }
            return conflicts;
        }

        private bool Conflicted(ILabware local, ILabware remote)
        {
            if (local.Name != remote.Name)
                return false;
            
            if (local.Notes != remote.Notes)
            {
                // notes and tags are sometimes ambiguously null or empty string.  We want to treat null equivalently to empty string for now.
                if (!((local.Notes == null && remote.Notes == "")
                ||    (local.Notes == "" && remote.Notes == null)))
                    return true;
            }
            if (local.Tags != remote.Tags)
            {
                // notes and tags are sometimes ambiguously null or empty string.  We want to treat null equivalently to empty string for now.
                if (!((local.Tags == null && remote.Tags == "")
                ||    (local.Tags == "" && remote.Tags == null)))
                    return true;
            }
            
            if (local.Properties.Count != remote.Properties.Count)
                return true;

            foreach (var key in local.Properties.Keys)
            {
                if (!remote.Properties.Keys.Contains(key))
                    return true;
                
                var local_prop = local.Properties[key];
                if (local_prop == null)
                    return true;

                var remote_prop = remote.Properties[key];

                // awesome.  a case where == gives the wrong value but .equals works
                if ( !local_prop.Equals(remote_prop))
                    return true;
            }           

            return false;
        }
    }
}
