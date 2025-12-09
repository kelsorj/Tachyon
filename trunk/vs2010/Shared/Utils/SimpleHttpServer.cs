using System;
using System.IO;
using System.Net;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public class SimpleHttpServer
    {
        readonly HttpListener _listener;
        ServerHitDelegate _serverHit;
        readonly bool _deleteReservation;
        readonly string _uri;

        public delegate void ServerHitDelegate( Stream outStream);
        public ServerHitDelegate ServerHit
        {
            get { return _serverHit; }
            set { _serverHit = value; }
        }

        public HttpListenerRequest Request { get; private set; }
        public HttpListenerResponse Response { get; private set; }
        public HttpListenerContext Context { get; private set; }

        // This example requires the System and System.Net namespaces.
        public SimpleHttpServer(string uri, ServerHitDelegate serverHit=null, bool deleteReservation=false)
        {
            _uri = uri;
            _serverHit = serverHit;
            _deleteReservation = deleteReservation;

            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }

            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            if (string.IsNullOrEmpty(uri))
                throw new ArgumentException("bad uri");


            bool need_privilege = false;
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(uri);
                _listener.Start();
            }
            catch (HttpListenerException e)
            {
                if (e.ErrorCode == 5) // Access Denied
                    need_privilege = true;
                else
                {
                    Console.WriteLine(e);
                    _listener = null;
                    return;
                }
            }
            if (need_privilege)
            {
                var process_args = string.Format(@"http add urlacl url={0} user=BUILTIN\users", uri);
                new ElevatedProcessLauncher("netsh.exe", process_args);

                try
                {
                    _listener = new HttpListener();
                    _listener.Prefixes.Add(uri);
                    _listener.Start();
                }
                catch (HttpListenerException e)
                {
                    Console.WriteLine(e);
                    _listener = null;
                    return;
                }
            }

            
            _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
        }

        public void Stop()
        {
            if (_listener != null)
                _listener.Stop();

            if (_deleteReservation)
            {
                var process_args = string.Format(@"http del urlacl url={0}", _uri);
                new ElevatedProcessLauncher("netsh.exe", process_args);
            }
        }

        void ListenerCallback(IAsyncResult result)
        {
            if (_listener == null || !_listener.IsListening)
                return;

            try
            {
                var context = _listener.EndGetContext(result);
                var response = context.Response;

                if (_serverHit != null)
                {
                    // set readable properties in case client wants to do something more complicated than just play with the stream
                    Context = context;
                    Request = context.Request;
                    Response = context.Response;
                    
                    _serverHit(response.OutputStream);
                    
                    // clear readable properties, client is responsible for not touching them once the serverHit function returns
                    Response = null;
                    Request = null;
                    Context = null;
                }

                response.OutputStream.Close();
                response.Close();

            }
            catch (HttpListenerException )
            {
                // occassionally expected exception if client closes connection before request is processed
            }
            catch(Exception e)
            {
                Console.WriteLine(e); // unknown exception
            }

            // continue listening for next request
            _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
        }  
    }
#endif
}
