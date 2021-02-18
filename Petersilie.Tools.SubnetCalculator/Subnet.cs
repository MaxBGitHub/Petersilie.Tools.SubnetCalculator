using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace Petersilie.Tools.SubnetCalculator
{
    public struct Subnet
    {
        public IPAddress IPAddress;
        public IPAddress Network;
        public IPAddress Netmask;
        public int Prefix;
        public IPAddress Broadcast;
        public IPAddress Gateway;
        public IPAddress DHCP;
        public IPAddress[] DNS;
        public IPAddress FirstHost;
        public IPAddress LastHost;
    }
}
