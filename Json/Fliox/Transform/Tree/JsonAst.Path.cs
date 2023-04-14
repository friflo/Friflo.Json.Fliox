// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using static Friflo.Json.Burst.JsonEvent;

// ReSharper disable SuggestBaseTypeForParameter
namespace Friflo.Json.Fliox.Transform.Tree
{
    public partial class JsonAst
    {
        public bool GetPathScalar(string path, out Scalar value) {
            if (path.Length == 0) {
                value = NodeToScalar(intern.nodes[0]);
                return true;
            }
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
                case ValueNull:
                    return Scalar.Null;
                case ValueBool: {
                    var isTrue      = intern.Buf[node.value.start] == (byte)'t'; // true
                    return new Scalar(isTrue);
                }
                case ValueString: {
                    var nodeValue   = new JsonValue(intern.Buf, node.value.start, node.value.len);
                    return          new Scalar(nodeValue.AsString());
                }
                case ValueNumber: {
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
                case ArrayStart:
                    return new Scalar(ScalarType.Array, "(array)", node.child);
                case ObjectStart:
                    return new Scalar(ScalarType.Object, "(object)", node.child);
            }
            throw new InvalidOperationException($"invalid node type: {node.type}");
        } 
        
        private bool GetPathNode(ReadOnlySpan<JsonValue> pathItems, out JsonAstNode node) {
            var nodes       = intern.nodes;
            node            = nodes[0];
            var itemCount   = pathItems.Length;
            int pathPos     = 0;
            for (; pathPos < itemCount; pathPos++) {
                if (node.type != ObjectStart) {
                    return false;
                }
                var childIndex = node.child;
                while (childIndex != -1) {
                    var childNode   = nodes[childIndex];
                    switch (childNode.type) {
                        case ValueString:
                        case ValueNumber:
                        case ValueBool:
                        case ArrayStart:
                        case ObjectStart:
                        case ValueNull:
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
        
        private static JsonValue[] GetPathItems(string path) {
            var utf8Path    = new JsonValue(path);  // todo optimize - avoid allocation
            var pathSpan    = utf8Path.MutableArray;
            var len         = pathSpan.Length;
            int count       = 1;
            for (int n = 0; n < len; n++) {
                count += pathSpan[n] == '.' ? 1 : 0;
            }
            int itemLen;
            var itemStart   = 0;
            var pathItems   = new JsonValue[count];
            count = 0;
            for (int n = 0; n < len; n++) {
                if (pathSpan[n] != '.')
                    continue;
                itemLen = n - itemStart;
                if (itemLen == 0) throw new InvalidOperationException($"Invalid path: {path}");
                pathItems[count++] = new JsonValue(pathSpan, itemStart, itemLen);
                itemStart = n + 1;
            }
            itemLen = len - itemStart;
            if (itemLen == 0) throw new InvalidOperationException($"Invalid path: {path}");
            pathItems[count] = new JsonValue(pathSpan, itemStart, itemLen);
            return pathItems;
        } 
    }
}