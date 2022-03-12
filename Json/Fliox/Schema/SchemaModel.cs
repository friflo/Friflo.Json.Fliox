// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema
{
    public delegate SchemaModel    SchemaGenerator(GeneratorOptions options);
    public delegate byte[]         CreateZip(IDictionary<string, string> files);

    public sealed class CustomGenerator
    {
        internal  readonly  string          type;   // jtd, ...
        internal  readonly  string          name;   // JSON Type Definition, ...
        internal  readonly  SchemaGenerator schemaGenerator;

        public    override  string          ToString() => type;

        public CustomGenerator (string type, string name, SchemaGenerator schemaGenerator) {
            this.type               = type;
            this.name               = name;
            this.schemaGenerator    = schemaGenerator;
        }
    }
    
    /// <summary>
    /// <see cref="SchemaModel"/> instances are used to represent schemas in various programming languages
    /// or schema formats like C#, Typescript, Kotlin, JSON Schema and HTML.<br/>
    /// <see cref="SchemaModel"/> instances are immutable.
    /// </summary>
    public sealed class SchemaModel {
        public   readonly   string                              type;           // csharp, json-schema, ...
        public   readonly   string                              label;          // C#,     JSON Schema, ...
        public   readonly   string                              contentType;    // "text/plain", "text/html", ...
        public   readonly   ReadOnlyDictionary<string, string>  files;          // key: file name, value: file content
        public   readonly   string                              fullSchema;     // set only for JSON Schema
        public   readonly   string                              zipNameSuffix;  // .csharp.zip, json-schema.zip, ...
        private             byte[]                              zipArchive;

        public   override   string                              ToString() => type;

        public byte[] GetZipArchive (CreateZip zip) {
            if (zipArchive == null && zip != null ) {
                zipArchive = zip(files);
            }
            return zipArchive;
        }

        public SchemaModel (string type, string label, string contentType, Dictionary<string, string> files, string fullSchema = null) {
            this.type           = type;
            this.label          = label;
            this.contentType    = contentType;
            this.files          = new ReadOnlyDictionary<string, string>(files);
            this.fullSchema     = fullSchema;
            zipNameSuffix       = $".{type}.zip";
        }

        public static Dictionary<string, SchemaModel> GenerateSchemaModels(
            TypeSchema                      typeSchema,
            ICollection<TypeDef>            separateTypes,
            IEnumerable<CustomGenerator>    generators = null)
        {
            generators = generators ?? Array.Empty<CustomGenerator>();
            using (var writer = new ObjectWriter(new TypeStore())) {
                writer.Pretty = true;
                return GenerateSchemaModels(typeSchema, separateTypes, writer, generators);
            }
        }
        
        private static Dictionary<string, SchemaModel> GenerateSchemaModels(
            TypeSchema                      typeSchema,
            ICollection<TypeDef>            separateTypes,
            ObjectWriter                    writer,
            IEnumerable<CustomGenerator>    generators)
        {
            var result              = new Dictionary<string, SchemaModel>();
            var options             = new JsonTypeOptions(typeSchema);

            var htmlGenerator       = HtmlGenerator.Generate(options);
            var htmlSchema          = new SchemaModel ("html",          "HTML",        "text/html",        htmlGenerator.files);
            result.Add(htmlSchema.type,       htmlSchema);
            
            var jsonOptions         = new JsonTypeOptions(typeSchema) { separateTypes = separateTypes };
            var jsonGenerator       = JsonSchemaGenerator.Generate(jsonOptions);
            var jsonSchemaMap       = new Dictionary<string, JsonValue>();
            foreach (var pair in jsonGenerator.files) {
                jsonSchemaMap.Add(pair.Key,new JsonValue(pair.Value));
            }
            var fullSchema          = writer.Write(jsonSchemaMap);
            var jsonModel           = new SchemaModel ("json-schema",   "JSON Schema", "application/json", jsonGenerator.files, fullSchema);
            result.Add(jsonModel.type,  jsonModel);
            
            var typescriptGenerator = TypescriptGenerator.Generate(options);
            var typescriptModel     = new SchemaModel ("typescript",    "Typescript",  "text/plain",       typescriptGenerator.files);
            result.Add(typescriptModel.type,   typescriptModel);
            
            var csharpGenerator     = CSharpGenerator.Generate(options);
            var csharpModel         = new SchemaModel ("csharp",        "C#",          "text/plain",       csharpGenerator.files);
            result.Add(csharpModel.type,       csharpModel);
            
            var kotlinGenerator     = KotlinGenerator.Generate(options);
            var kotlinModel         = new SchemaModel ("kotlin",        "Kotlin",      "text/plain",       kotlinGenerator.files);
            result.Add(kotlinModel.type,       kotlinModel);

            foreach (var generator in generators) {
                var generatorOpt = new GeneratorOptions(generator.type, generator.name, options.schema, options.replacements, options.separateTypes);
                try {
                    var schemaModel = generator.schemaGenerator(generatorOpt);
                    result.Add(generator.type, schemaModel);
                } catch (Exception e) {
                    Console.WriteLine($"SchemaSet generation failed for: {generator.name}. error: {e.Message}");
                }
            }
            return result;
        }
    }
}