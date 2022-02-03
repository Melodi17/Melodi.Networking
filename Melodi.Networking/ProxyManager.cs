using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Melodi.Networking
{
    public static class ProxyManager
    {
        /// <summary>
        /// Check if proxy is required to access external sites
        /// </summary>
        public static bool ProxyRequired => IsProxyRequired();
        /// <summary>
        /// Url to use to check and authenticate proxy
        /// </summary>
        public static string CheckUrl = "https://google.com";
        /// <summary>
        /// Check if proxy is required to access external sites
        /// </summary>
        /// <returns>If proxy required</returns>
        private static bool IsProxyRequired()
        {
            var det = WebRequest.GetSystemWebProxy();
            Uri proxyurl = det.GetProxy(new Uri(CheckUrl));
            return proxyurl != null;
        }
        /// <summary>
        /// Gets web proxy for network using supplied <paramref name="credentials"/>
        /// </summary>
        /// <param name="credentials">Network credentials to use to authenticate web proxy</param>
        /// <returns>A generated web proxy with the url and supplied <paramref name="credentials"/></returns>
        public static WebProxy GetWebProxy(NetworkCredential credentials = null)
        {
            var det = WebRequest.GetSystemWebProxy();
            Uri proxyurl = det.GetProxy(new Uri(CheckUrl));
            if (proxyurl != null)
            {
                WebProxy proxy = new(proxyurl);
                proxy.Credentials = credentials;
                return proxy;
            }
            else
            {
                return null;
            }
        }
    }
}
