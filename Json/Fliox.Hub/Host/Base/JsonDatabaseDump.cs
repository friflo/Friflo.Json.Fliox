// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Host.Utils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    public class JsonDatabaseDumpResult
    {
        public readonly     Dictionary<string, int> containers;
        public readonly     string                  error;
        public              int                     EntityCount => GetEntityCount();

        public override     string                  ToString() => GetString();

        internal JsonDatabaseDumpResult(string error, Dictionary<string, int> containers) {
            this.containers = new Dictionary<string, int>(containers);
            this.error = error;
        }
        
        private int GetEntityCount() {
            int count = 0;
            foreach (var pair in containers) {
                count += pair.Value;
            }
            return count;
        }
        
        private string GetString()
        {
            var sb = new StringBuilder();
            sb.Append("entities: ");
            sb.Append(EntityCount);
            if (error != null) {
                sb.Append(" - error: ");
                sb.Append(error);
            }
            return sb.ToString();
        }
    }
    
    public class JsonDatabaseDumpReader
    {
        private readonly    EntityProcessor         processor = new EntityProcessor();
        private             Utf8JsonParser          parser;
        private             JsonValue               databaseJson;
        private             int                     readEntityCount;
        private readonly    Dictionary<string, int> containers = new Dictionary<string, int>();
        
        public JsonDatabaseDumpResult Read(JsonValue json, MemoryDatabase database)
        {
            databaseJson = json;
            containers.Clear();
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
        
        private JsonDatabaseDumpResult ReadContainer(MemoryDatabase database)
        {
            while (true) {
                var ev = parser.NextEvent();
                switch (ev)
                {
                    case JsonEvent.ArrayStart:
                        readEntityCount = 0;
                        var containerName = new ShortString(parser.key.ToString());
                        var container = database.GetOrCreateContainer(containerName);
                        if (container == null) {
                            return CreateResult($"container not found. was: {containerName} at position: {parser.Position}");
                        }
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
        
        private JsonDatabaseDumpResult ReadRecords(EntityContainer container)
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
                        containers[container.name] = readEntityCount;
                        return null;
                    case JsonEvent.Error:
                        containers[container.name] = readEntityCount;
                        return CreateResult(parser.error.GetMessage());
                    default:
                        containers[container.name] = readEntityCount;
                        return CreateResult($"expect object. was: {ev} at position: {parser.Position}");
                }
            }
        }
        
        private JsonDatabaseDumpResult CreateResult(string error) {
            var result = new JsonDatabaseDumpResult(error, containers);
            return result;
        }
    }
}