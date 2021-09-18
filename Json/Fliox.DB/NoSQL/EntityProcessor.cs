// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.NoSQL
{
    internal enum ProcessingType {
        Validate,
        GetKey,
        SetKey
    }
    
    /// <summary>
    /// Is used to ensure that <see cref="ReadEntitiesResult"/> returned by <see cref="EntityContainer.ReadEntities"/>
    /// contains valid <see cref="ReadEntitiesResult.entities"/>.
    /// Validation is required for <see cref="EntityDatabase"/> implementations which cannot ensure that the value of
    /// its key/values are JSON. See <see cref="ReadEntitiesResult.ValidateEntities"/>.
    /// </summary>
    public class EntityProcessor : IDisposable
    {
        private             Bytes           jsonBytes   = new Bytes(128);
        private             JsonParser      parser;
        private             Bytes           idKey       = new Bytes(16);
        private             bool            foundKey;
        //                  --- ReplaceKey
        private             int             keyStart;
        private             int             keyEnd;
        private             bool            foundIntKey;
        private             long            longValue;
        private             bool            asIntKey;
        private readonly    StringBuilder   sb          = new StringBuilder();
        
        public bool GetEntityKey(string json, ref string keyName, out JsonKey keyValue, out string error) {
            return Traverse(json, ref keyName, out keyValue, ProcessingType.GetKey,   out error);
        }
        
        public bool Validate(string json, ref string keyName, out JsonKey keyValue, out string error) {
            return Traverse(json, ref keyName, out keyValue, ProcessingType.Validate, out error);
        }
        
        public string ReplaceKey(string json, ref string keyName, bool asIntKey, string newKeyName, out JsonKey keyValue, out string error) {
            this.asIntKey   = asIntKey;
            keyName         = keyName ?? "id";
            bool equalKeys  = keyName == newKeyName;
            if (!Traverse  (json, ref keyName, out keyValue, ProcessingType.SetKey,   out error))
                return null;
            if (equalKeys && foundIntKey == asIntKey)
                return json;
            sb.Clear();
            sb.Append(json, 0, keyStart);
            sb.Append('\"');
            sb.Append(newKeyName);
            sb.Append("\":");
            if (asIntKey) {
                sb.Append(longValue);
            } else {
                sb.Append('\"');
                keyValue.AppendTo(sb);
                sb.Append('\"');
            }
            var remaining = json.Length - keyEnd; 
            sb.Append(json, keyEnd, remaining);
            var result = sb.ToString();
            sb.Clear();
            return result;
        }

        private bool Traverse (string json, ref string keyName, out JsonKey keyValue, ProcessingType processingType, out string error) {
            foundKey = false;
            idKey.Clear();
            keyName  = keyName ?? "id";
            idKey.AppendString(keyName);
            keyValue = new JsonKey();
            jsonBytes.Clear();
            jsonBytes.AppendString(json);
            parser.InitParser(jsonBytes);
            var ev = parser.NextEvent();
            if (ev != JsonEvent.ObjectStart) {
                error   = $"entity value must be an object.";
                return false;
            }
            while (true) {
                var pos = parser.Position;
                ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                        if (!parser.key.IsEqualBytes(ref idKey))
                            break;
                        foundKey = true;
                        keyValue = new JsonKey(ref parser.value, ref parser.valueParser);
                        switch (processingType) {
                            case ProcessingType.Validate:
                                continue;
                            case ProcessingType.GetKey:
                                error       = null;
                                return true;
                            case ProcessingType.SetKey:
                                keyStart    = pos;
                                keyEnd      = parser.Position;
                                foundIntKey = ev == JsonEvent.ValueNumber;
                                if (asIntKey  && ev == JsonEvent.ValueString)
                                    continue;
                                if (!asIntKey && ev == JsonEvent.ValueNumber) {
                                    longValue = parser.ValueAsLong(out foundIntKey);
                                    continue;
                                }
                                error       = null;
                                return true;
                        }
                        break;
                    case JsonEvent.ValueBool:
                    case JsonEvent.ValueNull:
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        parser.SkipTree();
                        if (!parser.error.ErrSet)
                            break;
                        error = parser.error.msg.AsString();
                        return false;
                    case JsonEvent.Error:
                        error = parser.error.msg.AsString();
                        return false;
                    case JsonEvent.ObjectEnd:
                        ev = parser.NextEvent();
                        if (ev != JsonEvent.EOF) {
                            error = "Expected EOF in JSON value";
                            return false;
                        }
                        if (foundKey) {
                            error = null;
                            return true;
                        }
                        error = $"missing key in JSON value. keyName: '{keyName}'";
                        return false;
                    case JsonEvent.ArrayEnd:
                        throw new InvalidOperationException($"unexpected event: {ev}");
                }
            }
        }
        
        public void Dispose() {
            idKey.Dispose();
            jsonBytes.Dispose();
            parser.Dispose();
        }
    }
}