using BioNex.Shared.PlateDefs;

namespace BioNex.Shared.DeviceInterfaces
{
    public interface StackerInterface
    {
        /// <summary>
        /// Upstacks a plate.  Plate information is used to tell device what parameters to use
        /// for the process, like plate thickness, stacking height, etc.
        /// </summary>
        /// <param name="plate"></param>
        void Upstack( Plate plate);
        /// <summary>
        /// Downstacks a plate.  Technically, no plate information is needed, but I want it
        /// to support the notion of a composited stacker that can then delegate the
        /// operation to another stacker.
        /// </summary>
        /// <param name="plate"></param>
        void Downstack( Plate plate);
        void LoadStack( Plate plate);
        void ReleaseStack( Plate plate);
    }
}
