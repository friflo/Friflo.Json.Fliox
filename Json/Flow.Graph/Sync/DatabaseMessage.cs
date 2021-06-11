// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Flow.Sync
{
    /// <summary>
    /// A container for different types of message classified into request, response and event messages.
    /// It is used in communication protocols (HTTP) which support more than request/response schema.
    /// So it is used for WebSocket's to notify about event messages having no related request message. 
    /// </summary>
    public class DatabaseMessage
    {
        public DatabaseResponse resp;
        public DatabaseEvent    ev;
    }
}