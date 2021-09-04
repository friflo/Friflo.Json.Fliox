// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using Friflo.Json.Fliox.Mapper;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Fliox.DB.Cosmos
{
    public class DocumentContainer {
        public  int             _count;
        public  List<JsonValue> Documents;
    }
    
    
    public class ReusedMemoryStream : MemoryStream {
        protected override void Dispose(bool disposing) { }
    }
}

#endif