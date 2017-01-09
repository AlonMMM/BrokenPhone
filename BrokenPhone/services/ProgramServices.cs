using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BrokenPhone
{
    public static class ProgramServices
    {
        public static readonly string MY_ID = "Networking17AMPM";
        public static readonly int uniqueNumber = new Random().Next();

        public static IPAddress GetLocalIPAddress()
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

        public static int findOpenPort(int min, int max)
        {
            int myPort = min;
            bool alreadyInUse = true;
            while (alreadyInUse && myPort < max + 1)
            {
                myPort++;
                alreadyInUse = (from p in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners() where p.Port == myPort select p).Count() == 1;
            }
            return myPort;
        }

        public static byte[] cleanUnusedBytes(byte[] byteArr)
        {
            List<byte> cleanedArr = new List<byte>();
            for (int i = 0; i < byteArr.Length; i++)
            {
                if (byteArr[i] == 0)
                    break;
                cleanedArr.Add(byteArr[i]);
            }
            return cleanedArr.ToArray();
        }

        public static void log(string message)
        {
            Console.WriteLine(MY_ID + ": " + message);
        }
    }
}
