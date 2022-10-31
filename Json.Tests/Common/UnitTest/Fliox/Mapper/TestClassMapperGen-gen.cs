// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    static class Gen_GenChild {
           
        private const int Gen_val   = 0;
            
        private static bool  ReadField (GenChild obj, PropField field, ref Reader reader) {
            switch (field.genIndex) {
                case Gen_val: return reader.Read(field, ref obj.val);
            }
            return false;
        }
            
        private static void Write(GenChild obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.Write(fields[0], obj.val, ref firstMember);
        }
    }
            
    static class Gen_GenClass {
            
        private const int Gen_intVal0   = 0;
        private const int Gen_intVal1   = 1;
        private const int Gen_child     = 2;
            
        // delegate bool ReadFieldDelegate<in T>(T obj, PropField field, ref Reader reader);
            
        private static bool  ReadField (GenClass obj, PropField field, ref Reader reader) {
            switch (field.genIndex) {
                case Gen_intVal0:   return reader.Read   (field, ref obj.intVal0);
                case Gen_intVal1:   return reader.Read   (field, ref obj.intVal1);
                case Gen_child:     return reader.ReadObj(field, ref obj.child);
            }
            return false;
        }
            
        // delegate void WriteDelegate<in T>(T obj, PropField[] fields, ref Writer writer, ref bool firstMember);
            
        private static void Write(GenClass obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.Write    (fields[Gen_intVal0],   obj.intVal0, ref firstMember);
            writer.Write    (fields[Gen_intVal1],   obj.intVal1, ref firstMember);
            writer.WriteObj (fields[Gen_child],     obj.child,   ref firstMember);
        }
    }
}
