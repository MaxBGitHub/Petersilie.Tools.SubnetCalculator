using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Petersilie.Tools.SubnetCalculator
{
    public class x
    {
        public static void Main(string[] args)
        {
            var subnet = SubnetCalculator.CalculateSubnet("google.com", 8);
        }
    }


    public static class SubnetCalculator
    {
        public static IPAddress GetNetmask(int prefix)
        {
            if (prefix < 0 || prefix > 32) {
                throw new ArgumentException(
                    "Prefix must be between 0 and 32.",
                    nameof(prefix)
                );
            }

            uint mask = ~(uint.MaxValue >> prefix);
            byte[] maskBytes = BitConverter.GetBytes(mask);
            if (BitConverter.IsLittleEndian) {
                maskBytes = maskBytes.Reverse().ToArray();
            }
            return new IPAddress(maskBytes);
        }


        public static IPAddress GetNetmaskInverse(int prefix)
        {
            if (prefix < 0 || prefix > 32) {
                throw new ArgumentException(
                    "Prefix must be between 0 and 32.",
                    nameof(prefix)
                );
            }

            uint mask = uint.MaxValue >> prefix;
            byte[] maskBytes = BitConverter.GetBytes(mask);
            if (BitConverter.IsLittleEndian) {
                maskBytes = maskBytes.Reverse().ToArray();
            }
            return new IPAddress(maskBytes);
        }


        public static uint GetHostCount(int prefix)
        {
            if (prefix < 0 || prefix > 31) {
                throw new ArgumentException(
                    "Prefix must be between 0 and 31.",
                    nameof(prefix)
                );
            }

            return (uint.MaxValue >> prefix) - 1;
        }


        public static IPAddress GetBroadcastAddress(IPAddress address, int prefix)
        {
            if (null == address) {
                throw new ArgumentNullException(nameof(address));
            }

            if (AddressFamily.InterNetwork != address.AddressFamily) {
                throw new NotSupportedException("Only supports IPv4 addresses.");
            }

            if (prefix > 30 && prefix < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(prefix),
                    prefix,
                    "Prefix must be between 1 and 30."
                );
            }

            uint mask = ~(uint.MaxValue >> prefix);
            uint numHosts = ~mask - 1;
            byte[] networkBytes = address.GetAddressBytes();
            byte[] maskBytes = BitConverter.GetBytes(mask);
            // Check for little-endian and reverse if neccessary.
            if (BitConverter.IsLittleEndian) {
                maskBytes = maskBytes.Reverse().ToArray();
            }
            var netmask = new IPAddress(maskBytes);

            byte[] cbBroadcast = new byte[networkBytes.Length];

            // Calculate network address and broadcast address
            for (int i = 0; i < networkBytes.Length; i++) {
                cbBroadcast[i]  = (byte)(networkBytes[i] | ~maskBytes[i]);
            }

            return new IPAddress(cbBroadcast);
        }


        public static IPAddress GetNetworkAddress(IPAddress address, int prefix)
        {
            if (null == address) {
                throw new ArgumentNullException(nameof(address));
            }

            if (AddressFamily.InterNetwork != address.AddressFamily) {
                throw new NotSupportedException("Only supports IPv4 addresses.");
            }

            if (prefix > 30 && prefix < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(prefix),
                    prefix,
                    "Prefix must be between 1 and 30."
                );
            }

            uint mask = ~(uint.MaxValue >> prefix);
            uint numHosts = ~mask - 1;
            byte[] networkBytes = address.GetAddressBytes();
            byte[] maskBytes = BitConverter.GetBytes(mask);
            // Check for little-endian and reverse if neccessary.
            if (BitConverter.IsLittleEndian) {
                maskBytes = maskBytes.Reverse().ToArray();
            }

            var netmask = new IPAddress(maskBytes);

            byte[] cbNetwork = new byte[networkBytes.Length];

            // Calculate network address and broadcast address
            for (int i = 0; i < networkBytes.Length; i++) {
                cbNetwork[i]    = (byte)(networkBytes[i] &  maskBytes[i]);
            }

            return new IPAddress(cbNetwork);
        }


        private static Subnet CalcSubnetInternal(IPAddress address, int prefix)
        {
            Subnet subnet = new Subnet() {
                IPAddress = address,
                Prefix = prefix
            };

            uint mask = ~(uint.MaxValue >> subnet.Prefix);
            uint numHosts = ~mask - 1;
            byte[] networkBytes = subnet.IPAddress.GetAddressBytes();
            byte[] maskBytes = BitConverter.GetBytes(mask);
            // Check for little-endian and reverse if neccessary.
            if (BitConverter.IsLittleEndian) {
                maskBytes = maskBytes.Reverse().ToArray();
            }
            subnet.Netmask = new IPAddress(maskBytes);

            byte[] cbNetwork = new byte[networkBytes.Length];
            byte[] cbBroadcast = new byte[networkBytes.Length];

            // Calculate network address and broadcast address
            for (int i = 0; i < networkBytes.Length; i++) {
                cbNetwork[i]    = (byte)(networkBytes[i] &  maskBytes[i]);
                cbBroadcast[i]  = (byte)(networkBytes[i] | ~maskBytes[i]);
            }
            subnet.Network = new IPAddress(cbNetwork);
            subnet.Broadcast = new IPAddress(cbBroadcast);

            /* Convert network address to uint to caculate first 
            ** and last host address. */
            var network =  (uint)cbNetwork[0] << 24;
                network += (uint)cbNetwork[1] << 16;
                network += (uint)cbNetwork[2] << 8;
                network += (uint)cbNetwork[3];

            uint uFirstHost = network + 1;
            uint uLastHost = network + numHosts;

            // Convert decimal host to byte.
            byte[] firstHostBytes = new byte[] {
                (byte)((uFirstHost & 0xff000000) >> 24),
                (byte)((uFirstHost & 0x00ff0000) >> 16),
                (byte)((uFirstHost & 0x0000ff00) >>  8),
                (byte)((uFirstHost & 0x000000ff)      )
            };
            subnet.FirstHost = new IPAddress(firstHostBytes);

            // Convert decimal host to byte.
            byte[] lastHostBytes = new byte[] {
                (byte)((uLastHost & 0xff000000) >> 24),
                (byte)((uLastHost & 0x00ff0000) >> 16),
                (byte)((uLastHost & 0x0000ff00) >>  8),
                (byte)((uLastHost & 0x000000ff)      )
            };
            subnet.LastHost = new IPAddress(lastHostBytes);

            return subnet;
        }



        public static Subnet CalculateSubnet(byte[] addressBytes, int prefix)
        {
            if (null == addressBytes) {
                throw new ArgumentNullException(nameof(addressBytes));
            }

            if (4 != addressBytes.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(addressBytes.Length),
                    addressBytes.Length,
                    "Size of array must be 4."
                );
            }


            if (prefix > 30 && prefix < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(prefix),
                    prefix,
                    "Prefix must be between 1 and 30."
                );
            }

            IPAddress ip = new IPAddress(addressBytes);

            Subnet subnet = CalcSubnetInternal(ip, prefix);
            return subnet;
        }


        public static Subnet CalculateSubnet(IPAddress ipAddress, int prefix)
        {            
            if (null == ipAddress) {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            if (AddressFamily.InterNetwork != ipAddress.AddressFamily) {
                throw new NotSupportedException("Only supports IPv4 addresses.");
            }

            if (prefix > 30 && prefix < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(prefix),
                    prefix,
                    "Prefix must be between 1 and 30."
                );
            }

            Subnet subnet = CalcSubnetInternal(ipAddress, prefix);
            return subnet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostNameOrAddress"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static Subnet CalculateSubnet(string hostNameOrAddress, int prefix)
        {
            if (string.IsNullOrEmpty(hostNameOrAddress)) {
                throw new ArgumentNullException(nameof(hostNameOrAddress));
            }

            IPAddress ipAddress = null;

            var hostEntry = Dns.GetHostEntry(hostNameOrAddress);
            if (hostEntry.AddressList.Length < 1) {
                throw new ArgumentException("Unable to resolve host name.",
                    nameof(hostNameOrAddress));
            }

            foreach (var entry in hostEntry.AddressList) {
                if (AddressFamily.InterNetwork == entry.AddressFamily) {
                    ipAddress = entry;
                    break;
                }
            }

            if (null == ipAddress) {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            if (AddressFamily.InterNetwork != ipAddress.AddressFamily) {
                throw new NotSupportedException("Only supports IPv4 addresses.");
            }

            if (prefix > 30 && prefix < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(prefix),
                    prefix,
                    "Prefix must be between 1 and 30."
                );
            }

            Subnet subnet = CalcSubnetInternal(ipAddress, prefix);
            return subnet;
        }
    }
}
