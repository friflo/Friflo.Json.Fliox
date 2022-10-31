// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Utils;
using static Friflo.Json.Fliox.Schema.Language.Generator;
// Allowed namespaces: .Schema.Definition, .Schema.Doc, .Schema.Utils

namespace Friflo.Json.Fliox.Schema.Language
{
    public sealed class CSharpOptimizeGenerator
    {
        private  readonly   Generator                   generator;
        private  readonly   Dictionary<TypeDef, string> methodSuffixes;

        private CSharpOptimizeGenerator (Generator generator) {
            this.generator  = generator;
            methodSuffixes  = GetMethodSuffixes(generator.standardTypes);
        }
        
        public static void Generate(Generator generator) {
            var emitter = new CSharpOptimizeGenerator(generator);
            var sb      = new StringBuilder();
            foreach (var type in generator.types) {
                sb.Clear();
                var result = emitter.EmitType(type, sb);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
            }
            generator.EmitTypes();
        }
        
        private static Dictionary<TypeDef, string> GetMethodSuffixes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.Boolean,     "Boolean" );
            AddType (map, standard.String,      "String" );

            AddType (map, standard.Uint8,       "Byte" );
            AddType (map, standard.Int16,       "Int16" );
            AddType (map, standard.Int32,       "Int32" );
            AddType (map, standard.Int64,       "Int64" );
               
            AddType (map, standard.Double,      "Double" );
            AddType (map, standard.Float,       "Single" );
            AddType (map, standard.JsonKey,     "JsonKey" );
            return map;
        }

        private EmitType EmitType(TypeDef type, StringBuilder sb) {
            if (type.IsClass && !type.IsSchema) {
                return EmitClassType(type, sb);
            }
            return null;
        }
        
        private EmitType EmitClassType(TypeDef type, StringBuilder sb) {
            var fields      = type.Fields;
            var emitFields = new List<EmitField>(fields.Count);
            foreach (var field in fields) {
                var emitField = new EmitField(null, field);
                emitFields.Add(emitField);
            }
            sb.AppendLF($"// {Note}");
            sb.AppendLF("using Friflo.Json.Fliox.Mapper.Map;");
            sb.AppendLF("using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;");
            sb.AppendLF($"using {type.Namespace};");
            sb.AppendLF("");
            sb.AppendLF($"namespace Gen.{type.Namespace}");
            sb.AppendLF("{");
            sb.AppendLF($"    static class Gen_{type.Name}");
            sb.AppendLF("    {");
            int maxFieldName    = emitFields.MaxLength(field => field.def.name.Length);
            
            // --- field indices
            int index = 0;
            foreach (var field in emitFields) {
                sb.AppendLF($"        private const int Gen_{field.def.name} = {index++};");
            }
            sb.AppendLF("");
            
            // --- ReadField(...)
            sb.AppendLF($"        private static bool ReadField ({type.Name} obj, PropField field, ref Reader reader) {{");
            sb.AppendLF($"            bool success;");
            sb.AppendLF($"            switch (field.genIndex) {{");
            foreach (var field in emitFields) {
                
                var name        = field.def.name;
                var indent      = Indent(maxFieldName, name);
                var fieldDef    = field.def;
                if (fieldDef.type.IsClass || fieldDef.type.IsStruct || fieldDef.isArray || fieldDef.isDictionary) {
                    sb.AppendLF($"                case Gen_{name}:{indent} obj.{field.def.nativeName}{indent} = reader.Read   (field, obj.{field.def.nativeName}, out success);  return success;");
                } else {
                    var suffix  = GetMethodSuffix(field.def);
                    sb.AppendLF($"                case Gen_{name}:{indent} obj.{field.def.nativeName}{indent} = reader.Read{suffix}   (field, out success);  return success;");
                }
            }
            sb.AppendLF("            }");
            sb.AppendLF("            return false;");
            sb.AppendLF("        }");
            sb.AppendLF("");
            
            // --- Write(...)
            sb.AppendLF($"        private static void Write({type.Name} obj, PropField[] fields, ref Writer writer, ref bool firstMember) {{");
            foreach (var field in emitFields) {
                var name    = field.def.name;
                var indent  = Indent(maxFieldName, name);
                sb.AppendLF($"            writer.Write    (fields[Gen_{name}],{indent} obj.{field.def.nativeName},{indent} ref firstMember);");
            }
            sb.AppendLF("        }");
            sb.AppendLF("    }");
            sb.AppendLF("}");
            sb.AppendLF();
            return new EmitType(type, sb);
        }
        
        private string GetMethodSuffix(FieldDef field) {
            if (field.type.IsClass || field.type.IsStruct)
                return "";
            var suffix = methodSuffixes[field.type];
            if (field.isNullableElement)
                return suffix + "Null";
            return suffix;
        }
    }
}