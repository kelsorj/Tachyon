using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO;
using BioNex.Shared.LibraryInterfaces;
using System.ComponentModel.Composition;

namespace BioNex.Shared.ApplicationPreferences
{
    [Export(typeof(IPreferences))]
    public class Preferences : IPreferences
    {
        private string _path;
        private XDocument _xml;
        private XElement _root;

        private void CreatePreferencesFile( string rootnode, string path)
        {
            _xml = new XDocument();
            // originally, I set _application here ---vvv but changed it since I will set it later
            _xml.Add( _root = new XElement( rootnode));
            _xml.Save( path);
        }

        /// <summary>
        /// Loads preferences out of a preferences file.  If it doesn't exist, it will create the file.
        /// </summary>
        /// <remarks>
        /// I think I need to change the schema so we can have one preferences file, and multiple components can write to it
        /// </remarks>
        /// <param name="rootnode">the node to grab</param>
        /// <param name="path">the absolute preferences file path</param>
        [ImportingConstructor]
        public Preferences( [Import("Preferences.rootnode")] string rootnode,
                            [Import("Preferences.path")] string path)
        {
            // load the XML file, or create it if it doesn't exist
            try {
                _xml = XDocument.Load( path);
                // look for the rootnode in the xml document
                if( _xml.Root.Name != rootnode)
                    throw new KeyNotFoundException( String.Format( "The rootnode '{0}' was not found in the preferences file '{1}'", rootnode, _path));
            } catch( System.IO.FileNotFoundException) {
                CreatePreferencesFile( rootnode, path);
            }
            _root = _xml.Element( rootnode);
            _path = path;
            InitializeQueries();
        }

        private void InitializeQueries()
        {
            // I'm thinking about caching the query here for LINQ, but it requires a param and I'm not sure if it's possible
        }

        /// <summary>
        /// overwrites the current preferences file
        /// </summary>
        public void Save()
        {
            Save( _path);
        }

        /// <summary>
        /// saves the current preferences to a file at the specified path
        /// </summary>
        /// <param name="path">absolute path for preferences file</param>
        public void Save( string path)
        {
            _xml.Save( path);
            _path = path;
        }

        /// <summary>
        /// retrieves the value of the specified preference
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetPreferenceValue( string application, string key)
        {
            if( !_root.Descendants( application).Any())
                throw new KeyNotFoundException( String.Format( "The application settings element '{0}' was not found in the preferences file '{1}'", key, _path));

            // loop over the <prefer
            var preferences = from s in _root.Element(application).Elements()
                              where (string)s.Element("name") == key
                              select s.Element("value");
            if( preferences.Count() == 0)
                throw new KeyNotFoundException( String.Format( "The preference '{0}' was not found in the preferences file '{1}'", key, _path));
            var result = preferences.ElementAt(0);
            return result.Value;
        }

        /// <summary>
        /// adds / overwrites a preference with the specified value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetPreferenceValue( string application, string key, string value)
        {
            // check to see if the application element exists.  If not, create it
            if( !_root.Descendants( application).Any())
                _root.Add( new XElement( application));

            // loop over the <preference> elements
            var preferences = from s in _root.Element(application).Elements()
                              where (string)s.Element("name") == key
                              select s.Element("value");
            if( preferences.Count() == 0) {
                _root.Element(application).Add( new XElement( "preference", new XElement( "name", key), new XElement( "value", value)));
            } else {
                preferences.ElementAt( 0).Value = value;
            }
        }

        /// <summary>
        /// allows you to access the preferences like it's a Dictionary<string,string>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string this[string application, string key]
        {
            // use LINQ to get / set the values
            get {
                return GetPreferenceValue( application, key);
            }

            set {
                SetPreferenceValue( application, key, value);
            }
        }

        public int GetNumberOfPreferences( string application)
        {
            return _root.Element( application).Elements().Count();
        }
    }

    [TestFixture]
    public class TestPreferences
    {
        private readonly string BumblebeeKey = "Bumblebee";

        [Test]
        public void TestGetPreferenceFromFile()
        {
            // create a test XML document
            XDocument xml = XDocument.Parse( 
                @"<Preferences>
                    <Bumblebee>
                      <preference>
                        <name>Configuration files</name>
                        <value>c:\test</value>
                      </preference>
                    </Bumblebee>
                </Preferences>");

            xml.Save( "testprefs.xml");
            Preferences prefs = new Preferences( "Preferences", "testprefs.xml");
            Assert.AreEqual( "c:\\test", prefs[BumblebeeKey, "Configuration files"]);
        }

        [Test]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void TestGetPreferenceFromObject()
        {
            Preferences prefs = new Preferences( "Preferences", "test_preferences.xml");
            string value;
            // note that the following assignment of value used to look like this:
            // Assert.Throws<KeyNotFoundException>( () => value = prefs[BumblebeeKey, "doesn't exist"] );
            // but NUnit + anonymous methods make VS2008 raise an unhandled-exception error
            // you therefore need to use ExpectedException(typeof(KeyNotFoundException)), and then
            // remove the Assert.Throws<> part.
            value = prefs[BumblebeeKey, "doesn't exist"];
            prefs[BumblebeeKey, "new pref"] = "howdy";
            Assert.AreEqual( "howdy", prefs[BumblebeeKey, "new pref"]);
            // now save and reload prefs
            prefs.Save( "testprefs.xml");
            Preferences reloaded = new Preferences( "Preferences", "testprefs.xml");
            Assert.AreEqual( "howdy", reloaded[BumblebeeKey, "new pref"]);
        }

        [Test]
        public void TestAddPreference()
        {
            Preferences prefs = new Preferences( "Preferences", "test_preferences.xml");
            Assert.DoesNotThrow( () => prefs[BumblebeeKey, "test key"] = "test value");
            Assert.AreEqual( "test value", prefs[BumblebeeKey, "test key"]);
        }

        [Test]
        public void TestOverwritePreference()
        {
            Preferences prefs = new Preferences( "Preferences", "test_preferences.xml");
            prefs[BumblebeeKey, "test"] = "hi there";
            prefs[BumblebeeKey, "test"] = "overwritten!";
            Assert.AreEqual( "overwritten!", prefs[BumblebeeKey, "test"]);
            Assert.AreEqual( prefs.GetNumberOfPreferences( BumblebeeKey), 1);
        }

        [Test]
        public void TestAgainstCreatedFile()
        {
            File.Delete( "new_prefs.xml");
            Preferences prefs = new Preferences( "ApplicationPreferences", "new_prefs.xml");
            prefs[BumblebeeKey, "new pref"] = "howdy";
            Assert.AreEqual( "howdy", prefs[BumblebeeKey, "new pref"]);
            // now save and reload prefs
            prefs.Save();
            Preferences reloaded = new Preferences( "ApplicationPreferences", "new_prefs.xml");
            Assert.AreEqual( "howdy", reloaded[BumblebeeKey, "new pref"]);
        }

        [Test, Ignore]
        public void TestKeyNotFoundExceptionInNUnit()
        {
            Dictionary<string,string> test = new Dictionary<string,string>();
            string value;
            Assert.Throws<KeyNotFoundException>( () => value = test["asdg"]);
        }
    }
}
