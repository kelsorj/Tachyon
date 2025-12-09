using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace LabwareCloudService
{
    public partial class LabwareCloudService : ServiceBase
    {
        private LabwareCloudApp _app;

        public LabwareCloudService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            if (_app == null)
                _app = new LabwareCloudApp();
            _app.Start(true);
        }

        protected override void OnStop()
        {
            if( _app != null)
                _app.Stop();
        }
    }
}
