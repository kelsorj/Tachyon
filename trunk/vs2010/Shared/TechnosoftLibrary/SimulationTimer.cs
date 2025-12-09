using System.Threading;

namespace BioNex.Shared.TechnosoftLibrary
{
    interface ISimulationTimer
    {
        void Sleep( int time_ms);
    }

    public class RealTimeSimulationTimer : ISimulationTimer
    {
        public void Sleep( int time_ms)
        {
            Thread.Sleep( time_ms);
        }
    }

    public class FastSimulationTimer : ISimulationTimer
    {
        private int Multiplier { get; set; }

        /// <summary>
        /// Allows you to set the number of tight loops to spin for each "ms" of sleep time
        /// </summary>
        /// <param name="multiplier"></param>
        public FastSimulationTimer( int multiplier)
        {
            Multiplier = multiplier;
        }

        public void Sleep( int time_ms)
        {
            for( int i=0; i<time_ms; i++) {
                for( int j=0; j<Multiplier; j++);
            }
        }
    }
}
