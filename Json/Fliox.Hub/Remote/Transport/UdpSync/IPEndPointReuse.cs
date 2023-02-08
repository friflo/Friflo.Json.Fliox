// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Net;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote.Transport.Udp
{
    /// <summary>
    /// Optimization specific for Unity to avoid heap allocation in Socket.SendTo() method caused by
    /// <see cref="IPEndPoint.Serialize"/>
    /// </summary>
    public class IPEndPointReuse : IPEndPoint
    {
        private readonly    SocketAddress   address;
        private readonly    int             hashCode;
        
        public  override    int             GetHashCode() => hashCode;

        public IPEndPointReuse(long address, int port) : base(address, port) {
            this.address    = base.Serialize();
            hashCode        = base.GetHashCode();
        }

        public IPEndPointReuse(IPAddress address, int port) : base(address, port) {
            this.address    = base.Serialize();
            hashCode        = base.GetHashCode();
        }

        public override SocketAddress Serialize() => address;
        
        public override EndPoint Create(SocketAddress socketAddress) {
            if (address.Equals(socketAddress)) {
                return this;
            }
            return base.Create(socketAddress);
        }
    }
}