// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Native;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Schema.Language
{
    public delegate SchemaModel    SchemaGenerator(GeneratorOptions options);

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
    /// or schema formats like C#, Typescript, Kotlin, JSON Schema / OpenAPI and HTML.<br/>
    /// <see cref="SchemaModel"/> instances are immutable.
    /// </summary>
    public sealed class SchemaModel {
        public   readonly   string                              type;           // csharp,          json-schema, ...
        public   readonly   string                              label;          // C#,              JSON Schema, ...
        public   readonly   string                              contentType;    // "text/plain",    "text/html", ...
        public   readonly   string                              fileExt;        // .cs,             .json, ...
        public   readonly   ReadOnlyDictionary<string, string>  files;          // key: file name, value: file content

        public   override   string                              ToString() => type;

        public SchemaModel (string type, string label, string contentType, string fileExt, IDictionary<string, string> files) {
            this.type           = type;
            this.label          = label;
            this.contentType    = contentType;
            this.fileExt        = fileExt;
            this.files          = new ReadOnlyDictionary<string, string>(files);
        }
        
        /// <summary>
        /// Generate schema models for the given <paramref name="rootType"/>. <br/>
        /// The generated languages are the build-in supported languages: HTML, JSON Schema / OpenAPI, Typescript, C#, Kotlin
        /// and languages that are generated via the passed <paramref name="generators"/>
        /// </summary>
        public static List<SchemaModel> GenerateSchemaModels(Type rootType, IEnumerable<CustomGenerator> generators = null, string databaseUrl = null) {
            var typeSchema      = NativeTypeSchema.Create(rootType);
            var entityTypeMap   = typeSchema.GetEntityTypes();
            var entityTypes     = entityTypeMap.Values;
            return GenerateSchemaModels(typeSchema, entityTypes, generators, databaseUrl);
        }

        private const string TextPlain      = "text/plain; charset=utf-8";
        private const string TextHtml       = "text/html; charset=utf-8";
        private const string TextMarkdown   = "text/markdown; charset=utf-8";

        /// <summary>
        /// Generate schema models for build-in supported languages: HTML, JSON Schema / OpenAPI, Typescript, C#, Kotlin
        /// and languages that are generated via the passed <paramref name="generators"/>
        /// </summary>
        public static List<SchemaModel> GenerateSchemaModels(
            TypeSchema                      typeSchema,
            ICollection<TypeDef>            separateTypes,
            IEnumerable<CustomGenerator>    generators = null,
            string                          databaseUrl = null)
        {
            generators              = generators ?? Array.Empty<CustomGenerator>();
            var result              = new List<SchemaModel>();
            var options             = new JsonTypeOptions(typeSchema);

            var htmlGenerator       = HtmlGenerator.Generate(options);
            var htmlSchema          = new SchemaModel ("html",          "HTML",         TextHtml,       ".html",    htmlGenerator.files);
            result.Add(htmlSchema);
            
            var jsonOptions         = new JsonTypeOptions(typeSchema) { separateTypes = separateTypes, databaseUrl = databaseUrl};
            var jsonGenerator       = JsonSchemaGenerator.Generate(jsonOptions);
            var jsonModel           = new SchemaModel ("json-schema",   "JSON Schema / OpenAPI", "application/json", ".json", jsonGenerator.files);
            result.Add(jsonModel);
            
            var graphQLGenerator    = GraphQLGenerator.Generate(options);
            var graphQLModel        = new SchemaModel ("graphql",       "GraphQL",      TextPlain,      ".graphql", graphQLGenerator.files);
            result.Add(graphQLModel);
            
            var markdownGenerator   = MarkdownGenerator.Generate(options);
            var markdownModel       = new SchemaModel ("markdown",      "Markdown",     TextMarkdown,   ".md",      markdownGenerator.files);
            result.Add(markdownModel);
            
            var typescriptGenerator = TypescriptGenerator.Generate(options);
            var typescriptModel     = new SchemaModel ("typescript",    "Typescript",   TextPlain,      ".d.ts",    typescriptGenerator.files);
            result.Add(typescriptModel);
            
            var csharpGenerator     = CSharpGenerator.Generate(options);
            var csharpModel         = new SchemaModel ("csharp",        "C#",           TextPlain,      ".cs",      csharpGenerator.files);
            result.Add(csharpModel);
            
            var kotlinGenerator     = KotlinGenerator.Generate(options);
            var kotlinModel         = new SchemaModel ("kotlin",        "Kotlin",       TextPlain,      ".kt",      kotlinGenerator.files);
            result.Add(kotlinModel);

            foreach (var generator in generators) {
                var generatorOpt = new GeneratorOptions(generator.type, generator.name, options.schema, options.replacements, options.separateTypes);
                try {
                    var schemaModel = generator.schemaGenerator(generatorOpt);
                    result.Add(schemaModel);
                } catch (Exception e) {
                    Console.Error.WriteLine($"SchemaModel generation failed for: {generator.name}. error: {e.Message}");
                }
            }
            return result;
        }
        
        /// <summary>
        /// Write the generated file to the given folder and remove all others file with the used <see cref="fileExt"/>
        /// </summary>
        public void WriteFiles(string folder, bool cleanFolder = true) {
            Generator.WriteFilesInternal(folder, files, fileExt, cleanFolder);
        }
    }
}