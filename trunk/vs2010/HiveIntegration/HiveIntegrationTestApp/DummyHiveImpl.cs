using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HiveIntegrationTestApp
{
    public class DummyHiveImpl : BioNex.HiveIntegration.HiveRpcInterface
    {
        public bool BarcodePresent { get; set; }
        public bool SimulateApiError { get; set; }

        private void HandleApiError()
        {
            if( SimulateApiError)
                throw new Exception( "API simulated an error");
        }

        public override void Ping()
        {
            HandleApiError();
        }

        public override void Initialize(string xml_parameters)
        {
            HandleApiError();
        }

        public override void Close()
        {
            HandleApiError();
        }

        public override void UnloadPlate(string expected_barcode, string labware_name)
        {
            HandleApiError();
        }

        public override void LoadPlate(string expected_barcode, string labware_name)
        {
            HandleApiError();
        }

        public override bool HasBarcode(string barcode)
        {
            HandleApiError();
            return BarcodePresent;
        }

        public override string GetInventory()
        {
            HandleApiError();
            return "";
        }

        public override void ScanInventory()
        {
            HandleApiError();
        }

        public override void MovePlate(string barcode, string labware_name, string destination_group)
        {
            HandleApiError();
        }

        // DKM 2012-05-14 removed to keep API stateless
        /*
        public override bool Abort()
        {
            HandlApiError();
            return true;
        }

        public override bool Retry()
        {
            HandlApiError();
            return true;
        }

        public override bool Ignore()
        {
            HandlApiError();
            return true;
        }
         */

        public override int GetStatus()
        {
            return 0;
        }

        public override void PresentStage()
        {
            HandleApiError();
        }
    }
}
