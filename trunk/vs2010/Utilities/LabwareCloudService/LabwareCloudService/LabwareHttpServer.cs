using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using BioNex.Shared.LabwareDatabase;
using System.IO;

namespace LabwareCloudService
{
    public class LabwareHttpServer : SimpleHttpServer
    {
        bool _isService;
        LabwareDatabase _labware_db;

        public LabwareHttpServer(LabwareDatabase labwareDb, bool isService, int port)
            : base (string.Format("http://+:{0}/labware/", port))
        {
            _isService = isService;
            _labware_db = labwareDb;
            ServerHit = OnServerHit;
        }

        void OnServerHit(Stream outputStream)
        {
            var path = Request.Url;
            var nav_path = string.Format(@"http://{0}/labware/index.html", path.Authority);
            if (path.Segments.Length < 3 || path.Segments[2] != "index.html")
            {               
                Response.Redirect(nav_path);
            }

            var source =  GeneratePage(nav_path, path.Query);

            var bytes = Encoding.UTF8.GetBytes(source);
            var out_stream = new MemoryStream(bytes);
                    
            out_stream.CopyTo(outputStream);
            out_stream.Close();
        }                        

        string GeneratePage(string nav_path, string query)
        {
            var file = string.Format(@"{0}labware_service.html", AppDomain.CurrentDomain.BaseDirectory);
            var in_stream = new StreamReader(file);
            var source = in_stream.ReadToEnd();
            in_stream.Close();

            source = source.Replace("#SERVICE#", _isService ? "(service)" : "(console)");
            source = source.Replace("#NAV_PATH#", nav_path);

            var labware_entries = GetLabwareEntries(nav_path, query);
            source = source.Replace("#LABWARE_ENTRIES#", labware_entries);

            var labware_properties = GetLabwareProperties(nav_path, query);
            source = source.Replace("#LABWARE_PROPERTIES#", labware_properties);

            var labware_value = GetLabwareValue(nav_path, query);
            source = source.Replace("#LABWARE_VALUE#", labware_value);

            return source;
        }


        static long CountLinesInString(string s)
        {
            long count = 1;
            int start = 0;
            while ((start = s.IndexOf('\n', start)) != -1)
            {
                count++;
                start++;
            }
            return count;
        }


        string GetLabwareEntries(string nav_path, string query)
        {
            var labware = string.Join("", Uri.UnescapeDataString(query).TrimStart('?').TakeWhile((c) => { return c != '+'; }));
            var values = new List<string>();
            try
            {
                values = _labware_db.GetLabwareNames();
            }
            catch (Exception e)
            {
                values.Clear();
                values.Add("Problem with labware database: " + e);
            }

            string value = "<table width='100%' border='0'>";
            foreach (var s in values)
            {
                value += (s == labware)
                    ? "<tr bgcolor='#dcfa00' "
                    : "<tr onmouseover='ChangeColor(this, true);' onmouseout='ChangeColor(this, false);' ";

                value +=
                    "onclick='DoNav(\"" + nav_path + "?" + s + "\");'><td>" + s + "</td></tr>\n";
            }
            value += "</table>";
            return value;
        }

        string GetLabwareProperties(string nav_path, string query)
        {
            query = Uri.UnescapeDataString(query).TrimStart('?');
            var labware = string.Join("", query.TakeWhile((c) => { return c != '+'; }));
            var property = query.Remove(0, labware.Length).TrimStart('+');
            var values = new List<string>();
            try
            {
                var props = _labware_db.GetLabwareProperties();
                foreach (var prop in props)
                    values.Add(prop.Name);
                if (values.Count == 0)
                    values.Add("empty");
            }
            catch (Exception e)
            {
                values.Clear();
                values.Add("Problem with labware database: " + e);
            }

            string value = "<table width='100%' border='0'>";
            foreach (var s in values)
            {
                value += string.IsNullOrEmpty(labware)
                    ? "<tr>"
                    : s == property
                        ? "<tr bgcolor='#dcfa00'>"
                        : "<tr onmouseover='ChangeColor(this, true);' onmouseout='ChangeColor(this, false);'onclick='DoNav(\"" + nav_path + "?" + labware + "+" + s + "\");'>";
                value += "<td>" + s + "</td></tr>\n";
            }
            value += "</table>";
            return value;
        }

        string GetLabwareValue(string nav_path, string query)
        {
            query = Uri.UnescapeDataString(query).TrimStart('?');
            var labware = string.Join("", query.TakeWhile((c) => { return c != '+'; }));
            var property = query.Remove(0, labware.Length).TrimStart('+');

            var value = "";
            if (string.IsNullOrEmpty(labware) || string.IsNullOrEmpty(property))
                return "make a selection";
            try
            {
                var lb = _labware_db[labware];
                var prop = lb[property];
                value = prop != null ? prop.ToString() : "null";
            }
            catch (Exception e)
            {
                value = "Problem with labware database: " + e;
            }

            return value;
        }
    }
}
