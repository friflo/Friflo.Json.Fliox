// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable SwapViaDeconstruction
namespace Friflo.Json.Fliox.Hub.Host.Event.Collector
{
    internal sealed class DatabaseChanges
    {
        internal readonly   ShortString                                 dbName;
        internal readonly   Dictionary<ShortString, ContainerChanges>   containers;
        internal            TaskBuffer                                  writeBuffer;
        internal            TaskBuffer                                  readBuffer;
        
        internal DatabaseChanges(in ShortString name) {
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