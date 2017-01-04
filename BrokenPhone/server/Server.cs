using BrokenPhone.client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace BrokenPhone.server
{
    public class Server
    {
        private Socket udpListener;
        private Socket tcpConnection;
        private Client client;
        private bool hasFoundConnection;
        private byte[] udpBuffer;
        private int randomPort;
        private readonly string myID = "Networking17AMPM";
        private static readonly int udpPort = 6000;

        public Server()
        {
            udpListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            tcpConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            randomPort = findOpenPort();
            hasFoundConnection = false;
        }

        public void setClient(Client client)
        {
            this.client = client;
        }

        public void startListening()
        {
            udpListener.Bind(new IPEndPoint(IPAddress.Any, udpPort));
            udpBuffer = new byte[1024];
            EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
            //new Thread(delegate()
            //{
            //    while (true)
            //    {
                    udpListener.BeginReceiveFrom(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, udpListener);
            //    }
            //}).Start();
        }

        private void DoReceiveFrom(IAsyncResult ar)
        {

            //Get the received "Request" message.
            Socket recvSock = (Socket)ar.AsyncState;
            EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
            int msgLen = recvSock.EndReceiveFrom(ar, ref clientEP);
            byte[] localMsg = new byte[msgLen];
            Array.Copy(udpBuffer, localMsg, msgLen);

            //start listening for a new "Request" message
            EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
            udpListener.BeginReceiveFrom(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, udpListener);
          
            //send "Offer" message back           
            //IPEndPoint clientPoint = (IPEndPoint)recvSock.RemoteEndPoint;
            sendOffer(localMsg, clientEP);
        }

        private void sendOffer(byte[] localMsg, EndPoint clientPoint)
        {          
            // build "Offer" message
            string recievedMessageStr = System.Text.Encoding.Default.GetString(localMsg);
            string recievedMessageUniqNum = recievedMessageStr.Substring(0,recievedMessageStr.Length-1);
            string toSendStr = myID + recievedMessageUniqNum + GetLocalIPAddress().ToString() + udpPort;
            byte[] toSendByte = Encoding.ASCII.GetBytes(toSendStr);

            //send it
            udpListener.SendTo(toSendByte, clientPoint);
        }


        private int findOpenPort()
        {
            int myPort = udpPort;
            bool alreadyInUse = true;
            while (alreadyInUse && myPort<7000) {
                myPort++;
                alreadyInUse = (from p in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners() where p.Port == myPort select p).Count() == 1;           
            }
            return myPort;
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

    }
}
