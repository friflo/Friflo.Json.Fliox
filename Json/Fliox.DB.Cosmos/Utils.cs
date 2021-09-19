// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.DB.Cosmos
{
    public class ResponseFeed {
        public  int             _count;
        public  List<JsonValue> Documents;
    }

    internal class ReusedMemoryStream : MemoryStream {
        protected override void Dispose(bool disposing) { }
    }
    
    internal static class CosmosUtils
    {
        internal static async Task<List<JsonValue>> ReadDocuments(ObjectReader reader, Stream content) {
            var documentsJson   = await EntityUtils.ReadToEnd(content).ConfigureAwait(false);
            var responseFeed    = reader.Read<ResponseFeed>(documentsJson.array);
            return responseFeed.Documents;
        }
        
        public static void WriteJson(MemoryStream memory, Utf8Array json) {
            memory.SetLength(0);
            memory.Write(json.array, 0, json.array.Length);
            memory.Flush();
            memory.Seek(0, SeekOrigin.Begin);
        }
    }
    
}

#endif