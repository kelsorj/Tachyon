using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.LibraryInterfaces
{
    public class Constants
    {
        public static readonly string Strobe = "!STROBE!";
        public static bool IsStrobe(string test){ return test.ToUpper() == Strobe; }
        public static readonly string Empty = "EMPTY";
        public static bool IsEmpty(string test) { return test.ToUpper() == Empty || test == "##"; }
        public static readonly string NoRead = "NOREAD";
        public static bool IsNoRead(string test) { return test.ToUpper() == NoRead; }
    }
}
