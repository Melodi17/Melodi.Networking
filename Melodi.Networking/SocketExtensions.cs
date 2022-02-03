using System.Net.Sockets;
using System.Runtime.Serialization;

namespace Melodi.Networking
{
    public static class SocketExtensions
    {
        private static ObjectIDGenerator generator = new();
        /// <summary>
        /// Accurate check if <paramref name="socket"/> is connected
        /// </summary>
        /// <param name="socket">Socket to check</param>
        /// <returns>If <paramref name="socket"/> is connected</returns>
        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException) { return false; }
        }
        /// <summary>
        /// Unique ID for identifing a socket
        /// </summary>
        /// <param name="client">Client to get ID from</param>
        /// <returns>Clients unique ID</returns>
        public static long SocketId(this TcpClient client)
        {
            return generator.GetId(client, out _);
        }
    }
}
