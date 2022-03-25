// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Language;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    public delegate byte[] CreateZip(IDictionary<string, string> files);

    internal sealed class SchemaHandler : IRequestHandler
    {
        private   const     string                              SchemaBase = "/schema";
        internal            string                              image = "img/Json-Fliox-53x43.svg";
        internal  readonly  CreateZip                           zip;
        private   readonly  Dictionary<string, SchemaResource>  schemas         = new Dictionary<string, SchemaResource>();
        private   readonly  List<CustomGenerator>               generators      = new List<CustomGenerator>();
        private             string                              cacheControl    = HttpHostHub.DefaultCacheControl;
        
        internal            ICollection<CustomGenerator>        Generators      => generators;

        internal SchemaHandler() {
            this.zip = ZipUtils.Zip;
        }
        
        public SchemaHandler CacheControl(string cacheControl) {
            this.cacheControl   = cacheControl;
            return this;
        }
        
        public bool IsMatch(RequestContext context) {
            if (context.method != "GET")
                return false;
            return RequestContext.IsBasePath(SchemaBase, context.route);
        }
        
        public Task HandleRequest(RequestContext context) {
            if (context.route.Length == SchemaBase.Length) {
                context.WriteError("invalid schema path", "missing database", 400);
                return Task.CompletedTask;
            }
            var hub         = context.hub;
            var route       = context.route.Substring(SchemaBase.Length + 1);
            var firstSlash  = route.IndexOf('/');
            var name        = firstSlash == -1 ? route : route.Substring(0, firstSlash);
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
            var schemaPath  = route.Substring(firstSlash + 1);
            Result result   = new Result();
            bool success    = schema.GetSchemaFile(schemaPath, ref result, this, context);
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
        
        internal void AddGenerator(string type, string name, SchemaGenerator schemaGenerator) {
            if (name == null) throw new NullReferenceException(nameof(name));
            var generator = new CustomGenerator(type, name, schemaGenerator);
            generators.Add(generator);
        }
    }
    
    internal class ModelResource {
        internal  readonly  SchemaModel     schemaModel;
        internal  readonly  string          zipNameSuffix;  // .csharp.zip, json-schema.zip, ...
        private             byte[]          zipArchive;
        internal  readonly  string          fullSchema;

        public    override  string          ToString() => schemaModel.type;

        internal ModelResource(SchemaModel schemaModel, string fullSchema) {
            this.schemaModel    = schemaModel;
            this.fullSchema     = fullSchema;
            zipNameSuffix       = $".{schemaModel.type}.zip";
        }
        
        internal byte[] GetZipArchive (CreateZip zip) {
            if (zipArchive == null && zip != null ) {
                zipArchive = zip(schemaModel.files);
            }
            return zipArchive;
        }
    }
    
    internal sealed class SchemaResource {
        private  readonly   TypeSchema                          typeSchema;
        private  readonly   ICollection<TypeDef>                separateTypes;
        /// key: <see cref="SchemaModel.type"/>  (csharp, typescript, ...)
        private             Dictionary<string, ModelResource>   modelResources;
        private  readonly   string                              schemaName;
        
        internal SchemaResource(TypeSchema typeSchema, ICollection<TypeDef> separateTypes) {
            this.schemaName     = typeSchema.RootType.Name;
            this.typeSchema     = typeSchema;
            this.separateTypes  = separateTypes;
        }

        internal bool GetSchemaFile(string path, ref Result result, SchemaHandler handler, RequestContext context) {
            if (typeSchema == null) {
                return result.Error("no schema attached to database");
            }
            var storeName = typeSchema.RootType.Name;
            if (modelResources == null) {
                var generators      = handler.Generators;
                var schemaModels    = SchemaModel.GenerateSchemaModels(typeSchema, separateTypes, generators);
                modelResources      = new Dictionary<string, ModelResource>(schemaModels.Count);
                foreach (var model in schemaModels) {
                    var fullSchema  = GetFullJsonSchema(model, context);
                    var value       = new ModelResource(model, fullSchema);
                    modelResources.Add(model.type, value);
                }
            }
            if (path == "index.html") {
                var sb = new StringBuilder();
                HtmlHeader(sb, new []{"Hub", schemaName}, $"Available schemas / languages for schema <b>{storeName}</b>", handler);
                sb.AppendLine("<ul>");
                foreach (var pair in modelResources) {
                    var model = pair.Value.schemaModel;
                    sb.AppendLine($"<li><a href='./{pair.Key}/index.html'>{model.label}</a></li>");
                }
                sb.AppendLine("</ul>");
                HtmlFooter(sb);
                return result.Set(sb.ToString(), "text/html");
            }
            if (path == "json-schema.json") {
                var jsonSchemaModel = modelResources["json-schema"];
                return result.Set(jsonSchemaModel.fullSchema, jsonSchemaModel.schemaModel.contentType);
            }
            var schemaTypeEnd = path.IndexOf('/');
            if (schemaTypeEnd <= 0) {
                return result.Error($"invalid path:  {path}");
            }
            var schemaType = path.Substring(0, schemaTypeEnd);
            if (!modelResources.TryGetValue(schemaType, out ModelResource modelResource)) {
                return result.Error($"unknown schema type: {schemaType}");
            }
            var schemaModel = modelResource.schemaModel;
            var files       = schemaModel.files;
            var fileName    = path.Substring(schemaTypeEnd + 1);
            if (fileName == "index.html") {
                var zipFile = $"{storeName}{modelResource.zipNameSuffix}";
                var sb = new StringBuilder();
                HtmlHeader(sb, new[]{"Hub", schemaName, schemaModel.label}, $"{schemaModel.label} schema: <b>{storeName}</b>", handler);
                sb.AppendLine($"<a href='{zipFile}'>{zipFile}</a><br/>");
                sb.AppendLine($"<a href='directory' target='_blank'>file list</a>");
                sb.AppendLine("<ul>");
                var target = schemaModel.contentType == "text/html" ? "" : " target='_blank'";
                foreach (var file in files.Keys) {
                    sb.AppendLine($"<li><a href='./{file}'{target}>{file}</a></li>");
                }
                sb.AppendLine("</ul>");
                HtmlFooter(sb);
                return result.Set(sb.ToString(), "text/html");
            }
            if (fileName.StartsWith(storeName) && fileName.EndsWith(modelResource.zipNameSuffix)) {
                result.bytes        = modelResource.GetZipArchive(handler.zip);
                if (result.bytes == null)
                    return result.Error("ZipArchive not supported (Unity)");
                result.contentType  = "application/zip";
                result.isText       = false;
                return true;
            }
            if (fileName == "directory") {
                var pool = context.Pool;
                using (var pooled = pool.ObjectMapper.Get()) {
                    var writer      = pooled.instance.writer;
                    writer.Pretty   = true;
                    var directory   = writer.Write(files.Keys.ToList());
                    return result.Set(directory, "application/json");
                }
            }
            if (!files.TryGetValue(fileName, out string content)) {
                return result.Error($"file not found: '{fileName}'");
            }
            return result.Set(content, schemaModel.contentType);
        }

        private static void HtmlHeader(StringBuilder sb, string[] titlePath, string description, SchemaHandler handler) {
            var title           = string.Join(" · ", titlePath);
            var titleElements   = new List<string>();
            int n               = titlePath.Length - 1;
            for (int o = 0; o < titlePath.Length; o++) {
                var titleSection    = titlePath[o];
                var indexOffset     = o == 0 ? 1 : 0;
                var link            = string.Join("", Enumerable.Repeat("../", indexOffset + n--));
                titleElements.Add($"<a href='{link}index.html'>{titleSection}</a>");
            }
            var relativeBase    = string.Join("", Enumerable.Repeat("../", titlePath.Length));
            var imageUrl        = relativeBase + handler.image;
            
            var titleLinks = string.Join(" · ", titleElements);
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='UTF-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1'>");
            sb.AppendLine($"<meta name='description' content='{description}'>");
            sb.AppendLine("<meta name='color-scheme' content='dark light'>");
            sb.AppendLine($"<link rel='icon' href='{imageUrl}' width='53' height='43' type='image/x-icon'>");
            sb.AppendLine($"<title>{title}</title>");
            sb.AppendLine("<style>a {text-decoration: none; }</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body style='font-family: sans-serif'>");
            sb.AppendLine($"<h2><a href='{Generator.Link}' target='_blank' rel='noopener'><img src='{imageUrl}' alt='friflo JSON Fliox' /></a>");
            sb.AppendLine($"&nbsp;&nbsp;&nbsp;&nbsp;{titleLinks}</h2>");
            sb.AppendLine($"<p>{description}</p>");
        }
        
        // override intended
        private static void HtmlFooter(StringBuilder sb) {
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
        }
        
        private static  string GetFullJsonSchema(SchemaModel schemaModel, RequestContext context) {
            if (schemaModel.type != "json-schema")
                return null;
            var jsonSchemaMap = new Dictionary<string, JsonValue>(schemaModel.files.Count);
            foreach (var pair in schemaModel.files) {
                var file = pair.Value;
                jsonSchemaMap.Add(pair.Key, new JsonValue(file));
            }
            using (var pooled = context.Pool.ObjectMapper.Get()) {
                var writer = pooled.instance.writer;
                return writer.Write(jsonSchemaMap);
            }
        }
    }
    
    internal struct Result {
        internal    string  content;
        internal    string  contentType;
        internal    byte[]  bytes;
        internal    bool    isText;
        
        internal  bool Set(string  content, string  contentType) {
            this.content        = content;
            this.contentType    = contentType;
            isText              = true;
            return true;
        }
        
        internal bool Error(string  content) {
            this.content        = content;
            this.contentType    = "text/plain";
            isText              = true;
            return false;
        }
    }
}