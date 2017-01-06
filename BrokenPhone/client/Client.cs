using BrokenPhone.server;
using BrokenPhone.services;
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
        private readonly int udpPort = 6000;
        private bool hasFoundConnection;
        private byte[] udpBuffer;
        private int port;
        private readonly string myID = "Networking17AMPM";
        private Mutex getOfferMessageMutex;

        public Client()
        {
            udpBroadcast = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            tcpConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            hasFoundConnection = false;
            udpBuffer = new byte[26];
            getOfferMessageMutex = new Mutex();
        }

        public void broadcost()
        {
            Console.WriteLine("Client starts broadcasting...");
            udpBroadcast.EnableBroadcast = true;
            byte[] messageAsByteArray = createByteMessage();
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Broadcast, udpPort);
            udpBroadcast.Bind(new IPEndPoint(ProgramServices.GetLocalIPAddress(), ProgramServices.findOpenPort(5000,6000)));
            int broadCounter = 1;
            //Broadcast "Request" messages
            new Thread(delegate()
            {
                while (!hasFoundConnection)
                {
                    
                    Console.WriteLine("Broadcast message number {0}", broadCounter);
                    udpBroadcast.SendTo(messageAsByteArray, ipEndPoint);
                    broadCounter++;
                    Thread.Sleep(1000);
                }
            }).Start();

            //Listen if any "Offer" come back
            EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
            udpBroadcast.BeginReceiveFrom(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, ref newClientEP, getOfferMessage, udpBroadcast);
        }

        private void getOfferMessage(IAsyncResult ar)
        {
            if (!hasFoundConnection)
            {
                Console.WriteLine("Client received offer...");
                try
                {
                    //Get the received "offer" message.
                    byte[] localMsg = new byte[26];
                    Array.Copy(udpBuffer, localMsg, 26);
                    OfferMessage offerMessage = new OfferMessage(localMsg);
                    Console.WriteLine("Received offer from: {0}, unique number: {1}, IP: {2}, Port: {3}", offerMessage.Networking17, offerMessage.UniqueNumber, offerMessage.Ip, offerMessage.Port);

                    //Make TCP connection

                    

                    //Stop listening in UDP
                    Socket recvSock = (Socket)ar.AsyncState;
                    EndPoint serverEP = new IPEndPoint(IPAddress.Any, 0);
                    recvSock.EndReceiveFrom(ar, ref serverEP);

                    if (!connectTCP(serverEP))
                    {
                        throw new Exception("The server is busy or not available... ");
                    }
                    hasFoundConnection = true;
                    
                }
                catch (Exception expection)
                {
                    Console.WriteLine("Could not connect to TCP" + expection.Message);

                    //start listing again
                    Console.WriteLine("Start listaning again..");
                    EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
                    udpBroadcast.BeginReceiveFrom(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, ref newClientEP, getOfferMessage, udpBroadcast);
                }
            }
        }

        private bool connectTCP(EndPoint serverEP)
        {

            Console.WriteLine("Connecting to server----\nIP:{0} ,  Port:{1}...",((IPEndPoint)serverEP).Address,((IPEndPoint)serverEP).Port);



            Console.WriteLine("Connection was made");
            return true;
        }

        private string[] extractMessage(byte[] localMsg)
        {
            throw new NotImplementedException();
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

    }
}
