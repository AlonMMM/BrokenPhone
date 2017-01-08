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
        private string clientMessage;

        public enum TX_mode { ON, OFF };
        public TX_mode clientMode = TX_mode.OFF;

        public Client()
        {
            udpBroadcast = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            tcpConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            hasFoundConnection = false;
            udpBuffer = new byte[26];
        }

        //inject the server to the client
        public void setServer(Server server)
        {
            this.server = server;
        }

        public void broadcost()
        {
            Console.WriteLine("CLIENT: starts broadcasting in UDP...");
            udpBroadcast.EnableBroadcast = true;
            byte[] messageAsByteArray = createByteMessage();
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Broadcast, udpPort);
            udpBroadcast.Bind(new IPEndPoint(ProgramServices.GetLocalIPAddress(), ProgramServices.findOpenPort(5000, 6000)));
            int broadCounter = 1;
            //Broadcast "Request" messages
            new Thread(delegate ()
            {
                while (!hasFoundConnection)
                {

                    Console.WriteLine("CLIENT: UDP Broadcast message number {0}", broadCounter);
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
                Console.WriteLine("CLIENT: received offer in UDP... ");
                try
                {
                    //Get the received "offer" message.
                    byte[] localMsg = new byte[26];
                    Array.Copy(udpBuffer, localMsg, 26);
                    OfferMessage offerMessage = new OfferMessage(localMsg);
                    Console.WriteLine("CLIENT: Received offer from: {0}, unique number: {1}, IP: {2}, Port: {3}", offerMessage.Networking17, offerMessage.UniqueNumber, offerMessage.Ip, offerMessage.Port);

                    //Stop listening in UDP
                    Socket recvSock = (Socket)ar.AsyncState;
                    EndPoint serverEP = new IPEndPoint(IPAddress.Any, 0);
                    recvSock.EndReceiveFrom(ar, ref serverEP);

                    //Make TCP connection
                    if (!connectTCP(offerMessage.Ip, offerMessage.Port, clientMessage))
                    {
                        throw new Exception("CLIENT: The server is busy or not available... ");
                    }
                    hasFoundConnection = true;
                    clientMode = TX_mode.ON;
                }
                catch (Exception expection)
                {
                    Console.WriteLine("CLIENT: Could not connect to TCP" + expection.Message);

                    //start listing again
                    Console.WriteLine("CLIENT: Start listaning again..");
                    EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
                    udpBroadcast.BeginReceiveFrom(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, ref newClientEP, getOfferMessage, udpBroadcast);
                }
            }
        }

        private bool connectTCP(string ip, int port, string message)
        {
            Console.WriteLine("Connecting (TCP) to server----\nIP: {0} ,  Port: {1}...", ip, port);
            tcpConnection.Connect(ip, port);
            tcpConnection.RemoteEndPoint.ToString();

            // Encode the data string into a byte array.
            byte[] msg = Encoding.ASCII.GetBytes("This is a test<EOF>");

            // Send the data through the socket.
            int bytesSent = tcpConnection.Send(msg);

            // Receive the response from the remote device.
            byte[] recieveFromServer = new byte[1024];
            int bytesRec = tcpConnection.Receive(recieveFromServer);
            Console.WriteLine("Echoed test = {0}", Encoding.ASCII.GetString(recieveFromServer, 0, bytesRec));

            // Release the socket.
            tcpConnection.Shutdown(SocketShutdown.Both);
            tcpConnection.Close();
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
