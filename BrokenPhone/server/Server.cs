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
        private bool hasFoundConnection;
        private byte[] udpBuffer;
        private int tcpPort;
        private readonly string myID = "Networking17AMPM";
        private static readonly int udpPort = 6000;
        private static readonly IPAddress localIP = ProgramServices.GetLocalIPAddress();
        private byte[] brokenPhoneMessage = new Byte[1024];

        public static string data;
        public enum RX_mode { ON, OFF };
        public RX_mode clientMode = RX_mode.OFF;
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

            //start listning TCP
            listniAndHandleTCPconnection();
            //start listning UDP
            EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 6000);       
            udpListener.BeginReceiveFrom(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, udpListener);
                    
        }

        private void listniAndHandleTCPconnection()
        {

            IPEndPoint ipEndPoint = new IPEndPoint(localIP, 6001);
   
            tcpConnection.Bind(ipEndPoint);

            Console.WriteLine("TCP server start Listing for connection...");
            try
            {
                tcpConnection.Bind(ipEndPoint);
                tcpConnection.Listen(1);

                // Start listening for connections.
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    // Program is suspended while waiting for an incoming connection.
                    Socket handler = tcpConnection.Accept();
                    data = null;

                    // An incoming connection needs to be processed.
                    handler.Receive(brokenPhoneMessage);
                    data = Encoding.ASCII.GetString(brokenPhoneMessage);
                   
                    

                    // Show the data on the console.
                    Console.WriteLine("Text received : {0}", data);

                    // Echo the data back to the client.
                    byte[] msg = Encoding.ASCII.GetBytes(data);

                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

           
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
