using CookComputing.XmlRpc;

namespace xmlrpc_scratch
{
    public class StateNameService1 : ListenerService
    {
        [XmlRpcMethod("examples.getStateName1")]
        public string GetStateName(int stateNumber)
        {
            if (stateNumber < 1 || stateNumber > m_stateNames.Length)
                throw new XmlRpcFaultException(1, "Invalid state number");
            return m_stateNames[stateNumber - 1];
        }

        string[] m_stateNames
          = { "Alabama", "Alaska", "Arizona", "Arkansas",
        "California", "Colorado", "Connecticut", "Delaware", "Florida",
        "Georgia", "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa", 
        "Kansas", "Kentucky", "Lousiana", "Maine", "Maryland", "Massachusetts"
        };
    }
    public class StateNameService2 : ListenerService
    {
        [XmlRpcMethod("examples.getStateName2")]
        public string GetStateName(int stateNumber)
        {
            if (stateNumber < 1 || stateNumber > m_stateNames.Length)
                throw new XmlRpcFaultException(1, "Invalid state number");
            return m_stateNames[stateNumber - 1];
        }

        string[] m_stateNames
          = { "Michigan", "Minnesota", "Mississipi", "Missouri", "Montana",
        "Nebraska", "Nevada", "New Hampshire", "New Jersey", "New Mexico", 
        "New York", "North Carolina", "North Dakota", "Ohio", "Oklahoma",
        "Oregon", "Pennsylviania", "Rhose Island", "South Carolina", 
        "South Dakota", "Tennessee", "Texas", "Utah", "Vermont", "Virginia", 
        "Washington", "West Virginia", "Wisconsin", "Wyoming" };
    }
}
