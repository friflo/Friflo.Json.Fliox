// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable SwapViaDeconstruction
namespace Friflo.Json.Fliox.Hub.Host.Event.Collector
{
    internal sealed class DatabaseChanges
    {
        internal readonly   JsonKey                                     dbName;
        internal readonly   Dictionary<ShortString, ContainerChanges>   containers;
        internal            TaskBuffer                                  writeBuffer;
        internal            TaskBuffer                                  readBuffer;
        
        internal DatabaseChanges(in JsonKey name) {
            dbName      = name;
            containers  = new Dictionary<ShortString, ContainerChanges>(ShortString.Equality);
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