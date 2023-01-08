// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst.Utils;

// ReSharper disable SwapViaDeconstruction
namespace Friflo.Json.Fliox.Hub.Host.Event.Collector
{
    internal sealed class DatabaseChanges
    {
        internal readonly   SmallString                                 dbName;
        internal readonly   Dictionary<SmallString, ContainerChanges>   containers;
        internal            TaskBuffer                                  writeBuffer;
        internal            TaskBuffer                                  readBuffer;
        
        internal DatabaseChanges(in SmallString name) {
            dbName      = name;
            containers  = new Dictionary<SmallString, ContainerChanges>(SmallString.Equality);
            writeBuffer = new TaskBuffer();
            readBuffer  = new TaskBuffer();
        }
        
        internal void SwapBuffers() {
            var temp    = writeBuffer;
            writeBuffer = readBuffer;
            readBuffer  = temp;
            writeBuffer.Clear();
        }
    }
}