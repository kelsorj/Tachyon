using System.Net;
using System.IO;
using CookComputing.XmlRpc;

namespace xmlrpc_scratch
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

        private HttpListenerRequest request;
    }

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

        private HttpListenerResponse response;
    }
}
