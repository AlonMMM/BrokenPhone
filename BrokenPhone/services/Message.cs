using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrokenPhone.services
{
    abstract public class Message
    {
        protected string networking17;
        protected int uniqueNumber;
       
        public string Networking17
        {
            get { return networking17; }
        }

        public int UniqueNumber
        {
            get { return uniqueNumber; }

        }
        
        
        

        public Message(string networking17, int uniqueNumber)
        {
            this.networking17 = networking17;
            this.uniqueNumber = uniqueNumber;
        }

        public Message()
        {

        }
    }
}
