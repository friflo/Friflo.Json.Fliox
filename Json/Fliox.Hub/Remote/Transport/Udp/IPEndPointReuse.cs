// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Net;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote.Transport.Udp
{
    /// <summary>
    /// Optimization specific for Unity to avoid heap allocation in Socket.SendTo() methods caused by
    /// <see cref="IPEndPoint.Serialize"/>.<br/>
    /// Class is not used in CLR.
    /// </summary>
    internal sealed class IPEndPointReuse : IPEndPoint
    {
        private readonly    SocketAddress   address;
        private readonly    int             hashCode;
        
        public  override    int             GetHashCode()   => hashCode;
        public override     SocketAddress   Serialize()     => address;

        internal IPEndPointReuse(IPAddress address, int port) : base(address, port) {
            this.address    = base.Serialize();
            hashCode        = base.GetHashCode();
        }
        
        public static IPEndPoint Create(IPAddress address, int port) {
#if UNITY_5_3_OR_NEWER
            return new IPEndPointReuse (address, port);
#else
            return new IPEndPoint      (address, port);
#endif
        }

        /// Method is called from Socket.SendTo() in Unity 2021.3.9f1. Not utilized in CLR. 
        public override EndPoint Create(SocketAddress socketAddress) {
            if (address.Equals(socketAddress)) {
                return this;
            }
            return base.Create(socketAddress);
        }
    }
}