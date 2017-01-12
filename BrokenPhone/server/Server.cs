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
        public Socket tcpConnection = null;

        private Socket udpListener;
        private Client client;
        private bool hasFoundTcpConnection;
        private byte[] udpBuffer;
        private int tcpPort;

        private static readonly int udpPort = 6000;
        private static readonly IPAddress localIP = ProgramServices.GetLocalIPAddress();
        private byte[] brokenPhoneMessage = new byte[1024];
        public static string data;
        public enum RX_mode { ON, OFF };
        public RX_mode serverMode = RX_mode.OFF;
        private string clientName = "";

        public Server()
        {
            udpListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            tcpConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpPort = ProgramServices.findOpenPort(6000, 7000);
            hasFoundTcpConnection = false;
        }

        public void setClient(Client client)
        {
            this.client = client;
        }

        public void startListening()
        {
            ProgramServices.log("SERVER: starts listening in UDP...");
            udpListener.Bind(new IPEndPoint(IPAddress.Any, udpPort));
            udpBuffer = new byte[20];

            //start listning TCP
            Thread tcpKeepAliveThread = new Thread(listenAndHandleTCPconnection);
            tcpKeepAliveThread.Start();

            //start listning UDP
            EndPoint newClientEP = new IPEndPoint(IPAddress.Any, udpPort);
            udpListener.BeginReceiveFrom(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, udpListener);
        }

        //Listening for client connection in TCP
        private void listenAndHandleTCPconnection()
        {
            IPEndPoint ipEndPoint = new IPEndPoint(localIP, tcpPort);
            Socket handler = null;
            ProgramServices.log("SERVER: start Listing for TCP connection...");
            try
            {
                tcpConnection.Bind(ipEndPoint);
                tcpConnection.Listen(1);
                ProgramServices.log("SERVER: Waiting for a TCP connection...");
                // Thread is suspended while waiting for an incoming connection from client
                handler = tcpConnection.Accept();
                hasFoundTcpConnection = true;
                serverMode = RX_mode.ON;
                client.getBroadcastThread().Abort();
                while (handler.Connected)
                {
                    ProgramServices.log(string.Format("SERVER: Reading message from {0} TCP connection...", clientName));
                    // An incoming connection needs to be processed.
                    byte[] newMessage = new byte[1024];
                    handler.Receive(newMessage);
                    brokenPhoneMessage = newMessage;
                    data = Encoding.ASCII.GetString(ProgramServices.cleanUnusedBytes(brokenPhoneMessage));
                    client.handleMessageFromServerModule(data);
                }
            }
            catch (Exception e)
            {
                ProgramServices.log(e.ToString());
            }
            finally
            {
                if (handler != null)
                {
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
        }

        private void DoReceiveFrom(IAsyncResult ar)
        {
            try
            {
                Socket recvSock = (Socket)ar.AsyncState;
                EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
                if (serverMode == RX_mode.OFF)
                {
                    //Get the received "Request" message.       
                    int msgLen = recvSock.EndReceiveFrom(ar, ref clientEP);

                    //Check it's not a message from ourself
                    IPEndPoint remoteIpEndPoint = clientEP as IPEndPoint;
                    if (remoteIpEndPoint.Address.ToString() != ProgramServices.GetLocalIPAddress().ToString())
                    {
                        ProgramServices.log("SERVER: receives request...");
                        byte[] localMsg = new byte[msgLen];
                        Array.Copy(udpBuffer, localMsg, msgLen);
                        //send "Offer" message back           
                        sendOffer(localMsg, clientEP);
                        //start listening for a new "Request" message
                    }
                    EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
                    udpListener.BeginReceiveFrom(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, udpListener);
                }
                else
                {
                    recvSock.EndReceiveFrom(ar, ref clientEP);
                }
            }
            catch (Exception e)
            {
                EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
                udpListener.BeginReceiveFrom(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, udpListener);
            }
        }

        private void sendOffer(byte[] localMsg, EndPoint clientPoint)
        {
            ProgramServices.log("SERVER: sends offer...");

            // build "Offer" message
            // Create List of Bytes and add all the message we want to send to the list
            // finally convert the List to Array (of bytes)
            List<byte> toSendByteAsList = new List<byte>();
            toSendByteAsList.AddRange(Encoding.ASCII.GetBytes(ProgramServices.MY_ID).ToList());
            toSendByteAsList.AddRange(BitConverter.GetBytes(ProgramServices.uniqueNumber).ToList());
            string[] ipSpilittedByDots = ProgramServices.GetLocalIPAddress().ToString().Split('.');
            foreach (string ipPart in ipSpilittedByDots)
            {
                toSendByteAsList.Add(byte.Parse(ipPart));
            }
            short shotrPort = Convert.ToInt16(tcpPort);
            toSendByteAsList.AddRange(BitConverter.GetBytes(shotrPort).ToList());

            //send it
            clientName = System.Text.Encoding.Default.GetString(localMsg, 0, 16);
            ProgramServices.log(string.Format("SERVER: sends offer message to {0}", clientName));
            udpListener.SendTo(toSendByteAsList.ToArray(), clientPoint);
        }

    }
}
