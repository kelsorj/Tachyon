using System;
using System.Net;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public class IpAddressHelp
    {
        /// <summary>
        /// Pass this the name of a host and it tells you if this is the local machine or not
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static bool IsLocalIpAddress(string host)
        {
            try
            { 
                IPAddress[] hostIPs = Dns.GetHostAddresses(host);
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                // test if any host IP equals to any local IP or to localhost
                foreach (IPAddress hostIP in hostIPs)
                {
                    // is localhost
                    if (IPAddress.IsLoopback(hostIP)) 
                        return true;
                    // is local address
                    foreach (IPAddress localIP in localIPs)
                        if (hostIP.Equals(localIP)) 
                            return true;
                }
            }
            catch { }
            return false;
        }

        public static IPAddress GetFirstNonLoopBackAddress()
        {
            IPAddress loop_back = IPAddress.Parse("127.0.0.1");
            try
            {
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                foreach(var ip in localIPs)
                {
                    if (IPAddress.IsLoopback(ip))
                    {
                        loop_back = ip;
                        continue;
                    }
                    return ip;
                }
            }
            catch
            {
            
            }
            return loop_back;
        }
    }
#endif
}
