using BioNex.Shared.Teachpoints;

namespace BioNex.PlateMover
{
    public static class PlateMoverTeachpointNames
    {
        public const string HiveLandscapeTeachpoint = "Hive (landscape)";
        public const string HivePortraitTeachpoint = "Hive (portrait)";
        public const string ExternalTeachpoint = "External";
    }

    public class PlateMoverTeachpoint : GenericTeachpoint
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public double Y { get; set; }
        public double R { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public PlateMoverTeachpoint()
        {
        }
        // ----------------------------------------------------------------------
        public PlateMoverTeachpoint( string name, double y, double r)
            : base( name)
        {
            Y = y;
            R = r;
        }
    }
}
