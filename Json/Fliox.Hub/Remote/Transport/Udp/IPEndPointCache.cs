// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote.Transport.Udp
{
    /// <summary>
    /// Optimization specific for Unity to avoid heap allocation in Socket.ReceiveFrom() method caused by
    /// <see cref="IPEndPoint.Serialize"/>.<br/>
    /// Class is not used in CLR.
    /// </summary>
    internal sealed class IPEndPointCache : IPEndPoint
    {
        private readonly    SocketAddress                           address;
        private readonly    int                                     hashCode;
        private readonly    Dictionary<AddressKey, IPEndPointReuse> cachedIPEndPoints;
        
        public  override    int                                     GetHashCode()   => hashCode;
        /// Method is called from Socket.ReceiveFrom() in Unity 2021.3.9f1. Not utilized in CLR. 
        public  override    SocketAddress                           Serialize()     => address;

        private IPEndPointCache(IPAddress address, int port) : base(address, port) {
            this.address        = base.Serialize();
            hashCode            = base.GetHashCode();
            cachedIPEndPoints   = new Dictionary<AddressKey, IPEndPointReuse>(AddressKey.Equality);
        }
        
        internal static IPEndPoint Create(IPAddress address, int port) {
#if UNITY_5_3_OR_NEWER
            return new IPEndPointCache(address, port);
#else
            return new IPEndPoint     (address, port);
#endif
        }

        /// Method is called from Socket.ReceiveFrom() in Unity 2021.3.9f1. It is not called in MS CLR
        public override EndPoint Create(SocketAddress socketAddress) {
            // Unity/Mono SocketAddress.GetHashCode() implementation is not reliable. Its hash key it mutable.
            // => use immutable implementation
            var key = new AddressKey(socketAddress);
            
            if (cachedIPEndPoints.TryGetValue(key, out var endPoint)) {
                return endPoint;
            }
            var newIpEndPoint   = (IPEndPoint)base.Create(socketAddress);
            var newAddress      = newIpEndPoint.Address;
            var newPort         = newIpEndPoint.Port;
            var newEndpoint     = new IPEndPointReuse(newAddress, newPort);
            cachedIPEndPoints[key] = newEndpoint;
            return newEndpoint;
        }
    }
    
    internal readonly struct AddressKey
    {
        internal readonly int       hashCode;
        // IPv6AddressSize = 28  =>  4 long's provide 32 bytes 
        internal readonly long      bytes0;     // byte[0 .. 7]
        internal readonly long      bytes8;     // byte[8 ..15]
        internal readonly long      bytes16;    // byte[16..23]
        internal readonly long      bytes24;    // byte[24..31]
        
        internal static readonly  AddressKeyEqualityComparer Equality    = new AddressKeyEqualityComparer();

        internal AddressKey(SocketAddress address) {
            int size    = address.Size;
            
            long B(int index) => index < size ? address[index] : 0;
            
            bytes0  = B (0) | B (1) <<  8 | B (2) << 16 | B (3) << 24 | B (4) << 32 | B (5) << 40  | B (6) << 48 | B (7) << 56;
            bytes8  = B (8) | B (9) <<  8 | B(10) << 16 | B(11) << 24 | B(12) << 32 | B(13) << 40  | B(14) << 48 | B(15) << 56;
            bytes16 = B(16) | B(17) <<  8 | B(18) << 16 | B(19) << 24 | B(20) << 32 | B(21) << 40  | B(22) << 48 | B(23) << 56;
            bytes24 = B(24) | B(25) <<  8 | B(26) << 16 | B(27) << 24 | B(28) << 32 | B(29) << 40  | B(30) << 48 | B(31) << 56;
            
            hashCode    = unchecked((int) bytes0) ^ (int)( bytes0 >> 32) ^
                          unchecked((int) bytes8) ^ (int)( bytes8 >> 32) ^
                          unchecked((int)bytes16) ^ (int)(bytes16 >> 32) ^
                          unchecked((int)bytes24) ^ (int)(bytes24 >> 32);
        }
    }
    
    internal sealed class AddressKeyEqualityComparer : IEqualityComparer<AddressKey>
    {
        public int  GetHashCode(AddressKey value)       => value.hashCode;
        
        public bool Equals(AddressKey x, AddressKey y) {
            return x.bytes0 == y.bytes0 && x.bytes8 == y.bytes8 && x.bytes16 == y.bytes16 && x.bytes24 == y.bytes24;
        }
    }
}