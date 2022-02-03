using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Melodi.Networking
{
    public class TCPClient
    {
        /// <summary>
        /// Address to TCP server
        /// </summary>
        private string host;
        /// <summary>
        /// Port of TCP server
        /// </summary>
        private int port;
        /// <summary>
        /// Internal TCP client
        /// </summary>
        private TcpClient tcpClient;
        /// <summary>
        /// Whether listener is running
        /// </summary>
        private bool running = false;
        /// <summary>
        /// Async listener thread
        /// </summary>
        private Thread thread = null;
        /// <summary>
        /// Event to be triggered when a messaged is recieved from <see cref="host"/> on <see cref="port"/>
        /// </summary>
        public Action<string> OnMessage = null;
        /// <summary>
        /// Event to be triggered when <see cref="host"/> connects on <see cref="port"/>
        /// </summary>
        public Action OnConnect = null;
        /// <summary>
        /// Event to be triggered when <see cref="host"/> disconnects from <see cref="port"/>
        /// </summary>
        public Action OnDisconnect = null;
        /// <summary>
        /// Event to be triggered when client fails to connect to <see cref="host"/> on <see cref="port"/>
        /// </summary>
        public Action OnConnectFailed = null;

        public TCPClient(string host, int port)
        {
            this.host = host;
            this.port = port;
        }
        /// <summary>
        /// Starts client asynchronously
        /// </summary>
        public void Start()
        {
            this.tcpClient = new();
            try
            {
                this.tcpClient.Connect(this.host, this.port);
            }
            catch (Exception)
            {
                Stop();
                HandleConnectFailed();
            }
            HandleConnect();
            running = true;
            StreamReader reader = new(this.tcpClient.GetStream());

            thread = new(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                while (running)
                {
                    try
                    {
                        string message = reader.ReadLine();
                        if (!running || message == null || !tcpClient.Client.IsConnected())
                        {
                            break;
                        }
                        HandleMessage(message);
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
                if (running)
                    HandleDisconnect();
            });
            thread.Start();
        }
        /// <summary>
        /// Handle message being recieved from <see cref="host"/> on <see cref="port"/>
        /// </summary>
        /// <param name="buffer">Message recieved</param>
        private void HandleMessage(string buffer)
        {
            this.OnMessage?.Invoke(buffer);
        }
        /// <summary>
        /// Handle server disconnect from <see cref="host"/> on <see cref="port"/>
        /// </summary>
        private void HandleDisconnect()
        {
            this.OnDisconnect?.Invoke();
        }
        /// <summary>
        /// Handle server connect from <see cref="host"/> on <see cref="port"/>
        /// </summary>
        private void HandleConnect()
        {
            this.OnConnect?.Invoke();
        }
        /// <summary>
        /// Handle fail to connect to server at <see cref="host"/> on <see cref="port"/>
        /// </summary>
        private void HandleConnectFailed()
        {
            this.OnConnectFailed?.Invoke();
        }
        /// <summary>
        /// Send <paramref name="buffer"/> to <see cref="host"/> on <see cref="port"/>
        /// </summary>
        /// <param name="buffer"></param>
        public void Send(string buffer)
        {
            StreamWriter writer = new(this.tcpClient.GetStream());
            writer.WriteLine(buffer);
            writer.Flush();
        }
        /// <summary>
        /// Stop client and listener
        /// </summary>
        public void Stop()
        {
            this.tcpClient.Close();
            running = false;
        }
    }
}
