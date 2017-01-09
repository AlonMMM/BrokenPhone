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
        private string clientMessage="00000000000000000000000000000000000000000000000000000000000000000000000";
        private string serverName = "CLIENT: I'm connected to server named: ";

        private Semaphore messageSemaphore = new Semaphore(0,2);
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

        public void handleMessageFromServerModule(string message)
        {
            if (clientMode == TX_mode.OFF)
            {
                // If the client is not connected, print message to screen
                ProgramServices.log(message);
            }
            else
            {
                clientMessage = changeOneCharacter(message).Trim();
                messageSemaphore.Release();                     //release the thread which sleep inside the client send-message. there is a new message to handle!
            }
        }

        public void broadcost()
        {
            ProgramServices.log("CLIENT: starts broadcasting in UDP...");
            udpBroadcast.EnableBroadcast = true;
            byte[] messageAsByteArray = createByteMessage();
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Broadcast, udpPort);
            udpBroadcast.Bind(new IPEndPoint(ProgramServices.GetLocalIPAddress(), ProgramServices.findOpenPort(5000, 6000)));
            int broadCounter = 1;
            //Broadcast "Request" messages
            new Thread(delegate ()
            {
                while (!hasFoundConnection && server.serverMode==Server.RX_mode.OFF)
                {
                    // TO_DO: ADD GLOBAL MUTEX (?)
                    ProgramServices.log(string.Format("CLIENT: UDP Broadcast message number {0}", broadCounter));
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
                ProgramServices.log("CLIENT: received offer in UDP... ");
                try
                {
                    //Get the received "offer" message.
                    byte[] localMsg = new byte[26];
                    Array.Copy(udpBuffer, localMsg, 26);
                    OfferMessage offerMessage = new OfferMessage(localMsg);
                    ProgramServices.log(string.Format("CLIENT: Received offer from: {0}, unique number: {1}, IP: {2}, Port: {3}", offerMessage.Networking17, offerMessage.UniqueNumber, offerMessage.Ip, offerMessage.Port));

                    //Stop listening in UDP
                    Socket recvSock = (Socket)ar.AsyncState;
                    EndPoint serverEP = new IPEndPoint(IPAddress.Any, 0);
                    recvSock.EndReceiveFrom(ar, ref serverEP);

                    //Make TCP connection
                    if (!connectTCP(offerMessage))
                    {
                        throw new Exception("CLIENT: The server is busy or not available... ");
                    }
                   
                }
                catch (Exception expection)
                {
                    ProgramServices.log("CLIENT: Could not connect to TCP" + expection.Message);

                    //start listing again
                    ProgramServices.log("CLIENT: Start listaning again..");
                    EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
                    udpBroadcast.BeginReceiveFrom(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, ref newClientEP, getOfferMessage, udpBroadcast);
                }
            }
        }

        private bool connectTCP(OfferMessage msg)
        {
            ProgramServices.log(string.Format("CLIENT: Connecting (TCP) to server----\nName: {0} IP: {1} ,  Port: {2}...", msg.Networking17, msg.Ip, msg.Port));
            tcpConnection.Connect(msg.Ip, msg.Port);
            hasFoundConnection = true;
            clientMode = TX_mode.ON;
            serverName = serverName + msg.Networking17;
            //tcpConnection.RemoteEndPoint.ToString();

            Thread sendMessageThred = new Thread(sendMessageToRemoteServer);
            sendMessageThred.Start();

            return true;
        }

        private void sendMessageToRemoteServer()
        {
            while (tcpConnection.Connected)
            {

                ProgramServices.log(serverName);
                string messageFromUser = "";
                if (server.serverMode == Server.RX_mode.OFF)
                {
                    messageSemaphore.Release();
                    // Server module is NOT connected
                    ProgramServices.log("Please enter a message since my server module did not find a client: ");
                    messageFromUser = Console.ReadLine();
                }
                messageSemaphore.WaitOne();
                // Encode the data string into a byte array.
                string messageForTcpConnection = messageFromUser != "" ? messageFromUser : clientMessage;
                byte[] msg = Encoding.ASCII.GetBytes(messageForTcpConnection);

                // Send the data through the socket.
                int bytesSent = tcpConnection.Send(ProgramServices.cleanUnusedBytes(msg));
                ProgramServices.log(clientMessage);
            }
                // Release the socket.
                tcpConnection.Shutdown(SocketShutdown.Both);
                tcpConnection.Close();
        }


        //Create "Request" message
        private byte[] createByteMessage()
        {
            List<byte> messageByteList = new List<byte>();
            messageByteList.AddRange(Encoding.ASCII.GetBytes(myID).ToList());
            messageByteList.AddRange(BitConverter.GetBytes(ProgramServices.uniqueNumber).ToList());
            return messageByteList.ToArray();
        }

        // Changes one character in the message
        private string changeOneCharacter(string msg)
        {
            string stringMessage = msg.Trim();
            Random random = new Random();
            int randomIndex = random.Next(stringMessage.Length);
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string randomChar = new string(Enumerable.Repeat(chars, 1)
              .Select(s => s[random.Next(s.Length)]).ToArray());
            char[] charArrayMessage = stringMessage.ToCharArray();
            charArrayMessage[randomIndex] = Convert.ToChar(randomChar);
            return new string(charArrayMessage);
        }

    }
}
