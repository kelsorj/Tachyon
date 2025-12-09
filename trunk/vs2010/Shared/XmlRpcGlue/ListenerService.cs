using System;
using System.Net;
using CookComputing.XmlRpc;

// ListenerService is for XmlRpc using HttpListener,
//  something we're generally not doing for now since it requires elevated privileges to reserve the listening address.
//  instead, we're using the HttpChannel / MarshalByRef interface found in XmlRpcServer.cs
//  This file is retained for reference in case we ever decide we need to go this route instead

namespace BioNex.Shared.XmlRpcGlue
{
    public abstract class ListenerService : XmlRpcHttpServerProtocol
    {
        public virtual void ProcessRequest(HttpListenerContext RequestContext)
        {
            try
            {
                IHttpRequest req = new ListenerRequest(RequestContext.Request);
                IHttpResponse resp = new ListenerResponse(RequestContext.Response);
                HandleHttpRequest(req, resp);
                RequestContext.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                // "Internal server error"
                RequestContext.Response.StatusCode = 500;
                RequestContext.Response.StatusDescription = ex.Message;
            }
        }
    }

}
