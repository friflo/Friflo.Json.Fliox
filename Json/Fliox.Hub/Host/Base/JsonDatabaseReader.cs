// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    public class JsonDatabaseReaderResult
    {
        public readonly     Dictionary<string, int> Containers;
        public readonly     string                  error;
        
        internal JsonDatabaseReaderResult(string error, Dictionary<string, int> containers) {
            Containers = new Dictionary<string, int>(containers);
            this.error = error;
        }
    }
    
    public class JsonDatabaseReader
    {
        private readonly    EntityProcessor processor = new EntityProcessor();
        private             Utf8JsonParser  parser;
        private             JsonValue       databaseJson;
        private             int             readEntityCount;
        private readonly    Dictionary<string, int> Containers = new Dictionary<string, int>();
        
        public JsonDatabaseReaderResult Read(JsonValue json, MemoryDatabase database)
        {
            databaseJson = json;
            Containers.Clear();
            parser.InitParser(json);
            var ev = parser.NextEvent();
            
            switch (ev)
            {
                case JsonEvent.ObjectStart:
                    var result = ReadContainer(database);
                    if (result != null) {
                        return result;
                    }
                    return CreateResult(null);
                case JsonEvent.Error:
                    return CreateResult(parser.error.GetMessage());
                default:
                    return CreateResult($"expect object. was: {ev} at position: {parser.Position}");
            }
        }
        
        private JsonDatabaseReaderResult ReadContainer(MemoryDatabase database)
        {
            while (true) {
                var ev = parser.NextEvent();
                switch (ev)
                {
                    case JsonEvent.ArrayStart:
                        readEntityCount = 0;
                        var containerName = new ShortString(parser.key.ToString());
                        var container = database.GetOrCreateContainer(containerName);
                        var result = ReadRecords(container);
                        if (result != null) {
                            return result;
                        }
                        break;
                    case JsonEvent.ObjectEnd:
                        return null;
                    case JsonEvent.Error:
                        return CreateResult(parser.error.GetMessage());
                    default:
                        return CreateResult($"expect array. was: {ev} at position: {parser.Position}");
                }
            }
        }
        
        private JsonDatabaseReaderResult ReadRecords(EntityContainer container)
        {
            var memoryContainer = (MemoryContainer)container;
            var keyName = memoryContainer.keyName;
            while (true) 
            {
                var ev = parser.NextEvent();
                switch (ev)
                {
                    case JsonEvent.ObjectStart:
                        readEntityCount++;
                        var start   = parser.Position - 1;
                        parser.SkipTree();
                        var record = new JsonValue(databaseJson.MutableArray, start, parser.Position - start);
                        processor.GetEntityKey(record, keyName, out var key, out var error);
                        memoryContainer.AddKeyValue(key, record);
                        break;
                    case JsonEvent.ArrayEnd:
                        Containers[container.name] = readEntityCount;
                        return null;
                    case JsonEvent.Error:
                        Containers[container.name] = readEntityCount;
                        return CreateResult(parser.error.GetMessage());
                    default:
                        Containers[container.name] = readEntityCount;
                        return CreateResult($"expect object. was: {ev} at position: {parser.Position}");
                }
            }
        }
        
        private JsonDatabaseReaderResult CreateResult(string error) {
            var result = new JsonDatabaseReaderResult(error, Containers);
            return result;
        }
    }
}