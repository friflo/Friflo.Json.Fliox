// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.DB.Sync;

namespace Friflo.Json.Fliox.DB.NoSQL.Remote
{
    /// <summary>
    /// Specify how <see cref="DatabaseRequest"/>'s and <see cref="DatabaseResponse"/>'s are send via the used
    /// transmission protocol. E.g. HTTP or WebSockets.
    /// In case of <see cref="BiDirect"/> (used for WebSockets) requests and responses are encapsulated in a <see cref="DatabaseMessage"/>.
    /// Otherwise (HTTP) requests and responses are send / received as they are. Meaning they are not encapsulated.
    /// </summary>
    public enum ProtocolType {
        /// requests and responses are not encapsulated. Used for HTTP
        ReqResp,
        /// requests and responses are encapsulated in a <see cref="DatabaseMessage"/>. Used for WebSockets.
        /// <see cref="BiDirect"/> supports sending <see cref="DatabaseRequest"/> from both sides of a peer-to-peer
        /// connection.
        BiDirect
    }
}