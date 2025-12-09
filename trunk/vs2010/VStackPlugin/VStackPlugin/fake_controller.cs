using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VStackPlugin
{
    public interface IWorksController : IWorksDriverLib.CWorksController
    {
    }

    class fake_controller : IWorksController
    {
        public void NotifyDataChanged(IWorksDriverLib.CControllerClient Source, string ObjectDataChanged)
        {
        }

        public void NotifyTipOperation(IWorksDriverLib.CControllerClient Source, string TipOperationXML)
        {
        }

        public void OnCloseDiagsDialog(IWorksDriverLib.CControllerClient Source)
        {
        }

        public void PrintToLog(IWorksDriverLib.CControllerClient Source, string StringToPrint)
        {
        }

        public string Query(IWorksDriverLib.CControllerClient Source, string Query)
        {
            throw new NotImplementedException();
        }

        public void Update(IWorksDriverLib.CControllerClient Source, string Update)
        {
        }
    }
}
