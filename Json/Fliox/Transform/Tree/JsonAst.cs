// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Tree
{
    /// <summary>
    /// Representation of a <see cref="JsonValue"/> as a tree of <see cref="JsonAstNode"/>'s.<br/>
    /// The <see cref="JsonAstNode"/>'s are enumerated and can be accessed by index using <see cref="Nodes"/>. Root node has index [0]<br/>
    /// Its <see cref="Nodes"/> are reused to avoid heap allocations when creating trees from multiple JSON values.<br/>
    /// <br/>
    /// A <see cref="JsonAst"/> enables iteration of JSON object members without reading the entire JSON value. <br/>
    /// <br/>
    /// Two <see cref="JsonAst"/> instances are used by <see cref="JsonMerger"/> for efficient patching
    /// of a given JSON value with a second JSON patch value. 
    /// </summary>
    /// <remarks>
    /// JSON example of the indices used at <see cref="Nodes"/> 
    /// <code>
    /// {                   // 0 - root
    ///     "a": 1          // 1
    ///     "b": [          // 2
    ///         null        // 3
    ///         42          // 4
    ///     ],
    ///     "c": {          // 5
    ///         "c1": true  // 6
    ///     }
    /// } 
    /// </code>
    /// </remarks>
    public sealed partial class JsonAst
    {
        internal    JsonAstIntern       intern; // ast state / result
        // --- public API
        public      string              Error                               => intern.error;
        public      int                 NodesCount                          => intern.nodesCount;
        public      JsonAstNode[]       Nodes                               => intern.nodes;
        public      JsonAstNode         GetNode(int index)                  => intern.nodes[index];
        /// <summary> used to return <see cref="JsonAstNode.key"/> and <see cref="JsonAstNode.value"/> as string.</summary>
        public      string              GetSpanString(in JsonAstSpan span)  => Encoding.UTF8.GetString(intern.Buf, span.start, span.len);
        // --- debug helper
        public      JsonAstNodeDebug[]  DebugNodes                          => intern.GetDebugNodes();
        public      JsonAstNodeDebug    DebugNode(JsonAstNode node)         => intern.DebugNode(node);
    }

    /// Is struct to enhance performance when traversing with <see cref="JsonAstReader"/>
    internal struct JsonAstIntern
    {
        internal    string              error;
        internal    int                 nodesCount;
        private     int                 nodesCapacity;
        internal    JsonAstNode[]       nodes;
        private     byte[]              buf;
        private     int                 bufLength;
        private     int                 pos;
        
        internal    JsonAstNodeDebug[]  DebugNodes  => GetDebugNodes();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]        
        internal    byte[]              Buf         => buf;

        internal JsonAstIntern(int capacity) {
            error           = null;
            nodesCount      = 0;
            nodesCapacity   = capacity;
            nodes           = new JsonAstNode[nodesCapacity];
            bufLength       = 32;
            buf             = new byte[bufLength + Bytes.CopyRemainder];
            pos             = -1;
        }
        
        internal void Init() {
            error           = null;
            nodesCount      = 0;
            var constants   = JsonAstReader.NullTrueFalse;
            pos             = constants.Length;
            Buffer.BlockCopy(constants, 0, buf, 0, pos);
        }
        
        internal void AddNode(JsonEvent ev, in JsonAstSpan key, in JsonAstSpan value) {
            nodes[nodesCount] = new JsonAstNode(ev, key, value, -1, -1);
            if (++nodesCount < nodesCapacity) {
                return;
            }
            ExtendCapacity();
        }
        
        internal void AddContainerNode(JsonEvent ev, in JsonAstSpan key, int child) {
            nodes[nodesCount] = new JsonAstNode(ev, key, default, child, -1);
            if (++nodesCount < nodesCapacity) {
                return;
            }
            ExtendCapacity();
        }
        
        private void ExtendCapacity() {
            nodesCapacity = 2 * nodesCapacity;
            var newNodes = new JsonAstNode[nodesCapacity];
            for (int n = 0; n < nodesCount; n++) {
                newNodes[n] = nodes[n];
            }
            nodes = newNodes;
        }

        internal void SetNodeNext(int index, int next) {
            nodes[index].next = next;
        }
        
        internal JsonAstSpan AddSpan (in Bytes bytes) {
            var len     = bytes.Len;
            int destPos = Reserve(len);
            Buffer.BlockCopy(bytes.buffer, bytes.start, buf, destPos, len);
            return new JsonAstSpan(destPos, len);
        }
        
        private int Reserve (int len) {
            int curPos  = pos;
            int newLen  = curPos + len;
            if (curPos + len > bufLength) {
                var doubledLen = 2 * bufLength;
                if (newLen < doubledLen) {
                    newLen = doubledLen;
                }
                var newBuffer = new byte [newLen + Bytes.CopyRemainder];
                Buffer.BlockCopy(buf, 0, newBuffer, 0, curPos);
                buf         = newBuffer;
                bufLength   = newLen;
            }
            pos += len;
            return curPos;
        }
        
        public JsonAstNodeDebug[] GetDebugNodes() {
            var count       = nodesCount;
            var debugNodes  = new JsonAstNodeDebug[count];
            for (int n = 0; n < count; n++) {
                debugNodes[n] = new JsonAstNodeDebug(nodes[n], buf);
            }
            return debugNodes;
        }
        
        public JsonAstNodeDebug DebugNode(JsonAstNode node) {
            return new JsonAstNodeDebug(node, buf);
        }
    }
}