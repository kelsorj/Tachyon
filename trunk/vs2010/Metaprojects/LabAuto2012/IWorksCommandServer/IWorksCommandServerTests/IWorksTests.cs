using System;
using NUnit.Framework;
using BioNex.IWorksCommandClient;


namespace BioNex.IWorksCommandServer
{
    [TestFixture]
    public class IWorksTests
    {
        private class TestStackerImpl : StackerRpcInterface
        {
            public override void Ping() { }
            public override bool IsLocationAvailable(string location_xml) { return false; }
            public override int MakeLocationAvailable(string location_xml) { return 0; }
            public override int PlateDroppedOff(string plate_xml) { return 0; }
            public override int PlatePickedUp(string plate_xml) { return 0; }
            public override void PlateTransferAborted(string plate_info_xml) { }
            public override int PrepareForRun(string location_xml) { return 0; }
            public override int SinkPlate(string labware, int PlateFlags, string SinkToLocation) { return 0; }
            public override int SourcePlate(string labware, int PlateFlags, string SinkToLocation) { return 0; }
        }

        [Test]
        public void TestStackerServer()
        {
            try {
                var server = new RemoteStackerServer(new TestStackerImpl(), 7890);
                RemoteStackerClient client = new RemoteStackerClient();
                client.Connect( "localhost", 7890);
                client.SinkPlate("", 0, "");
                server.Stop();
            } catch( Exception ex) {
                Assert.Fail( ex.Message);
            }
        }
    }
}
