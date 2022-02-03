using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Melodi.Networking
{
    public class UDPConnection
    {
        /// <summary>
        /// Port to listen and send on
        /// </summary>
        private int port;
        /// <summary>
        /// Internal udpCLient
        /// </summary>
        private UdpClient udpClient;
        private IAsyncResult asyncResult = null;
        /// <summary>
        /// Async thread
        /// </summary>
        private Thread thread = null;
        /// <summary>
        /// Event to be triggered when a message is recieved on <see cref="port"/>
        /// </summary>
        public Action<IPEndPoint, byte[], string> onMessage = null;
        public UDPConnection(int port)
        {
            this.port = port;
            this.udpClient = new(port);
        }
        /// <summary>
        /// Start listener asynchronously
        /// </summary>
        /// <exception cref="Exception">Listener was already started</exception>
        public void Start()
        {
            if (thread != null)
            {
                throw new Exception("Already started, stop first");
            }

            StartListening();
        }
        /// <summary>
        /// Stop listener
        /// </summary>
        public void Stop()
        {
            try
            {
                thread = null;
                udpClient.Close();
            }
            catch { /* Don't care */ }
        }
        /// <summary>
        /// Start listening
        /// </summary>
        private void StartListening()
        {
            asyncResult = udpClient.BeginReceive(Receive, new object());
        }
        /// <summary>
        /// Recieve asynchronous request
        /// </summary>
        /// <param name="ar">Async result</param>
        private void Receive(IAsyncResult ar)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, this.port);
            byte[] bytes = udpClient.EndReceive(ar, ref ip);
            string message = Encoding.ASCII.GetString(bytes);

            if (this.onMessage != null)
            {
                this.onMessage.Invoke(ip, bytes, message);
            }

            StartListening();
        }
        /// <summary>
        /// Sends UDP data
        /// </summary>
        /// <param name="message">Data to send</param>
        /// <param name="port">Port to send to</param>
        /// <param name="dest">IPAdress to send to</param>
        public void Send(string message, int? port, IPAddress? dest)
        {
            UdpClient client = new();
            IPEndPoint ip = new(dest ?? IPAddress.Parse("255.255.255.255"), port ?? this.port);
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            client.Send(bytes, bytes.Length, ip);
            client.Close();
            //Console.WriteLine("Sent: {0} ", message);
        }
    }
}
