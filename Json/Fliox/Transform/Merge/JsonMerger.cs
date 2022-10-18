// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Transform.Tree;
using static Friflo.Json.Burst.JsonEvent;

namespace Friflo.Json.Fliox.Transform.Merge
{
    public class JsonMerger : IDisposable
    {
        private             Utf8JsonParser      parser;
        private             Utf8JsonWriter      writer;
        private             Bytes               json            = new Bytes(128);
        private readonly    JsonAstReader       astReader       = new JsonAstReader();
        private readonly    JsonAstWriter       astWriter       = new JsonAstWriter();
        private             JsonAst             ast;
        private readonly    List<AstMembers>    membersStack    = new List<AstMembers>();
        private             int                 membersStackIndex;
        
        // ReSharper disable once EmptyConstructor
        public JsonMerger() { }
        
        public void Dispose() {
            astReader.Dispose();
            astWriter.Dispose();
            json.Dispose();
            parser.Dispose();
            writer.Dispose();
        }
        
        public JsonValue    Merge (JsonValue value, JsonValue patch) {
            MergeInternal(value, patch);
            return new JsonValue(writer.json.AsArray());
        }
        
        public Bytes        MergeBytes (JsonValue value, JsonValue patch) {
            MergeInternal(value, patch);
            return writer.json;
        }

        private void MergeInternal (JsonValue value, JsonValue patch) {
            membersStackIndex   = 0;
            ast                 = astReader.CreateAst(patch);
            astWriter.Init(ast);
            writer.InitSerializer();
            writer.SetPretty(false);
            json.Clear();
            json.AppendArray(value);
            parser.InitParser(json);
            parser.NextEvent();

            TraverseValue(0);
        }
        
        private void TraverseValue(int index)
        {
            var ev  = parser.Event;
            switch (ev) {
                case ValueNull:     writer.ElementNul   ();                 break;
                case ValueBool:     writer.ElementBln   (parser.boolValue); break;
                case ValueNumber:   writer.ElementBytes (ref parser.value); break;
                case ValueString:   writer.ElementStr   (parser.value);     break;
                case ObjectStart:
                    writer.ObjectStart  ();
                    parser.NextEvent();
                    TraverseObject(index);  // descend
                    writer.ObjectEnd    ();
                    return;
                case ArrayStart:
                    writer.ArrayStart   (false);
                    parser.NextEvent();
                    TraverseArray();        // descend
                    writer.ArrayEnd     ();
                    return;
                case ObjectEnd:
                case ArrayEnd:
                case EOF:
                default:
                    throw new InvalidOperationException($"unexpected state: {ev}");
            }
            parser.NextEvent();
        }

        private void TraverseArray() {
            while (true) {
                var ev = parser.Event;
                switch (ev) {
                    case ValueNull:     writer.ElementNul   ();                 break;
                    case ValueBool:     writer.ElementBln   (parser.boolValue); break;
                    case ValueNumber:   writer.ElementBytes (ref parser.value); break;
                    case ValueString:   writer.ElementStr   (parser.value);     break;
                    case ObjectStart:
                        writer.ObjectStart  ();
                        parser.NextEvent();
                        TraverseObject(-1); // descend
                        writer.ObjectEnd    ();
                        break;
                    case ArrayStart:
                        writer.ArrayStart   (false);
                        parser.NextEvent();
                        TraverseArray();    // descend
                        writer.ArrayEnd     ();
                        break;
                    case ArrayEnd:
                        return;
                    case ObjectEnd:
                    case EOF:
                    default:
                        throw new InvalidOperationException($"unexpected state: {ev}");
                }
                parser.NextEvent();
            }
        }
        
        private void TraverseObject(int index)
        {
            var members = CreateMembers();
            GetMembers(index, members.items);

            while (true) {
                var ev  = parser.Event;
                switch (ev) {
                    case ValueNull:
                        if (!ReplaceNode (index, members, out _)) { writer.MemberNul  (parser.key); }
                        break;
                    case ValueBool:
                        if (!ReplaceNode (index, members, out _)) { writer.MemberBln  (parser.key, parser.boolValue); }
                        break;
                    case ValueNumber:
                        if (!ReplaceNode (index, members, out _)) { writer.MemberBytes(parser.key, ref parser.value); }
                        break;
                    case ValueString:
                        if (!ReplaceNode (index, members, out _)) { writer.MemberStr  (parser.key, parser.value); }
                        break;
                    case ObjectStart:
                        if (ReplaceNode  (index, members, out int member)) {
                            parser.SkipTree();
                        } else {
                            writer.MemberObjectStart(parser.key);
                            parser.NextEvent();
                            TraverseObject (member);// descend
                            writer.ObjectEnd        ();
                        }
                        break;
                    case ArrayStart:
                        if (ReplaceNode (index, members, out _)) {
                            parser.SkipTree();
                        } else {
                            writer.MemberArrayStart (parser.key);
                            parser.NextEvent();
                            TraverseArray();        // descend
                            writer.ArrayEnd         ();
                        }
                        break;
                    case ObjectEnd:
                        ReleaseMembers();
                        return;
                    case ArrayEnd:
                    case EOF:
                    default:
                        throw new InvalidOperationException($"unexpected state: {ev}");
                }
                parser.NextEvent();
            }
        }
        
        private AstMembers CreateMembers() {
            if (membersStackIndex < membersStack.Count) {
                return membersStack[membersStackIndex++];
            }
            membersStackIndex++;
            var members = new AstMembers(new List<AstMember>());
            membersStack.Add(members);
            return members;
        }
        
        private void ReleaseMembers() {
            var members = membersStack[--membersStackIndex];
            members.items.Clear();
        }
        
        private void GetMembers (int index, List<AstMember> members) {
            members.Clear();
            var child = ast.intern.nodes[index].child;
            while (child != -1) {
                members.Add(new AstMember(child, false));
                child = ast.intern.nodes[child].next;
            }
        }
        
        private bool ReplaceNode (int index, AstMembers members, out int member)
        {
            if (index == -1) {
                member = -1;
                return false;
            }
            ref var searchKey   = ref parser.key;
            var searchKeyLen    = searchKey.Len;
            var searchKeySpan   = new Span<byte> (searchKey.buffer.array, searchKey.start, searchKeyLen);
            var items           = members.items;
            var memberCount     = items.Count;
            for (int n = 0; n < memberCount; n++)
            {
                var astMember       = items[n];
                if (astMember.found)
                    continue;
                var node            = ast.intern.nodes[astMember.index];
                if (searchKeyLen != node.key.len)
                    continue;
                var nodeKey = new Span<byte> (ast.intern.Buf, node.key.start, node.key.len);
                if (!searchKeySpan.SequenceEqual(nodeKey))
                    continue;
                // --- found node member
                member              = astMember.index;
                members.items[n]    = new AstMember (member, true);
                astWriter.WriteObjectMember(member, ref writer);
                return true;
            }
            member = -1;
            return false;
        }
    }
    
    internal readonly struct AstMembers
    {
        internal readonly   List<AstMember> items;
        public   override   string          ToString() => $"{items.Count}";
        
        internal AstMembers(List<AstMember> items) {
            this.items = items;
        }
    }
    
    internal readonly struct AstMember {
        internal readonly   int        index;
        internal readonly   bool       found;
        
        internal AstMember(int index, bool found) {
            this.index  = index;
            this.found  = found;
        }
        
        public   override   string          ToString() => $"index: {index} found: {found}";
    }
}