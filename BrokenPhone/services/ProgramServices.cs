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
            int myPort = min - 1;
            bool alreadyInUse = true;
            while (alreadyInUse && myPort < max + 1)
            {
                myPort++;
                alreadyInUse = (from p in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners() where p.Port == myPort select p).Count() == 1;
            }
            return myPort;
        }
    }
}
