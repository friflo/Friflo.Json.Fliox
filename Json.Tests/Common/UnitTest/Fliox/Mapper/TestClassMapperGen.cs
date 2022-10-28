// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    public static class TestClassMapperGen
    {
        partial class GenChild
        {
            public int val;
        }
        
        partial class GenClass
        {
            public int      intVal0;
            public int      intVal1;
            public GenChild child;
        }
        
        partial class GenChild {
           
            private static void Gen_Write(GenChild obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
                writer.Write("val", fields[0], obj.val, ref firstMember);
            }
            
            private static bool  Gen_ReadField (GenChild obj, PropField field, ref Reader reader) {
                switch (field.fieldIndex) {
                    case 0: return reader.Read("val", field, ref obj.val);
                }
                return false;
            }
        }
            
        partial class GenClass {
            // delegate void WriteDelegate<in T>(T obj, PropField[] fields, ref Writer writer, ref bool firstMember);
            
            private static void Gen_Write(GenClass obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
                writer.Write("intVal0", fields[0], obj.intVal0, ref firstMember);
                writer.Write("intVal1", fields[1], obj.intVal1, ref firstMember);
                writer.Write("child",   fields[2], obj.child,   ref firstMember);
            }
            
            // delegate bool ReadFieldDelegate<in T>(T obj, PropField field, ref Reader reader);
            
            private static bool  Gen_ReadField (GenClass obj, PropField field, ref Reader reader) {
                switch (field.fieldIndex) {
                    case 0: return reader.Read("intVal0", field, ref obj.intVal0);
                    case 1: return reader.Read("intVal1", field, ref obj.intVal1);
                    case 2: return reader.Read("child",   field, ref obj.child);
                }
                return false;
            }
        }
        
        [Test]
        public static void TestGeneratorClass() {
            var genClass    = new GenClass { intVal0 = 11, intVal1 = 12, child = new GenChild { val = 22 }};
            var typeStore   = new TypeStore();
            var mapper      = new ObjectMapper(typeStore);

            var json = mapper.WriteAsValue(genClass);
            
            AreEqual(@"{""intVal0"":11,""intVal1"":12,""child"":{""val"":22}}", json.AsString());
            
            var dest    = new GenClass();
            mapper.ReadTo(json, dest);
            
            AreEqual(11, dest.intVal0);
            AreEqual(12, dest.intVal1);
            AreEqual(22, dest.child.val);
            
            var start = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < 10; n++) {
                mapper.writer.WriteAsBytes(genClass);
            }
            for (int n = 0; n < 10; n++) {
                mapper.ReadTo(json, dest);
            }
            var dif = GC.GetAllocatedBytesForCurrentThread() - start;
            AreEqual(0, dif);
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
            var genClass    = new GenClass { intVal0 = 11, intVal1 = 12 };
            var options     = new System.Text.Json.JsonSerializerOptions {IncludeFields = true};
            var json = System.Text.Json.JsonSerializer.Serialize(genClass, options);
            AreEqual(@"{""intVal0"":11,""intVal1"":12,""child"":null}", json);
            
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