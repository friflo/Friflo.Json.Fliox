// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.DB.Sync;
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
            using (StreamReader streamReader = new StreamReader(content)) {
                string documentsJson    = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                var responseFeed        = reader.Read<ResponseFeed>(documentsJson);
                return responseFeed.Documents;
            }
        }
            
        internal static void AddEntities(
            List<JsonValue>                     documents,
            string                              keyName,
            Dictionary<JsonKey, EntityValue>    entities,
            MessageContext                      messageContext)
        {
            using (var pooledValidator = messageContext.pools.EntityValidator.Get()) {
                var validator = pooledValidator.instance;
                foreach (var document in documents) {
                    if (!validator.GetEntityKey(document.json, ref keyName, out JsonKey keyValue, out _)) {
                        continue;
                    }
                    var value   = new EntityValue(document.json);
                    entities.Add(keyValue, value);
                }
            }
        }
        
        internal static void WriteJson(StreamWriter writer, MemoryStream memory, string json) {
            memory.SetLength(0);
            writer.Write(json);
            writer.Flush();
            memory.Seek(0, SeekOrigin.Begin);
        }
    }
}

#endif