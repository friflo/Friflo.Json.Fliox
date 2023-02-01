// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Fliox.Hub.Remote.Udp
{
    public class UdpListener
    {
        private readonly    UdpClient   udpListener;
        private readonly    IPEndPoint  ipEndPoint;
        private readonly    FlioxHub    hub;
        private readonly    HostEnv     hostEnv = new HostEnv();
        
        public UdpListener(string endpoint, FlioxHub hub) {
            this.hub    = hub;
            if (!TryParseEndpoint(endpoint, out ipEndPoint)) {
                throw new ArgumentException($"invalid endpoint: {endpoint}", nameof(endpoint));
            }
            udpListener = new UdpClient(ipEndPoint);
        }

        public async Task Run() {
            await UdpSocketHost.SendReceiveMessages(udpListener, ipEndPoint, hub, hostEnv);
        }
        
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
            if (!IPAddress.TryParse(endpoint.Substring(0, addressLength), out IPAddress address)) {
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
            result = new IPEndPoint(address, port);
            return true;
        }
    }
}