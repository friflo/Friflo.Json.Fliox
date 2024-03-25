// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Utils
{
    internal enum ProcessingType {
        Validate    = 1,
        GetKey      = 2,
        SetKey      = 3
    }
    
    /// <summary>
    /// Is used to ensure that <see cref="ReadEntitiesResult"/> returned by <see cref="EntityContainer.ReadEntitiesAsync"/>
    /// contains valid <see cref="ReadEntitiesResult.entities"/>.
    /// Validation is required for <see cref="FlioxHub"/> implementations which cannot ensure that the value of
    /// its key/values are JSON. See <see cref="ReadEntitiesResult.ValidateEntities"/>.
    /// </summary>
    public sealed class EntityProcessor : IDisposable
    {
        private             Utf8JsonParser          parser;
        private             Bytes                   idKey       = new Bytes(16);
        private             Bytes                   defaultKey  = new Bytes("id");
        private             bool                    foundKey;
        //                  --- ReplaceKey
        private             Utf8JsonParser.State    keyState;
        private             int                     keyStart;
        private             int                     keyEnd;
        private             bool                    foundIntKey;
        private             bool                    asIntKey;
        private             Bytes                   sb          = new Bytes(0);
        
        
        private void SetKey(ref Bytes dst, string value) {
            dst.Clear();
            if (value == null) {
                dst.AppendBytes(defaultKey);
            } else {
                dst.AppendStringUtf8(value);   
            }
        }
        
        public bool GetEntityKey(in JsonValue json, string keyName, out JsonKey keyValue, out string error) {
            return Traverse(json, keyName, out keyValue, ProcessingType.GetKey,   out error);
        }
        
        public bool Validate(in JsonValue json, string keyName, out JsonKey keyValue, out string error) {
            return Traverse(json, keyName, out keyValue, ProcessingType.Validate, out error);
        }
        
        public JsonValue ReplaceKey(in JsonValue json, string keyName, bool asIntKey, string newKeyName, out JsonKey keyValue, out string error) {
            this.asIntKey   = asIntKey;
            if (!Traverse  (json, keyName, out keyValue, ProcessingType.SetKey,   out error)) {
                return new JsonValue();
            }
            SetKey(ref idKey, newKeyName);
            keyName         = keyName       ?? "id";
            newKeyName      = newKeyName    ?? "id";
            bool equalKeys  = keyName == newKeyName;
            if (equalKeys && foundIntKey == asIntKey)
                return json;
            sb.Clear();
            sb.AppendArray(json, 0, keyStart);
            if (keyState == Utf8JsonParser.State.ExpectMember) {
                sb.AppendChar(',');
            }
            sb.AppendChar('\"');
            sb.AppendBytes(idKey);
            sb.AppendString("\":");
            if (asIntKey) {
                keyValue.AppendTo(ref sb, ref parser.format);
            } else {
                sb.AppendChar('\"');
                keyValue.AppendTo(ref sb, ref parser.format);
                sb.AppendChar('\"');
            }
            var remaining = json.Count - keyEnd; 
            sb.AppendArray(json, json.start + keyEnd, remaining);
            var result = sb.AsArray();
            sb.Clear();
            return new JsonValue(result);
        }

        private bool Traverse (in JsonValue json, string keyName, out JsonKey keyValue, ProcessingType processingType, out string error) {
            foundKey = false;
            SetKey(ref idKey, keyName);
            keyValue = new JsonKey();
            parser.InitParser(json);
            var ev = parser.NextEvent();
            if (ev != JsonEvent.ObjectStart) {
                error   = $"entity value must be an object.";
                return false;
            }
            while (true) {
                var pos     = parser.Position;
                var state   = parser.CurrentState;
                ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                        if (!parser.key.IsEqual(idKey))
                            break;
                        foundKey = true;
                        if (ev == JsonEvent.ValueNumber && !parser.isFloat) {
                            ValueParser.ParseLong(parser.value.AsSpan(), ref sb, out bool success);
                            if (!success) {
                                error = "invalid integer key: " + sb.AsString();
                                return false;
                            }
                        }
                        keyValue = new JsonKey(parser.value, default);
                        switch (processingType) {
                            case ProcessingType.Validate:
                                continue;
                            case ProcessingType.GetKey:
                                error       = null;
                                return true;
                            case ProcessingType.SetKey:
                                keyState    = state;
                                keyStart    = pos;
                                keyEnd      = parser.Position;
                                foundIntKey = ev == JsonEvent.ValueNumber;
                                if (asIntKey  && ev == JsonEvent.ValueString)
                                    continue;
                                if (!asIntKey && ev == JsonEvent.ValueNumber) {
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
        
        /// <summary>
        /// Parse the given <paramref name="json"/> while expecting the value is a JSON array of objects (entities).
        /// The JSON objects returned by this method are 1:1 byte identical to its JSON input
        /// to preserve line feeds and white spaces.
        /// <br/>
        /// The common approach using an <see cref="ObjectReader"/> with a List of JsonValue's is not used
        /// as it does not preserve white spaces.   
        /// <br/>
        /// Note: The method is independent from <see cref="Traverse"/> related methods.
        /// It is placed here as it shares all required parser related properties and
        /// its purpose is also related to parsing entities.
        /// </summary>
        public List<JsonEntity> ReadJsonArray(in JsonValue json, out string error) {
            parser.InitParser(json);
            var ev = parser.NextEvent();
            if (ev != JsonEvent.ArrayStart) {
                error   = $"expect JSON array";
                return null;
            }
            var srcArray    = json.AsReadOnlySpan();
            var array       = new List<JsonEntity>();
            while (true) {
                ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ValueNull:
                    case JsonEvent.ArrayStart:
                        error   = $"expect only objects in JSON array";
                        return null;
                    case JsonEvent.ObjectStart:
                        var objStart = parser.Position - 1;
                        parser.SkipTree();
                        if (parser.error.ErrSet) {
                            error = parser.error.msg.AsString();
                            return null;
                        }
                        var end     = parser.Position;
                        var len     = end - objStart; 
                        var payload = new byte[len];
                        for (int n = 0; n < len; n++) {
                            payload[n] = srcArray[objStart + n];
                        }
                        var element = new JsonEntity(new JsonValue(payload));
                        array.Add(element);
                        break;
                    case JsonEvent.ArrayEnd:
                        error = null;
                        return array;
                    case JsonEvent.Error:
                        error = parser.error.msg.AsString();
                        return null;
                    case JsonEvent.ObjectEnd:
                        throw new InvalidOperationException($"unexpected event: {ev}");
                }
            }
        }
        
        public void Dispose() {
            defaultKey.Dispose();
            idKey.Dispose();
            parser.Dispose();
            sb.Dispose();
        }
    }
}