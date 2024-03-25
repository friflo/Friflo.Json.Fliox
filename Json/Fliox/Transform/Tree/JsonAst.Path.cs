// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
        public bool GetPathValue(string path, out Scalar value) {
            var pathItems = GetPathItems(path.AsSpan());
            return GetPathValue(0, pathItems, out value);
        }
        
        public bool GetPathValue(int objectIndex, in ReadOnlySpan<Utf8Bytes> path, out Scalar value) {
            var index = GetPathNodeIndex(objectIndex, path);
            if (index != -1) {
                value = GetNodeValue(index);
                return true;
            }
            value = Scalar.Null;
            return true;
        }
        
        public Scalar GetNodeValue(int index) {
            var node = intern.nodes[index];
            return GetNodeValue(node);
        }

        private Scalar GetNodeValue(in JsonAstNode node) {
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
                            var dbl = ValueParser.ParseDoubleStd(bytes.AsSpan(), ref error, out _);
                            return new Scalar(dbl);
                        }
                    }
                    var lng = ValueParser.ParseLong(bytes.AsSpan(), ref error, out _);
                    return new Scalar(lng);
                }
                case ArrayStart:
                    return new Scalar(ScalarType.Array, "(array)", node.child);
                case ObjectStart:
                    return new Scalar(ScalarType.Object, "(object)", node.child);
            }
            throw new InvalidOperationException($"invalid node type: {node.type}");
        } 
        
        internal int GetPathNodeIndex(int objectIndex, in ReadOnlySpan<Utf8Bytes> path) {
            if (path.Length == 0) {
                return objectIndex;
            }
            var nodes       = intern.nodes;
            var node        = nodes[objectIndex];
            var itemCount   = path.Length;
            int pathPos     = 0;
            var buf         = intern.Buf;
            int childIndex  = -1;
            
            for (; pathPos < itemCount; pathPos++) {
                if (node.type != ObjectStart) {
                    return -1;
                }
                childIndex = node.child;
                while (childIndex != -1) {
                    var childNode   = nodes[childIndex];
                    var keyName     = new Utf8Bytes(buf, childNode.key.start, childNode.key.len);
                    if (keyName.IsEqual(path[pathPos])) {
                        node = childNode;
                        break;
                    }
                    childIndex = childNode.next;
                }
                if (childIndex == -1) {
                    return -1;
                }
            }
            return pathPos == itemCount ? childIndex : -1;
        }
        
        public static Utf8Bytes[] GetPathItems(in ReadOnlySpan<char> path) {
            if (path.Length == 0) {
                return Array.Empty<Utf8Bytes>();
            }
            var utf8Path    = new Utf8Bytes(path);
            var pathSpan    = utf8Path.ReadOnlySpan;
            var len         = pathSpan.Length;
            int count       = 1;
            for (int n = 0; n < len; n++) {
                count += pathSpan[n] == '.' ? 1 : 0;
            }
            int itemLen;
            var itemStart   = 0;
            var pathItems   = new Utf8Bytes[count];
            count = 0;
            for (int n = 0; n < len; n++) {
                if (pathSpan[n] != '.')
                    continue;
                itemLen = n - itemStart;
                if (itemLen == 0) throw new InvalidOperationException("Invalid path: " + path.ToString());
                pathItems[count++] = new Utf8Bytes(utf8Path, itemStart, itemLen);
                itemStart = n + 1;
            }
            itemLen = len - itemStart;
            if (itemLen == 0) throw new InvalidOperationException("Invalid path: " + path.ToString());
            pathItems[count] = new Utf8Bytes(utf8Path, itemStart, itemLen);
            return pathItems;
        } 
    }
}