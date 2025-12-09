using System.IO;
using System.Net;
using CookComputing.XmlRpc;

// ListenerResoibse is for XmlRpc using HttpListener,
//  something we're generally not doing for now since it requires elevated privileges to reserve the listening address.
//  instead, we're using the HttpChannel / MarshalByRef interface found in XmlRpcServer.cs
//  This file is retained for reference in case we ever decide we need to go this route instead

namespace BioNex.Shared.XmlRpcGlue
{
    public class ListenerResponse : CookComputing.XmlRpc.IHttpResponse
    {
        public ListenerResponse(HttpListenerResponse response)
        {
            this.response = response;
        }

        long IHttpResponse.ContentLength
        {
            set
            {
                response.ContentLength64 = value;
            }
        }

        string IHttpResponse.ContentType
        {
            get { return response.ContentType; }
            set { response.ContentType = value; }
        }

        TextWriter IHttpResponse.Output
        {
            get { return new StreamWriter(response.OutputStream); }
        }

        Stream IHttpResponse.OutputStream
        {
            get { return response.OutputStream; }
        }

        bool IHttpResponse.SendChunked
        {
            get
            {
                return response.SendChunked;
            }
            set
            {
                response.SendChunked = value;
            }
        }

        int IHttpResponse.StatusCode
        {
            get { return response.StatusCode; }
            set { response.StatusCode = value; }
        }

        string IHttpResponse.StatusDescription
        {
            get { return response.StatusDescription; }
            set { response.StatusDescription = value; }
        }

        private readonly HttpListenerResponse response;
    }
}
