// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.Hub.Cosmos
{
    internal sealed class ReusedMemoryStream : MemoryStream {
        protected override void Dispose(bool disposing) { }
    }
    
    internal static class CosmosUtils
    {
        internal static async Task<List<JsonValue>> ReadDocuments(ObjectReader reader, Stream content, StreamBuffer buffer) {
            var documentsJson   = await KeyValueUtils.ReadToEndAsync(content, buffer).ConfigureAwait(false);
            var responseFeed    = reader.Read<ResponseFeed>(documentsJson);
            return responseFeed.Documents;
        }
        
        public static void WriteJson(MemoryStream memory, in JsonValue json) {
            memory.SetLength(0);
            memory.Write(json);
            memory.Flush();
            memory.Seek(0, SeekOrigin.Begin);
        }
    }
    
#pragma warning disable CS0649
    internal sealed class ResponseFeed {
        public    int             _count;
        public    List<JsonValue> Documents;
    }
}

#endif