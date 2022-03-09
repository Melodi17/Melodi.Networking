using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

namespace Melodi.Networking
{
    public class HTTPServer
    {
        /// <summary>
        /// Internal HTTP listener
        /// </summary>
        private HttpListener listener;
        /// <summary>
        /// Collection of addresses to access server from
        /// </summary>
        private readonly string[] Addresses;
        /// <summary>
        /// Control class for server
        /// </summary>
        private readonly Type Tp;
        /// <summary>
        /// Whether server is running
        /// </summary>
        public bool Running;
        /// <summary>
        /// Collection of methods used for requests
        /// </summary>
        private MethodInfo[] requestMethods;
        /// <summary>
        /// Collection of methods used for default requests (requests without a specific case)
        /// </summary>
        private MethodInfo[] defaultRequestMethods;
        /// <summary>
        /// Method used to filter whether to block connection
        /// </summary>
        private MethodInfo filterMethod;
        /// <summary>
        /// Method used in the event of an error
        /// </summary>
        private MethodInfo errorMethod;

        public HTTPServer(Type tp, params string[] addresses)
        {
            Addresses = addresses;
            Running = false;
            Tp = tp;
        }
        /// <summary>
        /// Start listener/server asynchronously
        /// </summary>
        /// <exception cref="Exception">Server has already been started</exception>
        public void Start()
        {
            if (Running)
                throw new Exception("Server already started");

            listener = new();

            foreach (string item in Addresses)
                listener.Prefixes.Add(item);

            listener.Start();

            filterMethod = Tp.GetMethods()
                        .Where(x => x.GetCustomAttribute<HTTPFilterAttribute>() != null
                            && x.ReturnType == typeof(bool))
                        .FirstOrDefault();

            errorMethod = Tp.GetMethods()
                        .Where(x => x.GetCustomAttribute<HTTPErrorAttribute>() != null)
                        .FirstOrDefault();

            defaultRequestMethods = Tp.GetMethods()
                        .Where(x => x.GetCustomAttribute<HTTPDefaultAttribute>() != null
                            && x.ReturnType == typeof((byte[], string)))
                        .ToArray();

            requestMethods = Tp.GetMethods()
                        .Where(x => x.GetCustomAttribute<HTTPRequestAttribute>() != null
                            && x.ReturnType == typeof((byte[], string)))
                        .ToArray();

            Running = true;

            Thread t = new(StartAsync);
            t.IsBackground = true;
            t.Start();
        }
        /// <summary>
        /// Stop server
        /// </summary>
        public void Stop()
        {
            Running = false;
        }
        /// <summary>
        /// Start server while on a background thread
        /// </summary>
        private void StartAsync()
        {
            while (Running)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();

                    if (filterMethod != null)
                        filterMethod.Invoke(null, new object[] { context.Request });

                    MethodInfo[] methods = requestMethods.Where(x =>
                    {
                        HTTPRequestAttribute attribute = x.GetCustomAttribute<HTTPRequestAttribute>();
                        return attribute.Path == context.Request.Url.AbsolutePath
                            && attribute.Protocol == context.Request.HttpMethod.ToLower();
                    }).ToArray();

                    MethodInfo[] defaultMethods = defaultRequestMethods.Where(x =>
                    {
                        HTTPDefaultAttribute attribute = x.GetCustomAttribute<HTTPDefaultAttribute>();
                        return attribute.Protocol == context.Request.HttpMethod.ToLower();
                    }).ToArray();

                    byte[] buffer;
                    string contentType;
                    if (methods.Any())
                    {
                        var x = ((byte[], string))methods.First().Invoke(null, new object[] { context });
                        buffer = x.Item1;
                        contentType = x.Item2;
                    }
                    else if (defaultMethods.Any())
                    {
                        var x = ((byte[], string))defaultMethods.First().Invoke(null, new object[] { context });
                        buffer = x.Item1;
                        contentType = x.Item2;
                    }
                    else
                        throw new NotImplementedException($"Method for handling request {context.Request.RawUrl} was missing from {Tp.Name} class");

                    context.Response.ContentType = contentType;
                    context.Response.ContentLength64 = buffer.Length;

                    Stream output = context.Response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
                catch (Exception e)
                {
                    if (errorMethod != null)
                        errorMethod.Invoke(null, new object[] { e });
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HTTPRequestAttribute : Attribute
    {
        public string Path => _path;
        private string _path;
        public string Protocol => _protocol;
        private string _protocol;
        public HTTPRequestAttribute(string path, string protocol)
        {
            _path = path;
            _protocol = protocol.ToLower();
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HTTPDefaultAttribute : Attribute
    {
        public string Protocol => _protocol;
        private string _protocol;
        public HTTPDefaultAttribute(string protocol)
        {
            _protocol = protocol.ToLower();
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HTTPErrorAttribute : Attribute
    {
        public HTTPErrorAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HTTPFilterAttribute : Attribute
    {
        public HTTPFilterAttribute() { }
    }

    public static class HTTPExtensions
    {
        /// <summary>
        /// Set cookie for HTTP listener's response
        /// </summary>
        /// <param name="response">Response to set</param>
        /// <param name="key">Key of cookie</param>
        /// <param name="value">Value of cookie</param>
        /// <param name="expiry">Expiry of cookie</param>
        public static void SetCookie(this HttpListenerResponse response, string key, object value, TimeSpan expiry)
        {
            string cookieDate = DateTime.UtcNow.Add(expiry).ToString("ddd, dd-MMM-yyyy H:mm:ss");
            response.Headers.Add("Set-Cookie", $"{key}={value};Path=/;Expires=" + cookieDate + " GMT");
        }
    }
}
