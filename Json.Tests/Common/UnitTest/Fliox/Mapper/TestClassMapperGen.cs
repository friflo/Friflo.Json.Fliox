// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    internal struct GenChild
    {
        public int val;
    }
        
    internal class GenClass
    {
        public int          intVal0;
        public int          intVal1;
        public GenChild     child;
        public GenChild?    child2;
    }
    
    public static class TestClassMapperGen
    {
        [Test]
        public static void TestGeneratorClass() {
            var genClass    = new GenClass { intVal0 = 11, intVal1 = 12, child = new GenChild { val = 22 }};
            var typeStore   = new TypeStore();
            var mapper      = new ObjectMapper(typeStore);
            mapper.WriteNullMembers = false;

            var json = mapper.WriteAsValue(genClass);
            
            AreEqual(@"{""intVal0"":11,""intVal1"":12,""child"":{""val"":22}}", json.AsString());
            
            var dest    = new GenClass();
            mapper.ReadTo(json, dest, false);
            
            AreEqual(11, dest.intVal0);
            AreEqual(12, dest.intVal1);
            AreEqual(22, dest.child.val);
            
            var start = Mem.GetAllocatedBytes();
            for (int n = 0; n < 10; n++) {
                mapper.writer.WriteAsBytes(genClass);
            }
            for (int n = 0; n < 10; n++) {
                mapper.ReadTo(json, dest, false);
            }
            var diff = Mem.GetAllocationDiff(start);
            Mem.NoAlloc(diff);
        }
        
        //[Test]
        public static void TestGeneratorClass2() {
            var genClass    = new GenClass { intVal0 = 11, intVal1 = 12 };
            JsonSerializer.Serialize(genClass);
            
            for (int n = 0; n < 10; n++) {
                JsonSerializer.Serialize(genClass);
            }
        }
        
#if !UNITY_5_3_OR_NEWER
        [Test]
        public static void TestSystemTextJson() {
            var genClass    = new GenClass { intVal0 = 11, intVal1 = 12, child = new GenChild { val = 22 } };
            var options     = new System.Text.Json.JsonSerializerOptions {
                IncludeFields           = true,
                DefaultIgnoreCondition  = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            var json = System.Text.Json.JsonSerializer.Serialize(genClass, options);
            AreEqual(@"{""intVal0"":11,""intVal1"":12,""child"":{""val"":22}}", json);
            
            for (int n = 0; n < 10; n++) {
                System.Text.Json.JsonSerializer.Serialize(genClass, options);
            }
        }
        
        /* public static void TestSystemTextJson2() {
            var genClass        = new GenClass { intVal0 = 11, intVal1 = 12 };
            
            var bufferWriter    = new ArrayBufferWriter<byte>();
            for (int n = 0; n < 1; n++) {
                bufferWriter.Clear();
                var utf8Writer      = new Utf8JsonWriter (bufferWriter);
                JsonSerializer.Serialize(utf8Writer, genClass, new System.Text.Json.JsonSerializerOptions {IncludeFields = true});
            }
        } */
#endif
    }
}