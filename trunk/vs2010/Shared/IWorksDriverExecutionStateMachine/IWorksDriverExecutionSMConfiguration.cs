using BioNex.Shared.Utils;

namespace BioNex.Shared.IWorksDriverExecutionStateMachine
{
    public class IWorksDriverExecutionSMConfiguration
    {
        public bool IgnoreErrors { get; set; }
    }

    public class IWorksDriverExecutionSMConfigurationParser
    {
        public IWorksDriverExecutionSMConfiguration _configuration { get; private set; }

        public IWorksDriverExecutionSMConfigurationParser()
        {
            // HiveName = "Synapsis"; // default hive name
            string _configPath = FileSystem.GetAppPath() + "\\config\\IWorksDriverConfig.xml";
            _configuration = FileSystem.LoadXmlConfiguration< IWorksDriverExecutionSMConfiguration>(_configPath);
        }
    }
}
