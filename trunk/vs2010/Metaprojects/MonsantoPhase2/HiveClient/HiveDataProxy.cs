using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;

namespace BioNex.HiveRpc
{
    public interface IHiveDataProxy : IHiveData, IXmlRpcProxy
    {
    }
}
