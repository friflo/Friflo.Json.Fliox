// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using static Friflo.Json.Burst.JsonEvent;

namespace Friflo.Json.Fliox.Transform.Tree
{
    /// <summary>
    /// Used to merge / patch two given <see cref="JsonValue"/>'s efficiently. <br/>
    /// Implements [RFC 7386: JSON Merge Patch] https://www.rfc-editor.org/rfc/rfc7386 <br/>
    /// <br/>
    /// First it creates two tree representations of both given <see cref="JsonValue"/>'s <br/>
    /// Both trees are then traversed in step to find their differences.
    /// Based on their equalities and differences the resulting JSON value is created.<br/>
    /// Reusing a <see cref="JsonMerger"/> instance typically result in processing without any heap allocations.
    /// </summary>
    public sealed class JsonMerger : IDisposable
    {
        public              string              Error { get; private set; }
        public              bool                WriteNullMembers {
            get => writeNullMembers;
            set => writeNullMembers = astWriter.WriteNullMembers = value;
        }
        public              bool                Pretty {
            get => pretty;
            set => writer.SetPretty(pretty = value);
        }
        private             Utf8JsonParser      parser;
        private             Utf8JsonWriter      writer;
        private             bool                pretty;
        private readonly    JsonAstReader       astReader;
        private readonly    JsonAstWriter       astWriter;
        private             JsonAst             ast;
        private readonly    List<AstMembers>    membersStack;
        private             int                 membersStackIndex;
        private             bool                writeNullMembers;
        
        public JsonMerger() {
            astReader       = new JsonAstReader();
            astWriter       = new JsonAstWriter();
            membersStack    = new List<AstMembers>();
        }
        
        public void Dispose() {
            astReader.Dispose();
            astWriter.Dispose();
            parser.Dispose();
            writer.Dispose();
        }
        
        public JsonValue    Merge (in JsonValue value, in JsonValue patch) {
            if (!MergeInternal(value, patch)) {
                return default;
            }
            return new JsonValue(writer.json.AsArray());
        }
        
        public Bytes        MergeBytes (in JsonValue value, in JsonValue patch) {
            if (!MergeInternal(value, patch)) {
                return default;
            }
            return writer.json;
        }

        private bool MergeInternal (in JsonValue value, in JsonValue patch) {
            membersStackIndex   = 0;
            ast                 = astReader.CreateAst(patch);
            if (ast.Error != null) {
                Error = $"patch value error: {ast.Error}";
                return false;
            }
            astWriter.Init(ast);
            writer.InitSerializer();
            parser.InitParser(value);
            parser.NextEvent();

            Start(0);
            
            if (parser.error.ErrSet) {
                Error = $"merge value error: {parser.error.GetMessage()}";
                return false;
            }
            astWriter.AssertBuffers();
            return true;
        }
        
        private void Start(int index)
        {
            var ev  = parser.Event;
            switch (ev) {
                case ValueNull:     astWriter.WriteRootValue(ref writer);   break;
                case ValueBool:     astWriter.WriteRootValue(ref writer);   break;
                case ValueNumber:   astWriter.WriteRootValue(ref writer);   break;
                case ValueString:   astWriter.WriteRootValue(ref writer);   break;
                case ArrayStart:    astWriter.WriteRootValue(ref writer);   break;
                case ObjectStart:
                    writer.ObjectStart  ();
                    parser.NextEvent();
                    TraverseObject(index);  // descend
                    writer.ObjectEnd    ();
                    return;
                case ObjectEnd:
                case ArrayEnd:
                case EOF:
                default:
                    return;
            }
            parser.NextEvent();
        }

        private void TraverseArray() {
            while (true) {
                var ev = parser.Event;
                switch (ev) {
                    case ValueNull:     writer.ElementNul   ();                         break;
                    case ValueBool:     writer.ElementBln   (parser.boolValue);         break;
                    case ValueNumber:   writer.ElementBytes (parser.value);             break;
                    case ValueString:   writer.ElementStr   (parser.value.AsSpan());    break;
                    case ObjectStart:
                        writer.ObjectStart  ();
                        parser.NextEvent();
                        TraverseObject(-1); // descend
                        writer.ObjectEnd    ();
                        break;
                    case ArrayStart:
                        // no test coverage - at some point its enough :D
                        // addendum: well, I did it anyway :)
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
                        return;
                }
                parser.NextEvent();
            }
        }
        
        private void TraverseObject(int index)
        {
            var members = CreateMembers();
            GetPatchMembers(index, members.items);

            while (true) {
                var ev  = parser.Event;
                switch (ev) {
                    case ValueNull:
                        if (!ReplaceNode (index, members, out _)) {
                            if (writeNullMembers)                   writer.MemberNul  (parser.key.AsSpan());
                        }
                        break;
                    case ValueBool:
                        if (!ReplaceNode (index, members, out _)) { writer.MemberBln  (parser.key.AsSpan(), parser.boolValue); }
                        break;
                    case ValueNumber:
                        if (!ReplaceNode (index, members, out _)) { writer.MemberBytes(parser.key.AsSpan(), parser.value); }
                        break;
                    case ValueString:
                        if (!ReplaceNode (index, members, out _)) { writer.MemberStr  (parser.key.AsSpan(), parser.value.AsSpan()); }
                        break;
                    case ObjectStart:
                        if (ReplaceNode  (index, members, out int member)) {
                            parser.SkipTree();
                        } else {
                            writer.MemberObjectStart(parser.key.AsSpan());
                            parser.NextEvent();
                            TraverseObject (member);// descend
                            writer.ObjectEnd        ();
                        }
                        break;
                    case ArrayStart:
                        if (ReplaceNode (index, members, out _)) {
                            parser.SkipTree();
                        } else {
                            writer.MemberArrayStart (parser.key.AsSpan());
                            parser.NextEvent();
                            TraverseArray();        // descend
                            writer.ArrayEnd         ();
                        }
                        break;
                    case ObjectEnd:
                        WriteNewMembers(members);
                        ReleasePatchMembers();
                        return;
                    case ArrayEnd:
                    case EOF:
                    default:
                        return;
                }
                parser.NextEvent();
            }
        }
        
        private void WriteNewMembers(AstMembers members) {
            foreach (var member in members.items) {
                if (member.found)
                    continue;
                astWriter.WriteObjectMember(member.index, ref writer);
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
        
        private void ReleasePatchMembers() {
            var members = membersStack[--membersStackIndex];
            members.items.Clear();
        }
        
        private void GetPatchMembers (int index, List<AstMember> members) {
            members.Clear();
            if (index == -1) {
                return; // case: only left (original) object available - no counterpart in patch (right) object 
            }
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
            var searchKeySpan   = new Span<byte> (searchKey.buffer, searchKey.start, searchKey.Len);
            var items           = members.items;
            var memberCount     = items.Count;
            for (int n = 0; n < memberCount; n++)
            {
                var astMember   = items[n];
                if (astMember.found)
                    continue;
                var node        = ast.intern.nodes[astMember.index];
                var nodeKey     = new Span<byte> (ast.intern.Buf, node.key.start, node.key.len);
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
    
    internal readonly struct AstMember
    {
        internal readonly   int     index;
        internal readonly   bool    found;
        
        public   override   string  ToString() => $"index: {index} found: {found}";
        
        internal AstMember(int index, bool found) {
            this.index  = index;
            this.found  = found;
        }
    }
}