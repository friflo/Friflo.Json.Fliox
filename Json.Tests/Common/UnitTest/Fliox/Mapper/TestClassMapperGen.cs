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
        class GenClass
        {
            public int intVal0;
            public int intVal1;
            
            // delegate void WriteDelegate<in T>(T obj, PropField[] fields, ref Writer writer, ref bool firstMember);
            
            private static void Gen_Write(GenClass obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
                writer.Write("intVal0", fields[0], obj.intVal0, ref firstMember);
                writer.Write("intVal1", fields[1], obj.intVal1, ref firstMember);
            }
            
            // delegate void ReadFieldDelegate<in T>(T obj, PropField field, ref Reader reader, out bool success);
            
            private static void  Gen_ReadField (GenClass obj, PropField field, ref Reader reader, out bool success) {
                switch (field.fieldIndex) {
                    case 0:   obj.intVal0 = reader.ReadInt32("intVal0", field, out success);   return;
                    case 1:   obj.intVal1 = reader.ReadInt32("intVal1", field, out success);   return;
                }
                success = false;
            }
        }
        
        [Test]
        public static void TestGeneratorClass() {
            var genClass    = new GenClass { intVal0 = 11, intVal1 = 12 };
            var typeStore   = new TypeStore();
            var mapper      = new ObjectMapper(typeStore);
            
            var json = mapper.WriteAsValue(genClass);
            
            AreEqual(@"{""intVal0"":11,""intVal1"":12}", json.AsString());
            
            var dest    = new GenClass();
            mapper.ReadTo(json, dest);
            
            AreEqual(11, dest.intVal0);
            AreEqual(12, dest.intVal1);
            
            var start = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < 10; n++) {
                mapper.writer.WriteAsBytes(genClass);
            }
            for (int n = 0; n < 10_000_000; n++) {
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
        
        [Test]
        public static void TestSystemTextJson() {
            var genClass    = new GenClass { intVal0 = 11, intVal1 = 12 };
            var options     = new System.Text.Json.JsonSerializerOptions {IncludeFields = true};
            var json = System.Text.Json.JsonSerializer.Serialize(genClass, options);
            AreEqual(@"{""intVal0"":11,""intVal1"":12}", json);
            
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
    }
}