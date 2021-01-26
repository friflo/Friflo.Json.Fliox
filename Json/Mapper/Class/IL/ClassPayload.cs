// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Mapper.Class.IL
{
    public class ClassPayload : IDisposable
    {
        // payload size changes, depending on which class is used at the current classLevel
        internal readonly List<byte> data = new List<byte>(32);
        
        public void Dispose() {
        }

        public void StoreInt(int pos, int value) {

        }

        public int LoadInt(int pos) {
            return 0;
        }

    }
}