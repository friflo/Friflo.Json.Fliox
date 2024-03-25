// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    /// <summary>
    /// <see cref="HubRights"/> used to set general request / connection permissions.
    /// </summary>
    public sealed class HubRights
    {
        /// <summary>
        /// If <b>true</b> the hub store all unacknowledged events for a client in a FIFO queue and send them on reconnects.<br/>
        /// </summary>
        public bool? queueEvents;
    }
}