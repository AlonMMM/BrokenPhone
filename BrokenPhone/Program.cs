using BrokenPhone.client;
using BrokenPhone.server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BrokenPhone
{
    class Program
    {
        static Socket udpSocket;
        static void Main(string[] args)
        {
            //TESST ETSETSETSETSTSTE
            Server server = new Server();
            Client client = new Client();
            server.startListening();
            client.broadcost();
            Console.ReadKey();
        }

        private static void testServer()
        {
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ipEndPoint = new IPEndPoint(GetLocalIPAddress(), 6000);
            IPEndPoint ipStartPoint = new IPEndPoint(GetLocalIPAddress(), 6001);
            udpSocket.Bind(ipStartPoint);
            string message = "Networking17AMPM" + Int32.MaxValue;
            byte[] messageAsByteArray = Encoding.ASCII.GetBytes(message);
            udpSocket.SendTo(messageAsByteArray, ipEndPoint);
            Console.ReadKey();
        }

        private static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
    }
}
