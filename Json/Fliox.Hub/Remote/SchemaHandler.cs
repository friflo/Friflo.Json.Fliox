// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Hub.Remote
{
    public delegate byte[] CreateZip(Dictionary<string, string> files);
    
    internal sealed class SchemaHandler : IRequestHandler
    {
        private  const      string                              SchemaBase = "/schema";
        private  readonly   FlioxHub                            hub;
        internal            string                              image = "/img/Json-Fliox-53x43.svg";
        internal readonly   CreateZip                           zip;
        private  readonly   Dictionary<string, SchemaResource>  schemas = new Dictionary<string, SchemaResource>();
        private  readonly   List<CustomGenerator>               generators = new List<CustomGenerator>();
        private  readonly   string                              cacheControl;

        internal SchemaHandler(FlioxHub hub, string cacheControl, CreateZip zip = null) {
            this.hub            = hub;
            this.zip            = zip;
            this.cacheControl   = cacheControl;
        }
        
        public bool IsMatch(RequestContext context) {
            if (context.method != "GET")
                return false;
            return RequestContext.IsBasePath(SchemaBase, context.path);
        }
        
        public Task HandleRequest(RequestContext context) {
            if (context.path.Length == SchemaBase.Length) {
                context.WriteError("invalid schema path", "missing database", 400);
                return Task.CompletedTask;
            }
            var path        = context.path.Substring(SchemaBase.Length + 1);
            var firstSlash  = path.IndexOf('/');
            var name        = firstSlash == -1 ? path : path.Substring(0, firstSlash);
            if (!schemas.TryGetValue(name, out var schema)) {
                if (!hub.TryGetDatabase(name, out var database)) {
                    context.WriteError("schema not found", name, 404);
                    return Task.CompletedTask;
                }
                var typeSchema = database.Schema.typeSchema;
                if (typeSchema == null) {
                    context.WriteError("missing schema for database", name, 404);
                    return Task.CompletedTask;
                }
                schema = AddSchema(name, database.Schema.typeSchema);
            }
            var schemaPath  = path.Substring(firstSlash + 1);
            Result result   = new Result();
            bool success    = schema.GetSchemaFile(schemaPath, ref result, this);
            if (!success) {
                context.WriteError("schema error", result.content, 404);
                return Task.CompletedTask;
            }
            if (cacheControl != null) {
                context.AddHeader("Cache-Control", cacheControl); // seconds
            }
            if (result.isText) {
                context.WriteString(result.content, result.contentType);
                return Task.CompletedTask;
            }
            context.Write(new JsonValue(result.bytes), 0, result.contentType, 200);
            return Task.CompletedTask;
        }
        
        internal SchemaResource AddSchema(string name, TypeSchema typeSchema, ICollection<TypeDef> sepTypes = null) {
            sepTypes    = sepTypes ?? typeSchema.GetEntityTypes();
            var schema  = new SchemaResource(typeSchema, sepTypes);
            schemas.Add(name, schema);
            return schema;
        }
        
        public void AddGenerator(string type, string name, Func<GeneratorOptions, SchemaSet> generate) {
            if (name == null) throw new NullReferenceException(nameof(name));
            var generator = new CustomGenerator(type, name, generate);
            generators.Add(generator);
        }
        
        internal Dictionary<string, SchemaSet> GenerateSchemas(TypeSchema typeSchema, ICollection<TypeDef> separateTypes, ObjectWriter writer) {
            var result              = new Dictionary<string, SchemaSet>();
            var options             = new JsonTypeOptions(typeSchema);

            var htmlGenerator       = HtmlGenerator.Generate(options);
            var htmlSchema          = new SchemaSet (writer, "HTML",        "text/html",        htmlGenerator.files);
            result.Add("html",       htmlSchema);
            
            var jsonOptions         = new JsonTypeOptions(typeSchema) { separateTypes = separateTypes };
            var jsonGenerator       = JsonSchemaGenerator.Generate(jsonOptions);
            var jsonSchemaMap       = new Dictionary<string, JsonValue>();
            foreach (var pair in jsonGenerator.files) {
                jsonSchemaMap.Add(pair.Key,new JsonValue(pair.Value));
            }
            var fullSchema          = writer.Write(jsonSchemaMap);
            var jsonSchema          = new SchemaSet (writer, "JSON Schema", "application/json", jsonGenerator.files, fullSchema);
            result.Add("json-schema",  jsonSchema);
            
            var typescriptGenerator = TypescriptGenerator.Generate(options);
            var typescriptSchema    = new SchemaSet (writer, "Typescript",  "text/plain",       typescriptGenerator.files);
            result.Add("typescript",   typescriptSchema);
            
            var csharpGenerator     = CSharpGenerator.Generate(options);
            var csharpSchema        = new SchemaSet (writer, "C#",          "text/plain",       csharpGenerator.files);
            result.Add("csharp",       csharpSchema);
            
            var kotlinGenerator     = KotlinGenerator.Generate(options);
            var kotlinSchema        = new SchemaSet (writer, "Kotlin",      "text/plain",       kotlinGenerator.files);
            result.Add("kotlin",       kotlinSchema);

            foreach (var generator in generators) {
                var generatorOpt = new GeneratorOptions(generator.type, generator.name, options.schema, options.replacements, options.separateTypes, writer);
                try {
                    var schemaSet = generator.generateSchemaSet(generatorOpt);
                    result.Add(generator.type, schemaSet);
                } catch (Exception e) {
                    Console.WriteLine($"SchemaSet generation failed for: {generator.name}. error: {e.Message}");
                }
            }
            return result;
        }
    }
    
    internal sealed class CustomGenerator
    {
        internal readonly string                            type;
        internal readonly string                            name;
        internal readonly Func<GeneratorOptions, SchemaSet> generateSchemaSet;
        
        internal CustomGenerator (string type, string name, Func<GeneratorOptions, SchemaSet> generateSchemaSet) {
            this.type               = type;
            this.name               = name;
            this.generateSchemaSet  = generateSchemaSet;
        }
    }


    internal sealed class SchemaResource {
        private  readonly   TypeSchema                      typeSchema;
        private  readonly   ICollection<TypeDef>            separateTypes;

        private             Dictionary<string, SchemaSet>   schemas;
        private  readonly   string                          schemaName;
        
        internal SchemaResource(TypeSchema typeSchema, ICollection<TypeDef> separateTypes) {
            this.schemaName     = typeSchema.RootType.Name;
            this.typeSchema     = typeSchema;
            this.separateTypes  = separateTypes;
        }

        internal bool GetSchemaFile(string path, ref Result result, SchemaHandler handler) {
            if (typeSchema == null) {
                return result.Error("no schema attached to database");
            }
            var storeName = typeSchema.RootType.Name;
            if (schemas == null) {
                using (var writer = new ObjectWriter(new TypeStore())) {
                    writer.Pretty = true;
                    schemas = handler.GenerateSchemas(typeSchema, separateTypes, writer);
                }
            }
            if (path == "index.html") {
                var sb = new StringBuilder();
                HtmlHeader(sb, new []{"Hub", schemaName}, $"Available schemas / languages for schema <b>{storeName}</b>", handler);
                sb.AppendLine("<ul>");
                foreach (var pair in schemas) {
                    sb.AppendLine($"<li><a href='./{pair.Key}/index.html'>{pair.Value.name}</a></li>");
                }
                sb.AppendLine("</ul>");
                HtmlFooter(sb);
                return result.Set(sb.ToString(), "text/html");
            }
            if (path == "json-schema.json") {
                var jsonSchema = schemas["json-schema"];
                return result.Set(jsonSchema.fullSchema, jsonSchema.contentType);
            }
            var schemaTypeEnd = path.IndexOf('/');
            if (schemaTypeEnd <= 0) {
                return result.Error($"invalid path:  {path}");
            }
            var schemaType = path.Substring(0, schemaTypeEnd);
            if (!schemas.TryGetValue(schemaType, out SchemaSet schemaSet)) {
                return result.Error($"unknown schema type: {schemaType}");
            }
            var fileName = path.Substring(schemaTypeEnd + 1);
            if (fileName == "index.html") {
                var zipFile = $"{storeName}{schemaSet.zipName}";
                var sb = new StringBuilder();
                HtmlHeader(sb, new[]{"Hub", schemaName, schemaSet.name}, $"{schemaSet.name} files schema: <b>{storeName}</b>", handler);
                sb.AppendLine($"<a href='{zipFile}'>{zipFile}</a><br/>");
                sb.AppendLine($"<a href='directory' target='_blank'>{storeName} {schemaSet.name} files</a>");
                sb.AppendLine("<ul>");
                var target = schemaSet.contentType == "text/html" ? "" : " target='_blank'";
                foreach (var file in schemaSet.files.Keys) {
                    sb.AppendLine($"<li><a href='./{file}'{target}>{file}</a></li>");
                }
                sb.AppendLine("</ul>");
                HtmlFooter(sb);
                return result.Set(sb.ToString(), "text/html");
            }
            if (fileName.StartsWith(storeName) && fileName.EndsWith(schemaSet.zipName)) {
                result.bytes        = schemaSet.GetZipArchive(handler.zip);
                if (result.bytes == null)
                    return result.Error("ZipArchive not supported (Unity)");
                result.contentType  = "application/zip";
                result.isText       = false;
                return true;
            }
            if (fileName == "directory") {
                return result.Set(schemaSet.directory, "application/json");
            }
            if (!schemaSet.files.TryGetValue(fileName, out string content)) {
                return result.Error($"file not found: '{fileName}'");
            }
            return result.Set(content, schemaSet.contentType);
        }
        

        
        private static void HtmlHeader(StringBuilder sb, string[] titlePath, string description, SchemaHandler handler) {
            var title = string.Join(" · ", titlePath);
            var titleElements = new List<string>();
            int n       = titlePath.Length - 1;
            for (int o = 0; o < titlePath.Length; o++) {
                var titleSection    = titlePath[o];
                var indexOffset     = o == 0 ? 1 : 0;
                var link            = string.Join("", Enumerable.Repeat("../", indexOffset + n--));
                titleElements.Add($"<a href='{link}index.html'>{titleSection}</a>");
            }
            var titleLinks = string.Join(" · ", titleElements);
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='UTF-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1'>");
            sb.AppendLine($"<meta name='description' content='{description}'>");
            sb.AppendLine("<meta name='color-scheme' content='dark light'>");
            sb.AppendLine($"<link rel='icon' href='{handler.image}' type='image/x-icon'>");
            sb.AppendLine($"<title>{title}</title>");
            sb.AppendLine("<style>a {text-decoration: none; }</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body style='font-family: sans-serif'>");
            sb.AppendLine($"<h2><a href='https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema' target='_blank' rel='noopener'><img src='{handler.image}' alt='friflo JSON Fliox' /></a>");
            sb.AppendLine($"&nbsp;&nbsp;&nbsp;&nbsp;{titleLinks}</h2>");
            sb.AppendLine($"<p>{description}</p>");
        }
        
        // override intended
        private static void HtmlFooter(StringBuilder sb) {
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
        }
    }
    
    public struct Result {
        public  string  content;
        public  string  contentType;
        public  byte[]  bytes;
        public  bool    isText;
        
        public bool Set(string  content, string  contentType) {
            this.content        = content;
            this.contentType    = contentType;
            isText              = true;
            return true;
        }
        
        public bool Error(string  content) {
            this.content        = content;
            this.contentType    = "text/plain";
            isText              = true;
            return false;
        }
    }

    public sealed class SchemaSet {
        public   readonly   string                      name;
        public   readonly   string                      contentType;
        public   readonly   Dictionary<string, string>  files;
        public   readonly   string                      fullSchema;
        public   readonly   string                      directory;
        internal readonly   string                      zipName;
        private             byte[]                      zipArchive;

        public byte[] GetZipArchive (CreateZip zip) {
            if (zipArchive == null && zip != null ) {
                zipArchive = zip(files);
            }
            return zipArchive;
        }

        public SchemaSet (ObjectWriter writer, string name, string contentType, Dictionary<string, string> files, string fullSchema = null) {
            this.name           = name;
            this.contentType    = contentType;
            this.files          = files;
            this.fullSchema     = fullSchema;
            zipName             = $".{name}.zip";
            directory           = writer.Write(files.Keys.ToList());
        }
    }
}