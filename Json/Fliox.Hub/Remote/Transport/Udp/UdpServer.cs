// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    public sealed class UdpServer
    {
        private readonly    UdpClient   udpListener;
        private readonly    IPEndPoint  ipEndPoint;
        private readonly    FlioxHub    hub;
        private readonly    HostEnv     hostEnv = new HostEnv();
        
        public UdpServer(string endpoint, FlioxHub hub) {
            this.hub    = hub;
            if (!TransportUtils.TryParseEndpoint(endpoint, out ipEndPoint)) {
                throw new ArgumentException($"invalid endpoint: {endpoint}", nameof(endpoint));
            }
            udpListener = new UdpClient(ipEndPoint);
        }

        public async Task Run() {
            await UdpSocketHost.SendReceiveMessages(udpListener, ipEndPoint, hub, hostEnv);
        }
    }
}