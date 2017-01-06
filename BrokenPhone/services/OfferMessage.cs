using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrokenPhone.services
{
    public class OfferMessage : Message
    {
        private string ip;
        private int port;
        private short shortPort;

        public string Ip
        {
            get { return ip; }
        }

        public int Port
        {
            get { return port; }

        }
               
        public OfferMessage(string networking17, int uniqueNumber, string ip, int port)
            : base(networking17, uniqueNumber)
        {
            this.ip = ip;
            this.port = port;
            shortPort = Convert.ToInt16(port);
        }
        public OfferMessage(byte[] message)
        {
            networking17 = System.Text.Encoding.Default.GetString(message, 0, 16);
            byte[] uniqueNumberAsByteArray = { message[16], message[17], message[18], message[19] };
            uniqueNumber = BitConverter.ToInt32(uniqueNumberAsByteArray, 0);
            this.ip = message[20] + "." + message[21] + "." + message[22] + "." + message[23];
            shortPort = BitConverter.ToInt16(message, 24);
            this.port = Convert.ToInt32(shortPort);
            if (port > 7000)
            {
                byte[] reversePort = { message[25] , message[24] };

                this.port = BitConverter.ToInt32(reversePort, 0);

            }
        }


    }
}
