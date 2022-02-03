using System;
using System.Net;
using System.Net.Sockets;

namespace Melodi.Networking
{
    public class TCPRemoteClient : IDisposable
    {
        /// <summary>
        /// Unique ID for identifing client
        /// </summary>
        public long ID => client.SocketId();
        /// <summary>
        /// Accurately check if client is connected
        /// </summary>
        public bool Connected => client.Client.IsConnected();
        /// <summary>
        /// Raw socket of client
        /// </summary>
        public Socket RawSocket => client.Client;
        /// <summary>
        /// Remote IP of client
        /// </summary>
        public IPEndPoint RemoteIP => client.Client.RemoteEndPoint as IPEndPoint;
        /// <summary>
        /// Local IP of client
        /// </summary>
        public IPEndPoint LocalIP => client.Client.LocalEndPoint as IPEndPoint;
        /// <summary>
        /// Internal TCP client
        /// </summary>
        private TcpClient client;
        private TCPRemoteClient(TcpClient client)
        {
            this.client = client;
        }
        /// <summary>
        /// Get network stream from client
        /// </summary>
        /// <returns>Client's netork stream</returns>
        public NetworkStream GetStream()
        {
            return client.GetStream();
        }
        /// <summary>
        /// Disconnect from client
        /// </summary>
        public void Disconnect()
        {
            client.Close();
        }
        /// <summary>
        /// Disconnects and disposes client
        /// </summary>
        public void Dispose()
        {
            Disconnect();
            client.Dispose();
        }

        public static implicit operator TCPRemoteClient(TcpClient client) => new(client);
    }
}
