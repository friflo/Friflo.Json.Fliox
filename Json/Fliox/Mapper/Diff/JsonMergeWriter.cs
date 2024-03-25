// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map;
using static Friflo.Json.Fliox.Mapper.Diff.DiffType;

namespace Friflo.Json.Fliox.Mapper.Diff
{
    /// <summary>
    /// Create the a JSON patch value by the given <see cref="DiffNode"/>. <br/>
    /// The JSON patch result is intended to be merged (assigned) into a given object
    /// by using <see cref="ObjectReader.ReadTo{T}(Friflo.Json.Burst.Bytes,T,bool)"/>
    /// </summary>
    public sealed class JsonMergeWriter : IDisposable
    {
        public      bool    Pretty { get => writer.pretty; set => writer.pretty = value; }

        private     Writer  writer;

        public JsonMergeWriter(TypeStore typeStore) {
            writer = new Writer(typeStore);
        }
        
        public void Dispose() {
            writer.Dispose();
        }
        
        private void Init() {
            writer.bytes.Clear();
            writer.level            = 0;
            writer.writeNullMembers = true;
        }
        
        public JsonValue WriteMergePatch (DiffNode diffNode) {
            Init();
            
            Traverse(ref writer, diffNode, true);
            
            return new JsonValue(writer.bytes.AsArray());
        }
        
        public Bytes WriteMergePatchBytes (DiffNode diffNode) {
            Init();
            
            Traverse(ref writer, diffNode, true);
            
            return writer.bytes;
        }
        
        public JsonValue WriteEntityMergePatch<T> (DiffNode diffNode, T entity) where T : class {
            Init();
            // TypeMapper could be passed as parameter to avoid lookup
            var  mapper         = writer.typeCache.GetTypeMapper(typeof(T));
            bool firstMember    = true;
            mapper.WriteEntityKey(ref writer, entity, ref firstMember);
            
            Traverse(ref writer, diffNode, firstMember);
            
            return new JsonValue(writer.bytes.AsArray());
        }
        
        private static void Traverse(ref Writer writer, DiffNode diffNode, bool firstMember) {
            int     pairCount   = 0;
            foreach (var child in diffNode.children) {
                var key         = child.NodeKey;
                if (key is PropField field) {
                    var diffType    = child.DiffType;
                    switch (diffType) {
                        case OnlyRight:
                        case NotEqual:
                            pairCount++;
                            writer.WriteFieldKey (field, ref firstMember);
                            ref var right   = ref child.valueRight;
                            if (right.IsNull) {
                                writer.AppendNull();
                            } else {
                                var mapper      = child.NodeMapper;
                                mapper.WriteVar(ref writer, right);
                            }
                            continue;
                        case None:
                            pairCount++;
                            writer.WriteFieldKey (field, ref firstMember);
                            Traverse(ref writer, child, true);
                            continue;
                    }
                    continue;
                }
                // --- is Dictionary
                WriteDictionaryKeyValue(ref writer, child, ref firstMember, ref pairCount);
            }
            writer.WriteObjectEnd(firstMember);
        }
        
        private static void WriteDictionaryKeyValue(
            ref Writer      writer,
                DiffNode    child,
            ref bool        firstMember,
            ref int         pairCount)
        {
            var diffType    = child.DiffType;
            var key         = child.NodeKey;
            switch (diffType) {
                case OnlyRight:
                case NotEqual:
                    writer.bytes.AppendChar('{');
                    firstMember     = false;
                    var mapper      = child.NodeMapper;
                    mapper.WriteKey (ref writer, key, pairCount++);
                    ref var right   = ref child.valueRight;
                    if (right.IsNull) {
                        writer.AppendNull();
                    } else {
                        var valueMapper = mapper.GetElementMapper();
                        valueMapper.WriteVar(ref writer, right);
                    }
                    return;
                case None:
                    writer.bytes.AppendChar('{');
                    firstMember     = false;
                    mapper          = child.NodeMapper;
                    mapper.WriteKey (ref writer, key, pairCount++);
                    Traverse(ref writer, child, true);
                    return;
            }
        } 
    }
}