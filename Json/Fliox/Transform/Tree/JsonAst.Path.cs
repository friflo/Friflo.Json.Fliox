// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;

namespace Friflo.Json.Fliox.Transform.Tree
{
    public partial class JsonAst
    {
        public bool GetPathScalar(string path, out Scalar value) {
            var pathItems = GetPathItems(path);
            if (GetPathNode(pathItems, out var node)) {
                value = NodeToScalar(node);
                return true;
            }
            value = default;
            return false;
        }
        
        public Scalar GetNodeScalar(int index) {
            var node = intern.nodes[index];
            return NodeToScalar(node);
        }

        private Scalar NodeToScalar(in JsonAstNode node) {
            switch (node.type) {
                case JsonEvent.ValueNull:
                    return Scalar.Null;
                case JsonEvent.ValueBool: {
                    var isTrue      = intern.Buf[node.value.start] == (byte)'t'; // true
                    return new Scalar(isTrue);
                }
                case JsonEvent.ValueString: {
                    var nodeValue   = new JsonValue(intern.Buf, node.value.start, node.value.len);
                    return          new Scalar(nodeValue.AsString());
                }
                case JsonEvent.ValueNumber: {
                    var b       = intern.Buf;
                    var start   = node.value.start;
                    var end     = start + node.value.len;
                    var bytes   = new Bytes { buffer = b, start = start, end = end };
                    var error   = new Bytes();
                    for (int n = start; n < end; n++) {
                        if (b[n] == '.') {
                            var dbl = ValueParser.ParseDoubleStd(ref bytes, ref error, out _);
                            return new Scalar(dbl);
                        }
                    }
                    var lng = ValueParser.ParseLong(ref bytes, ref error, out _);
                    return new Scalar(lng);
                }
                case JsonEvent.ArrayStart:
                    return new Scalar(ScalarType.Array, "(array)", node.child);
                case JsonEvent.ObjectStart:
                    return new Scalar(ScalarType.Object, "(object)", node.child);
            }
            throw new InvalidOperationException($"invalid node type: {node.type}");
        } 
        
        public bool GetPathNode(List<JsonValue> pathItems, out JsonAstNode node) {
            var nodes       = intern.nodes;
            node            = nodes[0];
            var itemCount   = pathItems.Count;
            int pathPos     = 0;
            for (; pathPos < itemCount; pathPos++) {
                if (node.type != JsonEvent.ObjectStart) {
                    return false;
                }
                var childIndex = node.child;
                while (childIndex != -1) {
                    var childNode   = nodes[childIndex];
                    switch (childNode.type) {
                        case JsonEvent.ArrayStart:
                        case JsonEvent.ObjectStart:
                        case JsonEvent.ValueNull:
                        case JsonEvent.ValueBool:
                        case JsonEvent.ValueNumber:
                        case JsonEvent.ValueString:
                            break;
                        default:
                            return false;
                    }
                    var key = new JsonValue(intern.Buf, childNode.key.start, childNode.key.len);
                    if (key.IsEqual(pathItems[pathPos])) {
                        node = childNode;
                        break;
                    }
                    childIndex = childNode.next;
                }
                if (childIndex == -1) {
                    return false;
                }
            }
            return pathPos == itemCount;
        }
        
        private static List<JsonValue> GetPathItems(string path) {
            var utf8Path    = new JsonValue(path);
            var pathSpan    = utf8Path.MutableArray;
            var len         = pathSpan.Length;
            var pathItems   = new List<JsonValue>();
            var itemStart   = 0;
            int itemLen;
            for (int n = 0; n < len; n++) {
                if (pathSpan[n] != '.') {
                    continue;
                }
                itemLen = n - itemStart;
                if (itemLen == 0) throw new InvalidOperationException($"Invalid path: {path}");
                var pathItem = new JsonValue(pathSpan, itemStart, itemLen);
                pathItems.Add(pathItem);
                itemStart = n + 1;
            }
            itemLen = len - itemStart;
            if (itemLen == 0) throw new InvalidOperationException($"Invalid path: {path}");
            var lastItem = new JsonValue(pathSpan, itemStart, itemLen);
            pathItems.Add(lastItem);
            return pathItems;
        } 
    }
}