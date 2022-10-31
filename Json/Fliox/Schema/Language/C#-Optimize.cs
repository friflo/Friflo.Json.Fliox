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

        private CSharpOptimizeGenerator (Generator generator) {
            this.generator  = generator;
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
            // generator.GroupTypesByPath(true); // sort dependencies - otherwise possible error TS2449: Class '...' used before its declaration.
            generator.EmitFiles(sb, ns => $"{ns}{generator.fileExt}");
        }
        
        
        private EmitType EmitType(TypeDef type, StringBuilder sb) {
            if (type.IsClass) {
                return EmitClassType(type, sb);
            }
            return null;
        }
        
        private EmitType EmitClassType(TypeDef type, StringBuilder sb) {
            var imports     = new HashSet<TypeDef>();
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
            sb.AppendLF($"    static class {type.Name} {{");
            
            // int maxFieldName    = emitFields.MaxLength(field => field.type.Length);
            // --- field indices
            int index = 0;
            foreach (var field in emitFields) {
                sb.AppendLF($"        private const in Gen_{field.def.name} = {index++}");
            }
            
            // --- ReadField(...)
            sb.AppendLF($"        private static bool ReadField ({type.Name} obj, PropField field, ref Reader reader) {{");
            sb.AppendLF($"            switch (field.genIndex) {{");
            foreach (var field in emitFields) {
                sb.AppendLF($"                case Gen_{field.def.name}:   return reader.Read   (field, ref obj.{field.def.name});");
            }
            sb.AppendLF("            }");
            sb.AppendLF("        }");
            
            // --- Write(...)
            sb.AppendLF($"        private static void Write(GenClass obj, PropField[] fields, ref Writer writer, ref bool firstMember) {{");
            foreach (var field in emitFields) {
                sb.AppendLF($"            writer.Write    (fields[Gen_{field.def.name}],   obj.{field.def.name}, ref firstMember);");
            }
            sb.AppendLF("        }");
            sb.AppendLF("    }");
            sb.AppendLF();
            return new EmitType(type, sb, imports);
        }
    }
}