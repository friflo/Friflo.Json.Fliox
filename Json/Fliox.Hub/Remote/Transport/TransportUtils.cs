// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    internal static class TransportUtils
    {
        internal static bool TryParseEndpoint(string endpoint, out IPEndPoint result) {
            int addressLength   = endpoint.Length;  // If there's no port then send the entire string to the address parser
            int lastColonPos    = endpoint.LastIndexOf(':');
            // Look to see if this is an IPv6 address with a port.
            if (lastColonPos > 0) {
                if (endpoint[lastColonPos - 1] == ']') {
                    addressLength = lastColonPos;
                }
                // Look to see if this is IPv4 with a port (IPv6 will have another colon)
                else if (endpoint.Substring(0, lastColonPos).LastIndexOf(':') == -1) {
                    addressLength = lastColonPos;
                }
            }
            result = null;
            var address = endpoint.Substring(0, addressLength);
            if (address == "localhost") address = "127.0.0.1";
            if (!IPAddress.TryParse(address, out IPAddress ipAddress)) {
                return false;
            }
            if (addressLength == endpoint.Length) {
                return false;
            }
            if (!int.TryParse(endpoint.AsSpan(addressLength + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int port)) {
                return false;
            }
            if (port > IPEndPoint.MaxPort) {
                return false;
            }
            result = new IPEndPoint(ipAddress, port);
            return true;
        }
        
        public static string Truncate(this string value) {
            const int maxLength = 120;
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength); 
        }
    }
    
    internal readonly struct UdpMeta
    {
        internal readonly   IPEndPoint  remoteEndPoint;

        public   override   string      ToString() => remoteEndPoint.ToString();

        internal UdpMeta (IPEndPoint remoteEndPoint) {
            this.remoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
        }
    }
}