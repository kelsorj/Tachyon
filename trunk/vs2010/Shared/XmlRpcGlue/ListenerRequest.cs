using System.IO;
using System.Net;

// ListenerRequest is for XmlRpc using HttpListener,
//  something we're generally not doing for now since it requires elevated privileges to reserve the listening address.
//  instead, we're using the HttpChannel / MarshalByRef interface found in XmlRpcServer.cs
//  This file is retained for reference in case we ever decide we need to go this route instead

namespace BioNex.Shared.XmlRpcGlue
{
    public class ListenerRequest : CookComputing.XmlRpc.IHttpRequest
    {
        public ListenerRequest(HttpListenerRequest request)
        {
            this.request = request;
        }

        public Stream InputStream
        {
            get { return request.InputStream; }
        }

        public string HttpMethod
        {
            get { return request.HttpMethod; }
        }

        private readonly HttpListenerRequest request;
    }
}
