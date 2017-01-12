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
        private string clientMessage = "";
        private string serverName = "CLIENT: I'm connected to server named: ";
        private Queue<string> messagesFromServerModule;
        private Semaphore messageSemaphore = new Semaphore(0, Int32.MaxValue);
        private Thread broadcastThread;

        public enum TX_mode { ON, OFF };
        public TX_mode clientMode = TX_mode.OFF;

        public Client()
        {
            udpBroadcast = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            tcpConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            hasFoundConnection = false;
            udpBuffer = new byte[26];
            messagesFromServerModule = new Queue<string>();
        }

        //inject the server to the client
        public void setServer(Server server)
        {
            this.server = server;
        }

        public Thread getBroadcastThread() { return broadcastThread; }

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
                messagesFromServerModule.Enqueue(clientMessage);
                messageSemaphore.Release();  //release the thread which sleep inside the client send-message. there is a new message to handle.
            }
        }

        public void startBroadcosting()
        {
            ProgramServices.log("CLIENT: starts broadcasting in UDP...");
            udpBroadcast.EnableBroadcast = true;
            byte[] messageAsByteArray = createByteMessage();
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Broadcast, udpPort);
            udpBroadcast.Bind(new IPEndPoint(ProgramServices.GetLocalIPAddress(), ProgramServices.findOpenPort(5000, 6000)));

            //start broadcosting Thread
            broadcastThread = new Thread(() => broadcost(messageAsByteArray, ipEndPoint));
            broadcastThread.Start();

            //Listen if any "Offer" come back
            EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
            udpBroadcast.BeginReceiveFrom(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, ref newClientEP, getOfferMessage, udpBroadcast);
        }

        private void broadcost(byte[] messageAsByteArray, IPEndPoint ipEndPoint)
        {
            int broadCounter = 1;
            //Broadcast "Request" messages
            try
            {
                while (!hasFoundConnection && server.serverMode == Server.RX_mode.OFF)
                {
                    ProgramServices.log(string.Format("CLIENT: UDP Broadcast message number {0}", broadCounter));
                    udpBroadcast.SendTo(messageAsByteArray, ipEndPoint);
                    broadCounter++;
                    Thread.Sleep(1000);
                }
            }
            catch (ThreadAbortException e)
            {
                ProgramServices.log("CLIENT: broadcosting finished because my server module connected to a client");
                udpBroadcast.Dispose();
            }
        }

        private void getOfferMessage(IAsyncResult ar)
        {
            if (!hasFoundConnection && server.serverMode == Server.RX_mode.OFF)
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
            
            Thread sendMessageThred = new Thread(sendMessageToRemoteServer);
            sendMessageThred.Start();

            return true;
        }

        private void sendMessageToRemoteServer()
        {
            Thread.Sleep(1000); // give a chance for our server module to be connected to another client
            while (tcpConnection.Connected)
            {
                ProgramServices.log(serverName);
                string messageFromUser = "";
            getMessageFromUser:
                try
                {
                    if (server.serverMode == Server.RX_mode.OFF)
                    {
                        // Server module is NOT connected
                        ProgramServices.log("CLIENT: Please enter a message since my server module did not find a client:");
                        messageFromUser = Reader.ReadLine(10000); // give the user 10 seconds to enter message
                        messageSemaphore.Release();
                    }
                }
                catch (TimeoutException e)
                {
                    // if the server module still did not connect to a server, try to get a message from the user again.                
                    if (server.serverMode == Server.RX_mode.OFF)
                        goto getMessageFromUser;
                }

                // here the Thread will sleep if there are no messages in the messagesFromServerModule Queue.
                // the Thread would wake up only after another messages has been received and enqueued 
                // meaning handleMessageFromServerModule has been called from server module)
                messageSemaphore.WaitOne();

                // Encode the data string into a byte array.
                string messageForTcpConnection = messageFromUser != "" ? messageFromUser : messagesFromServerModule.Dequeue();
                byte[] msg = Encoding.ASCII.GetBytes(messageForTcpConnection);

                // Send the data through the socket.
                int bytesSent = tcpConnection.Send(ProgramServices.cleanUnusedBytes(msg));

            }
            // Release the socket.
            tcpConnection.Shutdown(SocketShutdown.Both);
            tcpConnection.Close();
        }


        //Create "Request" message
        private byte[] createByteMessage()
        {
            List<byte> messageByteList = new List<byte>();
            messageByteList.AddRange(Encoding.ASCII.GetBytes(ProgramServices.MY_ID).ToList());
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
