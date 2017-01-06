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
        private int tcpPort;
        private readonly string myID = "Networking17AMPM";
        private static readonly int udpPort = 6000;

        public Server()
        {
            udpListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            tcpConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpPort = ProgramServices.findOpenPort(6000,7000);
            hasFoundConnection = false;
        }

        public void setClient(Client client)
        {
            this.client = client;
        }

        public void startListening()
        {
            Console.WriteLine("Server starts listening in UDP...");
            udpListener.Bind(new IPEndPoint(IPAddress.Any, udpPort));
            udpBuffer = new byte[20];
            EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);       
            
            //new Thread(delegate()
            //{
            //    while (!hasFoundConnection)
            //    {
                    udpListener.BeginReceiveFrom(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, udpListener);
            //    }
            //}).Start();
        }

        private void DoReceiveFrom(IAsyncResult ar)
        {
            Console.WriteLine("Servers receives request...");

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
            sendOffer(localMsg, clientEP);
        }

        private void sendOffer(byte[] localMsg, EndPoint clientPoint)
        {
            Console.WriteLine("Server sends offer...");
            
            // build "Offer" message
            // Create List of Bytes and add all the message we want to send to the list
            // finally convert the List to Array (of bytes)
            int uniqueNum = BitConverter.ToInt32(localMsg, 16);
            List<byte> toSendByteAsList = new List<byte>();
            toSendByteAsList.AddRange(Encoding.ASCII.GetBytes(myID).ToList()); 
            toSendByteAsList.AddRange(BitConverter.GetBytes(uniqueNum).ToList());
            string[] ipSpilittedByDots = ProgramServices.GetLocalIPAddress().ToString().Split('.');
            foreach (string ipPart in ipSpilittedByDots)
            {
                toSendByteAsList.Add(byte.Parse(ipPart));
            }
            short shotrPort = Convert.ToInt16(tcpPort);
            toSendByteAsList.AddRange(BitConverter.GetBytes(shotrPort).ToList());

            //send it
            Console.WriteLine("Server sends offer message to {0}", uniqueNum);
            udpListener.SendTo(toSendByteAsList.ToArray(), clientPoint);

        }

    }
}
