// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnassignedReadonlyField
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public sealed class Signaling : FlioxClient
    {
        public CommandTask<AddHostResult>       AddHost      (AddHost  param)       => send.Command<AddHost,       AddHostResult>        (param);
        public CommandTask<ConnectClientResult> ConnectClient(ConnectClient param)  => send.Command<ConnectClient, ConnectClientResult>  (param);
        
        // --- containers
        public  readonly    EntitySet <string, WebRtcHost>         hosts;

        public Signaling (FlioxHub hub, string dbName = null) : base(hub, dbName) { }
    }
}
