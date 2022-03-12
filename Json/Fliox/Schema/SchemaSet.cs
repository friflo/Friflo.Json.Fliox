// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema
{
    public delegate SchemaSet   SchemaGenerator(GeneratorOptions options);
    public delegate byte[]      CreateZip(Dictionary<string, string> files);

    public sealed class CustomGenerator
    {
        internal readonly   string          type;
        internal readonly   string          name;
        internal readonly   SchemaGenerator schemaGenerator;
        
        public CustomGenerator (string type, string name, SchemaGenerator schemaGenerator) {
            this.type               = type;
            this.name               = name;
            this.schemaGenerator    = schemaGenerator;
        }
    }
    
    public sealed class SchemaSet {
        public   readonly   string                      type;           // csharp, json-schema, ...
        public   readonly   string                      label;          // C#,     JSON Schema, ...
        public   readonly   string                      contentType;
        public   readonly   Dictionary<string, string>  files;
        public   readonly   string                      fullSchema;
        public   readonly   string                      directory;
        public   readonly   string                      zipName;
        private             byte[]                      zipArchive;

        public byte[] GetZipArchive (CreateZip zip) {
            if (zipArchive == null && zip != null ) {
                zipArchive = zip(files);
            }
            return zipArchive;
        }

        public SchemaSet (ObjectWriter writer, string type, string label, string contentType, Dictionary<string, string> files, string fullSchema = null) {
            this.type           = type;
            this.label          = label;
            this.contentType    = contentType;
            this.files          = files;
            this.fullSchema     = fullSchema;
            zipName             = $".{type}.zip";
            directory           = writer.Write(files.Keys.ToList());
        }

        public static Dictionary<string, SchemaSet> GenerateSchemas(
            TypeSchema                      typeSchema,
            ICollection<TypeDef>            separateTypes,
            IEnumerable<CustomGenerator>    generators = null)
        {
            generators = generators ?? Array.Empty<CustomGenerator>();
            using (var writer = new ObjectWriter(new TypeStore())) {
                writer.Pretty = true;
                return GenerateSchemas(typeSchema, separateTypes, writer, generators);
            }
        }
        
        private static Dictionary<string, SchemaSet> GenerateSchemas(
            TypeSchema                      typeSchema,
            ICollection<TypeDef>            separateTypes,
            ObjectWriter                    writer,
            IEnumerable<CustomGenerator>    generators)
        {
            var result              = new Dictionary<string, SchemaSet>();
            var options             = new JsonTypeOptions(typeSchema);

            var htmlGenerator       = HtmlGenerator.Generate(options);
            var htmlSchema          = new SchemaSet (writer, "html",        "HTML",        "text/html",        htmlGenerator.files);
            result.Add(htmlSchema.type,       htmlSchema);
            
            var jsonOptions         = new JsonTypeOptions(typeSchema) { separateTypes = separateTypes };
            var jsonGenerator       = JsonSchemaGenerator.Generate(jsonOptions);
            var jsonSchemaMap       = new Dictionary<string, JsonValue>();
            foreach (var pair in jsonGenerator.files) {
                jsonSchemaMap.Add(pair.Key,new JsonValue(pair.Value));
            }
            var fullSchema          = writer.Write(jsonSchemaMap);
            var jsonSchema          = new SchemaSet (writer, "json-schema", "JSON Schema", "application/json", jsonGenerator.files, fullSchema);
            result.Add(jsonSchema.type,  jsonSchema);
            
            var typescriptGenerator = TypescriptGenerator.Generate(options);
            var typescriptSchema    = new SchemaSet (writer, "typescript",  "Typescript",  "text/plain",       typescriptGenerator.files);
            result.Add(typescriptSchema.type,   typescriptSchema);
            
            var csharpGenerator     = CSharpGenerator.Generate(options);
            var csharpSchema        = new SchemaSet (writer, "csharp",      "C#",          "text/plain",       csharpGenerator.files);
            result.Add(csharpSchema.type,       csharpSchema);
            
            var kotlinGenerator     = KotlinGenerator.Generate(options);
            var kotlinSchema        = new SchemaSet (writer, "kotlin",      "Kotlin",      "text/plain",       kotlinGenerator.files);
            result.Add(kotlinSchema.type,       kotlinSchema);

            foreach (var generator in generators) {
                var generatorOpt = new GeneratorOptions(generator.type, generator.name, options.schema, options.replacements, options.separateTypes, writer);
                try {
                    var schemaSet = generator.schemaGenerator(generatorOpt);
                    result.Add(generator.type, schemaSet);
                } catch (Exception e) {
                    Console.WriteLine($"SchemaSet generation failed for: {generator.name}. error: {e.Message}");
                }
            }
            return result;
        }
    }
}