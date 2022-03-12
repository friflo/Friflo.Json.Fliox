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

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    internal sealed class SchemaHandler : IRequestHandler
    {
        private   const     string                              SchemaBase = "/schema";
        internal            string                              image = "/img/Json-Fliox-53x43.svg";
        internal  readonly  CreateZip                           zip;
        private   readonly  Dictionary<string, SchemaResource>  schemas         = new Dictionary<string, SchemaResource>();
        private   readonly  List<CustomGenerator>               generators      = new List<CustomGenerator>();
        private             string                              cacheControl    = HttpHostHub.DefaultCacheControl;
        
        internal            ICollection<CustomGenerator>        Generators      => generators;

        internal SchemaHandler(CreateZip zip = null) {
            this.zip            = zip;
        }
        
        public SchemaHandler CacheControl(string cacheControl) {
            this.cacheControl   = cacheControl;
            return this;
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
            var hub         = context.hub;
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
            bool success    = schema.GetSchemaFile(schemaPath, ref result, this, hub);
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
            sepTypes    = sepTypes ?? typeSchema.GetEntityTypes().Values;
            var schema  = new SchemaResource(typeSchema, sepTypes);
            schemas.Add(name, schema);
            return schema;
        }
        
        public void AddGenerator(string type, string name, SchemaGenerator schemaGenerator) {
            if (name == null) throw new NullReferenceException(nameof(name));
            var generator = new CustomGenerator(type, name, schemaGenerator);
            generators.Add(generator);
        }
    }
    
    internal sealed class SchemaResource {
        private  readonly   TypeSchema                      typeSchema;
        private  readonly   ICollection<TypeDef>            separateTypes;
        private             Dictionary<string, SchemaModel> schemaModels;
        private  readonly   string                          schemaName;
        
        internal SchemaResource(TypeSchema typeSchema, ICollection<TypeDef> separateTypes) {
            this.schemaName     = typeSchema.RootType.Name;
            this.typeSchema     = typeSchema;
            this.separateTypes  = separateTypes;
        }

        internal bool GetSchemaFile(string path, ref Result result, SchemaHandler handler, FlioxHub hub) {
            if (typeSchema == null) {
                return result.Error("no schema attached to database");
            }
            var storeName = typeSchema.RootType.Name;
            if (schemaModels == null) {
                var generators = handler.Generators;
                schemaModels = SchemaModel.GenerateSchemaModels(typeSchema, separateTypes, generators);
            }
            if (path == "index.html") {
                var sb = new StringBuilder();
                HtmlHeader(sb, new []{"Hub", schemaName}, $"Available schemas / languages for schema <b>{storeName}</b>", handler);
                sb.AppendLine("<ul>");
                foreach (var pair in schemaModels) {
                    sb.AppendLine($"<li><a href='./{pair.Key}/index.html'>{pair.Value.label}</a></li>");
                }
                sb.AppendLine("</ul>");
                HtmlFooter(sb);
                return result.Set(sb.ToString(), "text/html");
            }
            if (path == "json-schema.json") {
                var jsonSchemaModel = schemaModels["json-schema"];
                return result.Set(jsonSchemaModel.fullSchema, jsonSchemaModel.contentType);
            }
            var schemaTypeEnd = path.IndexOf('/');
            if (schemaTypeEnd <= 0) {
                return result.Error($"invalid path:  {path}");
            }
            var schemaType = path.Substring(0, schemaTypeEnd);
            if (!schemaModels.TryGetValue(schemaType, out SchemaModel schemaModel)) {
                return result.Error($"unknown schema type: {schemaType}");
            }
            var fileName = path.Substring(schemaTypeEnd + 1);
            if (fileName == "index.html") {
                var zipFile = $"{storeName}{schemaModel.zipNameSuffix}";
                var sb = new StringBuilder();
                HtmlHeader(sb, new[]{"Hub", schemaName, schemaModel.label}, $"{schemaModel.label} schema: <b>{storeName}</b>", handler);
                sb.AppendLine($"<a href='{zipFile}'>{zipFile}</a><br/>");
                sb.AppendLine($"<a href='directory' target='_blank'>file list</a>");
                sb.AppendLine("<ul>");
                var target = schemaModel.contentType == "text/html" ? "" : " target='_blank'";
                foreach (var file in schemaModel.files.Keys) {
                    sb.AppendLine($"<li><a href='./{file}'{target}>{file}</a></li>");
                }
                sb.AppendLine("</ul>");
                HtmlFooter(sb);
                return result.Set(sb.ToString(), "text/html");
            }
            if (fileName.StartsWith(storeName) && fileName.EndsWith(schemaModel.zipNameSuffix)) {
                result.bytes        = schemaModel.GetZipArchive(handler.zip);
                if (result.bytes == null)
                    return result.Error("ZipArchive not supported (Unity)");
                result.contentType  = "application/zip";
                result.isText       = false;
                return true;
            }
            if (fileName == "directory") {
                var pool = hub.sharedEnv.Pool;
                using (var pooled = pool.ObjectMapper.Get()) {
                    var writer = pooled.instance.writer;
                    writer.Pretty = true;
                    var directory = writer.Write(schemaModel.files.Keys.ToList());
                    return result.Set(directory, "application/json");
                }
            }
            if (!schemaModel.files.TryGetValue(fileName, out string content)) {
                return result.Error($"file not found: '{fileName}'");
            }
            return result.Set(content, schemaModel.contentType);
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
            sb.AppendLine($"<link rel='icon' href='{handler.image}' width='53' height='43' type='image/x-icon'>");
            sb.AppendLine($"<title>{title}</title>");
            sb.AppendLine("<style>a {text-decoration: none; }</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body style='font-family: sans-serif'>");
            sb.AppendLine($"<h2><a href='{Generator.Link}' target='_blank' rel='noopener'><img src='{handler.image}' alt='friflo JSON Fliox' /></a>");
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
}