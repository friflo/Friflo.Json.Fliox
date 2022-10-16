// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Tree
{
    public readonly struct JsonAstSpan {
        internal  readonly  int         start;
        internal  readonly  int         len;
        
        internal JsonAstSpan (int start) {
            this.start  = start;
            this.len    = -1;
        }
        
        internal JsonAstSpan (int start, int len) {
            this.start  = start;
            this.len    = len;
        }
    }
        

    public struct JsonAst
    {
        private readonly    List<JsonAstNode>   nodes;
        private             byte[]              buf;
        private             int                 pos;
        public              JsonAstNodeDebug[]  DebugNodes => GetDebugNodes();
        
        internal JsonAst(List<JsonAstNode> nodes) {
            this.nodes      = nodes;
            buf             = new byte[32];
            pos             = 0;
            var constants   = JsonAstSerializer.NullTrueFalse;
            Buffer.BlockCopy(constants, 0, buf, 0, constants.Length);
        }
        
        internal void Init() {
            nodes.Clear();
            pos             = 0;
            var constants   = JsonAstSerializer.NullTrueFalse;
            Buffer.BlockCopy(constants, 0, buf, 0, constants.Length);
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
        
        private JsonAstNodeDebug[] GetDebugNodes() {
            var count       = nodes.Count;
            var debugNodes  = new JsonAstNodeDebug[count];
            for (int n = 0; n < count; n++) {
                debugNodes[n] = new JsonAstNodeDebug(nodes[n], buf);
            }
            return debugNodes;
        }
    }
}