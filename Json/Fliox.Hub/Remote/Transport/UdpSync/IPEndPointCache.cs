// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote.Transport.Udp
{
    /// <summary>
    /// Optimization specific for Unity to avoid heap allocation in Socket.ReceiveFrom() method caused by
    /// <see cref="IPEndPoint.Serialize"/>
    /// </summary>
    public class IPEndPointCache : IPEndPoint
    {
        private readonly    SocketAddress                               address;
        private readonly    int                                         hashCode;
        private readonly    Dictionary<SocketAddress, IPEndPointReuse>  cachedIPEndPoints = new Dictionary<SocketAddress, IPEndPointReuse>();
        
        public  override    int                                         GetHashCode()   => hashCode;
        /// Method is called from Socket.ReceiveFrom() in Unity 2021.3.9f1. It is not called in MS CLR
        public  override    SocketAddress                               Serialize()     => address;

        private IPEndPointCache(IPAddress address, int port) : base(address, port) {
            this.address    = base.Serialize();
            hashCode        = base.GetHashCode();
        }
        
        public static IPEndPoint Create(IPAddress address, int port) {
#if UNITY_5_3_OR_NEWER
            return new IPEndPointCache(address, port);
#else
            return new IPEndPoint     (address, port);
#endif
        }

        /// Method is called from Socket.ReceiveFrom() in Unity 2021.3.9f1. It is not called in MS CLR
        public override EndPoint Create(SocketAddress socketAddress) {
            // var hc = socketAddress.GetHashCode(); // test if hash code is cached 
            if (cachedIPEndPoints.TryGetValue(socketAddress, out var endPoint)) {
                return endPoint;
            }
            var newIpEndPoint   = (IPEndPoint)base.Create(socketAddress);
            var newAddress      = newIpEndPoint.Address;
            var newPort         = newIpEndPoint.Port;
            var newEndpoint     = new IPEndPointReuse(newAddress, newPort);
            cachedIPEndPoints[socketAddress] = newEndpoint;
            return newEndpoint;
        }
    }
}