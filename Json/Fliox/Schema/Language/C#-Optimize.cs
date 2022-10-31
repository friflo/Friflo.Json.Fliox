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
        private  readonly   Dictionary<TypeDef, string> standardType;

        private CSharpOptimizeGenerator (Generator generator) {
            this.generator  = generator;
            standardType  = GetStandardTypes(generator.standardTypes);
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
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.Boolean,     "Boolean" );

            AddType (map, standard.Uint8,       "Byte" );
            AddType (map, standard.Int16,       "Int16" );
            AddType (map, standard.Int32,       "Int32" );
            AddType (map, standard.Int64,       "Int64" );
               
            AddType (map, standard.Double,      "Double" );
            AddType (map, standard.Float,       "Single" );
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
            sb.AppendLF("// ReSharper disable InconsistentNaming");
            sb.AppendLF($"namespace Gen.{type.Namespace}");
            sb.AppendLF("{");
            sb.AppendLF($"    static class Gen_{type.Name}");
            sb.AppendLF("    {");
            int maxFieldName    = emitFields.MaxLength(field => field.def.nativeName.Length);
            int maxSuffix       = emitFields.MaxLength(field => IsPrimitive(field.def, out var suffix) ? suffix.Length : 4);
            
            // --- field indices
            int index = 0;
            foreach (var field in emitFields) {
                sb.AppendLF($"        private const int Gen_{field.def.nativeName} = {index++};");
            }
            sb.AppendLF("");
            
            // --- ReadField(...)
            sb.AppendLF($"        private static bool ReadField ({type.Name} obj, PropField field, ref Reader reader) {{");
            sb.AppendLF($"            bool success;");
            sb.AppendLF($"            switch (field.genIndex) {{");
            foreach (var field in emitFields) {
                var def     = field.def;
                var name    = def.nativeName;
                var indent  = Indent(maxFieldName, name);
                if (IsPrimitive(def, out string suffix)) {
                    var suffixIndent    = Indent(maxSuffix, suffix);
                    sb.AppendLF($"                case Gen_{name}:{indent} obj.{name}{indent} = reader.Read{suffix}{suffixIndent} (field, out success);  return success;");
                } else {
                    var suffixIndent    = Indent(maxSuffix, "");
                    sb.AppendLF($"                case Gen_{name}:{indent} obj.{name}{indent} = reader.Read{suffixIndent} (field, obj.{name},{indent} out success);  return success;");
                } 
            }
            sb.AppendLF("            }");
            sb.AppendLF("            return false;");
            sb.AppendLF("        }");
            sb.AppendLF("");
            
            // --- Write(...)
            sb.AppendLF($"        private static void Write({type.Name} obj, PropField[] fields, ref Writer writer, ref bool firstMember) {{");
            foreach (var field in emitFields) {
                var def     = field.def;
                var name    = def.nativeName;
                var indent  = Indent(maxFieldName, name);
                if (IsPrimitive(def, out string suffix)) {
                    var suffixIndent    = Indent(maxSuffix, suffix);
                    sb.AppendLF($"            writer.Write{suffix}{suffixIndent} (fields[Gen_{name}],{indent} obj.{name},{indent} ref firstMember);");
                } else {
                    var suffixIndent    = Indent(maxSuffix, "");
                    sb.AppendLF($"            writer.Write{suffixIndent} (fields[Gen_{name}],{indent} obj.{name},{indent} ref firstMember);");
                } 
            }
            sb.AppendLF("        }");
            sb.AppendLF("    }");
            sb.AppendLF("}");
            sb.AppendLF();
            return new EmitType(type, sb);
        }
        
        private bool IsPrimitive(FieldDef field, out string suffix) {
            if (field.isArray || field.isDictionary) {
                suffix = "";
                return false;
            }
            var type = field.type;
            if (type == generator.standardTypes.String) {
                suffix = "String";
                return true;
            }
            if (type == generator.standardTypes.JsonKey) {
                suffix = "JsonKey";
                return true;
            }
            if (standardType.TryGetValue(type, out suffix)) {
                if (!field.required) {
                    suffix += "Null";
                }
                return true;
            }
            suffix = "";
            return false;
        }
    }
}