using System.Collections.Generic;
using System.Xml.Serialization;

namespace BioNex.TipBoxManager
{
    public class TipBoxManagerConfiguration
    {
        public string TipBoxDevice { get; set; }
        [ XmlArrayItem( "TipBoxLocation")]
        public List< string> TipBoxLocations { get; set; }
    }
}
