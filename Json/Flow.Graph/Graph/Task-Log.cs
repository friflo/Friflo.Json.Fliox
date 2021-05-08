// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
    public class LogTask : SyncTask
    {
        internal readonly   List<EntityPatch>   patches = new List<EntityPatch>();
        public   int        Count               => patches.Count;

        internal            TaskState           state;
        internal override   TaskState           State       => state;
        
        internal override   string              Label       => $"LogTask #patches: {patches.Count}";

        internal LogTask() { }
    }
    
}