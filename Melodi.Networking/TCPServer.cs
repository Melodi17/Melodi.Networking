using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Melodi.Networking
{
    public class TCPServer
    {
        /// <summary>
        /// Port to listen on
        /// </summary>
        private int port;
        /// <summary>
        /// Internal tcp listener
        /// </summary>
        private TcpListener tcpListener;
        /// <summary>
        /// Whether listener is/should be running
        /// </summary>
        private bool running = false;
        /// <summary>
        /// Async listener thread
        /// </summary>
        private Thread thread = null;
        /// <summary>
        /// List of connected clients
        /// </summary>
        public List<TCPRemoteClient> Clients = new();
        /// <summary>
        /// Event to be triggered by a message being recieved from a client on <see cref="port"/>
        /// </summary>
        public Action<TCPRemoteClient, string> OnMessage = null;
        /// <summary>
        /// Event to be triggered when a client connects on <see cref="port"/>
        /// </summary>
        public Action<TCPRemoteClient> OnConnect = null;
        /// <summary>
        /// Event to be triggered when a client disconnects from <see cref="port"/>
        /// </summary>
        public Action<TCPRemoteClient> OnDisconnect = null;
        public TCPServer(int port)
        {
            this.port = port;
            this.tcpListener = new(IPAddress.Any, port);
        }
        /// <summary>
        /// Asynchronously start listening for connections on <see cref="port"/>
        /// </summary>
        /// <exception cref="Exception">Listener is already started</exception>
        public void Start()
        {
            if (thread != null)
            {
                throw new Exception("Already started, stop first");
            }

            tcpListener.Start();
            running = true;

            thread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                while (running)
                {
                    try
                    {
                        TCPRemoteClient client = tcpListener.AcceptTcpClient();
                        if (running)
                        {
                            HandleConnect(client);
                        }
                    }
                    catch (Exception e) { /* Don't Care */ }
                }
            });
            thread.Start();
        }
        /// <summary>
        /// Disconnect all connected clients
        /// </summary>
        public void KillConnections()
        {
            foreach (TCPRemoteClient item in Clients)
            {
                item.Disconnect();
            }
        }
        /// <summary>
        /// Stop listener
        /// </summary>
        public void Stop()
        {
            tcpListener.Stop();
            running = false;
            KillConnections();
        }
        /// <summary>
        /// Send <paramref name="buffer"/> to <paramref name="client"/>
        /// </summary>
        /// <param name="client">Client to sent <paramref name="buffer"/> to</param>
        /// <param name="buffer">Message to be sent to <paramref name="client"/></param>
        public void Send(TCPRemoteClient client, string buffer)
        {
            StreamWriter writer = new(Clients.First(x => x.ID == client.ID).GetStream());
            writer.WriteLine(buffer);
            writer.Flush();
        }
        /// <summary>
        /// Handle connect to listener
        /// </summary>
        /// <param name="client">Client that connected</param>
        private void HandleConnect(TCPRemoteClient client)
        {
            Clients.Add(client);
            StreamReader reader = new(client.GetStream());

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                while (running)
                {
                    try
                    {
                        string response = reader.ReadLine();
                        if (!running) { break; }
                        if (!client.Connected || response == null) { break; }
                        HandleMessage(client, response);
                    }
                    catch (Exception) { break; }
                }
                if (running)
                {
                    HandleDisconnect(client);
                }
            }).Start();

            OnConnect?.Invoke(client);
        }
        /// <summary>
        /// Handle message from <paramref name="client"/>
        /// </summary>
        /// <param name="client">Client <paramref name="buffer"/> recieved from</param>
        /// <param name="buffer">Message recieved from <paramref name="client"/></param>
        private void HandleMessage(TCPRemoteClient client, string buffer)
        {
            OnMessage?.Invoke(client, buffer);
        }
        /// <summary>
        /// Handle disconnect to listener
        /// </summary>
        /// <param name="client">Client that disconnected</param>
        private void HandleDisconnect(TCPRemoteClient client)
        {
            Clients.RemoveAll(x => x.ID == client.ID);

            OnDisconnect?.Invoke(client);
        }
    }
}
