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
            //testRandomChar();

            Server server = new Server();
            Client client = new Client();
            server.setClient(client);
            client.setServer(server);
            server.startListening();
            client.startBroadcosting();
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
