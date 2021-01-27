// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;

namespace Friflo.Json.Mapper.Class.IL
{
    // This class contains IL specific state/data which is used by JsonReader & JsonWriter. So its not thread safe.
    public class ClassPayload : IDisposable
    {
        // payload size changes, depending on which class is used at the current classLevel
        internal    ValueList<byte> data = new ValueList<byte>(32, AllocType.Persistent);

        public ClassPayload () { }
        
        public void Dispose() {
            data.Dispose();
        }

        public void StoreInt(int pos, int value) {

        }

        public int LoadInt(int pos) {
            return 0;
        }

    }
}