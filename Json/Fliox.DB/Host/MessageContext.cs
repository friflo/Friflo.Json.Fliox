// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.DB.Auth;
using Friflo.Json.Fliox.DB.Host.Event;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Remote;

namespace Friflo.Json.Fliox.DB.Host
{
    // ------------------------------------ MessageContext ------------------------------------
    /// <summary>
    /// One <see cref="MessageContext"/> is created per <see cref="SyncRequest"/> instance to enable
    /// multi threaded and concurrent request handling.
    /// <br></br>
    /// Note: In case of adding transaction support in future transaction data/state will be stored here.
    /// </summary>
    public class MessageContext
    {
        /// <summary>Is set for clients requests only. In other words - from the initiator of a <see cref="ProtocolRequest"/></summary>
        public              string          clientId;
        public  readonly    IPools          pools;
        public  readonly    IEventTarget    eventTarget;
        public              AuthState       authState;
        
        private             PoolUsage       startUsage;
        public              Action          canceler = () => {};
        public override     string          ToString() => $"clientId: {clientId}, auth: {authState}";

        public MessageContext (IPools pools, IEventTarget eventTarget) {
            this.pools          = pools;
            startUsage          = pools.PoolUsage;
            this.eventTarget    = eventTarget;
        }
        
        public MessageContext (IPools pools, IEventTarget eventTarget, string clientId) {
            this.pools          = pools;
            startUsage          = pools.PoolUsage;
            this.eventTarget    = eventTarget;
            this.clientId       = clientId;
        }
        
        public void Cancel() {
            canceler(); // canceler.Invoke();
        }

        public void Release() {
            startUsage.AssertEqual(pools.PoolUsage);
        }
    }
}
