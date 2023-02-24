// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.Hub.WebRTC.DB
{
    public class Signaling : FlioxClient
    {
        public CommandTask<AddHostResult>     AddHost (AddHost param) => send.Command<AddHost, AddHostResult>   (param);
        
        // --- containers
        public  readonly    EntitySet <string, WebRtcPeer>         peers;

        public Signaling (FlioxHub hub, string dbName = null) : base(hub, dbName) { }
    }
}
