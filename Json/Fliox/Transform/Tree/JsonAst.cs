// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Tree
{
    public class JsonAst
    {
        public      int                 NodesCount  => intern.nodesCount;
        public      JsonAstNode[]       Nodes       => intern.nodes;
        public      JsonAstNodeDebug[]  DebugNodes  => intern.GetDebugNodes();
        
        public      JsonAstNodeDebug    DebugNode(JsonAstNode node) => intern.DebugNode(node);
        
        internal    JsonAstIntern       intern;
        
        public JsonAstNode GetNode(int index) {
            return intern.nodes[index];
        }
        
        /// <summary> used to return <see cref="JsonAstNode.key"/> and <see cref="JsonAstNode.value"/> as string.</summary>
        public string GetSpanString(in JsonAstSpan span) {
            return Encoding.UTF8.GetString(intern.Buf, span.start, span.len);
        }
    }

    /// Is struct to enhance performance when traversing with <see cref="JsonAstReader"/>
    internal struct JsonAstIntern
    {
        internal    int                 nodesCount;
        private     int                 nodesCapacity;
        internal    JsonAstNode[]       nodes;
        private     byte[]              buf;
        private     int                 pos;
        
        internal    JsonAstNodeDebug[]  DebugNodes  => GetDebugNodes();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]        
        internal    byte[]              Buf         => buf;

        internal JsonAstIntern(int capacity) {
            nodesCount      = 0;
            nodesCapacity   = capacity;
            nodes           = new JsonAstNode[nodesCapacity];
            buf             = new byte[32];
            pos             = -1;
        }
        
        internal void Init() {
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
            Buffer.BlockCopy(bytes.buffer.array, bytes.start, buf, destPos, len);
            return new JsonAstSpan(destPos, len);
        }
        
        private int Reserve (int len) {
            int curPos  = pos;
            int newLen  = curPos + len;
            int bufLen  = buf.Length;
            if (curPos + len > bufLen) {
                var doubledLen = 2 * bufLen;
                if (newLen < doubledLen) {
                    newLen = doubledLen;
                }
                var newBuffer = new byte [newLen];
                Buffer.BlockCopy(buf, 0, newBuffer, 0, curPos);
                buf = newBuffer;
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