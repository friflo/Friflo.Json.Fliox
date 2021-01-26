// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Mapper.Class.IL
{
    public class ClassPayload : IDisposable
    {
        private readonly byte[] data; // payload size is known exactly upfront - class layout is fixed
        
        public void Dispose() {
        }

        public void StoreInt(int pos, int value) {

        }

        public int LoadInt(int pos) {
            return 0;
        }

    }
}