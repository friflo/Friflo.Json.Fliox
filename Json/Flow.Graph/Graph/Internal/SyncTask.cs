// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Flow.Graph.Internal
{
    public abstract class SyncTask
    {
        internal abstract string      Label  { get; }
        internal abstract bool        Synced { get; }
        
        internal Exception AlreadySyncedError() {
            return new TaskAlreadySyncedException($"Task already synced. {Label}");
        }
        
        internal Exception RequiresSyncError(string message) {
            return new TaskNotSyncedException($"{message} {Label}");
        }
    }
}