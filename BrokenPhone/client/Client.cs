using BrokenPhone.server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrokenPhone.client
{
    public class Client
    {
        private Socket udpBroadcast;
        private Socket tcpConnection;
        private Server server;
        private int udpPort;
        private bool hasFoundConnection;
        private byte[] udpBuffer;
        private int port;
        private readonly string myID = "Networking17AMPM";

        public Client()
        {
            udpBroadcast = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            tcpConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            hasFoundConnection = false;
            udpBuffer = new byte[26];
        }

        public void broadcost()
        {
            udpBroadcast.EnableBroadcast = true;
            byte[] messageAsByteArray = createByteMessage();
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Broadcast, 6000);
            udpBroadcast.Bind(new IPEndPoint(GetLocalIPAddress(), 6027)); //TO-DO: Change from 6027 to random open port
            //Broadcast "Request" messages
            new Thread(delegate()
            {
                while (!hasFoundConnection)
                {
                    udpBroadcast.SendTo(messageAsByteArray, ipEndPoint);
                    Thread.Sleep(1000);
                }
            }).Start();

            //Listen if any "Offer" come back
            EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
            udpBroadcast.BeginReceiveFrom(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, ref newClientEP, connectToTcp, udpBroadcast);
        }

        private void connectToTcp(IAsyncResult ar)
        {
            hasFoundConnection = true;
            Console.ReadKey();
        }

        //Create "Request" message
        private byte[] createByteMessage()
        {
            List<byte> messageByteList = new List<byte>();
            messageByteList.AddRange(Encoding.ASCII.GetBytes(myID).ToList());
            Random random = new Random();
            int randomNumber = random.Next();
            messageByteList.AddRange(BitConverter.GetBytes(randomNumber).ToList());
            return messageByteList.ToArray();
        }

        private IPAddress GetLocalIPAddress()
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

        private int findOpenPort()
        {
            int myPort = udpPort;
            bool alreadyInUse = true;
            while (alreadyInUse && myPort < 7000)
            {
                myPort++;
                alreadyInUse = (from p in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners() where p.Port == myPort select p).Count() == 1;
            }
            return myPort;
        }


    }
}
