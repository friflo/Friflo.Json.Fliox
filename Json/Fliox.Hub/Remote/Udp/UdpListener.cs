// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
            ipEndPoint  = IPEndPoint.Parse(endpoint);
            udpListener = new UdpClient(ipEndPoint);
        }

        public async Task Run() {
            await UdpSocketHost.SendReceiveMessages(udpListener, ipEndPoint, hub, hostEnv);
        }
        
    }
}