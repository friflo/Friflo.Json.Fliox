// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.DB.Auth;
using Friflo.Json.Fliox.DB.Host.Event;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host.Internal
{
    // ------------------------------------ MessageContext ------------------------------------
    /// <summary>
    /// One <see cref="MessageContext"/> is created per <see cref="ProtocolMessage"/> to enable
    /// multi threaded / concurrent request, response and event handling (processing).
    /// These message types a represented by <see cref="ProtocolRequest"/>, <see cref="ProtocolResponse"/> and
    /// <see cref="ProtocolEvent"/>.
    /// <br></br>
    /// Note: In case of adding transaction support for <see cref="SyncRequest"/>'s in future transaction data / state
    /// need to be handled by this class.
    /// </summary>
    public sealed class MessageContext
    {
        // --- public
        public    readonly  IPools              pools;

        // --- internal / private by intention
        /// <summary>Is set for clients requests only. In other words - from the initiator of a <see cref="ProtocolRequest"/></summary>
        internal            JsonKey             clientId;
        internal            ClientIdValidation  clientIdValidation;
        internal  readonly  IEventTarget        eventTarget;
        internal            AuthState           authState;
        private             PoolUsage           startUsage;
        internal            Action              canceler = () => {};
        
        public override     string              ToString() => $"userId: {authState.User}, auth: {authState}";

        internal MessageContext (IPools pools, IEventTarget eventTarget) {
            this.pools          = pools;
            startUsage          = pools.PoolUsage;
            this.eventTarget    = eventTarget;
        }
        
        internal MessageContext (IPools pools, IEventTarget eventTarget, in JsonKey clientId) {
            this.pools          = pools;
            startUsage          = pools.PoolUsage;
            this.eventTarget    = eventTarget;
            this.clientId       = clientId;
        }
        
        internal void Cancel() {
            canceler(); // canceler.Invoke();
        }

        internal void Release() {
            startUsage.AssertEqual(pools.PoolUsage);
        }
    }
}
