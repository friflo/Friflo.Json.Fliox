// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.Definition;

// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Flow.Database.Remote
{
    public delegate byte[] CreateZip(Dictionary<string, string> files);
    
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class SchemaHandler : IHttpContextHandler
    {
        public              string                          image = "/Json-Flow-53x43.svg";
        public  readonly    CreateZip                       zip;
        
        private             Dictionary<string, SchemaSet>   schemas;
        private const       string                          BasePath = "/schema/";
        
        public SchemaHandler(CreateZip zip = null) {
            this.zip = zip;
        }
        
        public async Task<bool> HandleContext(HttpListenerContext context, HttpHostDatabase hostDatabase) {
            HttpListenerRequest  req  = context.Request;
            HttpListenerResponse resp = context.Response;
            if (req.HttpMethod == "GET" && req.Url.AbsolutePath.StartsWith(BasePath)) {
                var path = req.Url.AbsolutePath.Substring(BasePath.Length);
                Result result = new Result();
                bool success = GetSchemaFile(path, hostDatabase.local.schema, ref result);
                byte[]  response;
                if (result.isText) {
                    response    = Encoding.UTF8.GetBytes(result.content);
                } else {
                    response    = result.bytes;
                }
                HttpStatusCode status = success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                HttpHostDatabase.SetResponseHeader(resp, result.contentType, status, response.Length);
                await resp.OutputStream.WriteAsync(response, 0, response.Length).ConfigureAwait(false);
                resp.Close();
                return true;
            }
            return false;
        }
        
        public bool GetSchemaFile(string path, DatabaseSchema schema, ref Result result) {
            if (schema == null) {
                return result.Error("no schema attached to database");
            }
            var storeName = schema.typeSchema.RootType.Name;
            if (schemas == null) {
                using (var writer = new ObjectWriter(new TypeStore())) {
                    writer.Pretty = true;
                    schemas = GenerateSchemas(writer, schema.typeSchema);
                }
            }
            if (path == "index.html") {
                var sb = new StringBuilder();
                HtmlHeader(sb, new []{"server", "schema"}, $"Available schemas / languages for database schema <b>{storeName}</b>");
                sb.AppendLine("<ul>");
                foreach (var pair in schemas) {
                    sb.AppendLine($"<li><a href='./{pair.Key}/index.html'>{pair.Value.name}</a></li>");
                }
                sb.AppendLine("</ul>");
                HtmlFooter(sb);
                return result.Set(sb.ToString(), "text/html");
            }
            var schemaTypeEnd = path.IndexOf('/');
            if (schemaTypeEnd <= 0) {
                return result.Error($"invalid path:  {path}");
            }
            var schemaType = path.Substring(0, schemaTypeEnd);
            if (!schemas.TryGetValue(schemaType, out SchemaSet schemaSet)) {
                return result.Error($"unknown schema type: {schemaType}");
            }
            var zipFile = $"{storeName}.{schemaType}.zip";
            var fileName = path.Substring(schemaTypeEnd + 1);
            if (fileName == "index.html") {
                var sb = new StringBuilder();
                HtmlHeader(sb, new[]{"server", "schema", schemaSet.name}, $"{schemaSet.name} files for database schema <b>{storeName}</b>");
                sb.AppendLine($"<a href='{zipFile}'>{zipFile}</a><br/>");
                sb.AppendLine($"<a href='directory' target='_blank'>{storeName} {schemaSet.name} files</a>");
                sb.AppendLine("<ul>");
                foreach (var file in schemaSet.files.Keys) {
                    sb.AppendLine($"<li><a href='./{file}' target='_blank'>{file}</a></li>");
                }
                sb.AppendLine("</ul>");
                HtmlFooter(sb);
                return result.Set(sb.ToString(), "text/html");
            }
            if (fileName == zipFile) {
                result.bytes        = schemaSet.zipArchive;
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
                return result.Error("file not found");
            }
            return result.Set(content, schemaSet.contentType);
        }
        
        // override intended
        public virtual Dictionary<string, SchemaSet> GenerateSchemas(ObjectWriter writer, TypeSchema typeSchema) {
            var result              = new Dictionary<string, SchemaSet>();
            
            var entityTypes         = typeSchema.GetEntityTypes();
            var jsonOptions         = new JsonTypeOptions(typeSchema) { separateTypes = entityTypes };
            var jsonGenerator       = JsonSchemaGenerator.Generate(jsonOptions);
            var jsonSchema          = new SchemaSet (writer, zip, "JSON Schema", "application/json", jsonGenerator.files);
            result.Add("json-schema",  jsonSchema);
            
            var options             = new JsonTypeOptions(typeSchema);
            var typescriptGenerator = TypescriptGenerator.Generate(options);
            var typescriptSchema    = new SchemaSet (writer, zip, "Typescript",  "text/plain",       typescriptGenerator.files);
            result.Add("typescript",   typescriptSchema);
            
            var csharpGenerator     = CSharpGenerator.Generate(options);
            var csharpSchema        = new SchemaSet (writer, zip, "C#",          "text/plain",       csharpGenerator.files);
            result.Add("csharp",       csharpSchema);
            
            var kotlinGenerator     = KotlinGenerator.Generate(options);
            var kotlinSchema        = new SchemaSet (writer, zip, "Kotlin",      "text/plain",       kotlinGenerator.files);
            result.Add("kotlin",       kotlinSchema);
            return result;
        }
        
        // override intended
        public virtual void HtmlHeader(StringBuilder sb, string[] titlePath, string description) {
            var title = string.Join(" - ", titlePath);
            var titleElements = new List<string>();
            int n = titlePath.Length -1;
            foreach (var name in titlePath) {
                var link = string.Join("", Enumerable.Repeat("../", n--));
                titleElements.Add($"<a href='{link}index.html'>{name}</a>");
            }
            var titleLinks = string.Join(" - ", titleElements);
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='UTF-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1'>");
            sb.AppendLine($"<meta name='description' content='{description}'>");
            sb.AppendLine("<meta name='color-scheme' content='dark light'>");
            sb.AppendLine($"<link rel='icon' href='{image}' type='image/x-icon'>");
            sb.AppendLine($"<title>{title}</title>");
            sb.AppendLine("<style>a {text-decoration: none; }</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body style='font-family: sans-serif'>");
            sb.AppendLine($"<h2><a href='https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema' target='_blank' rel='noopener'><img src='{image}' alt='friflo JSON Flow' /></a>");
            sb.AppendLine($"&nbsp;&nbsp;&nbsp;&nbsp;{titleLinks}</h2>");
            sb.AppendLine($"<p>{description}</p>");
        }
        
        // override intended
        public virtual void HtmlFooter(StringBuilder sb) {
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

    public class SchemaSet {
        public readonly  string                      name;
        public readonly  string                      contentType;
        public readonly  Dictionary<string, string>  files;
        public readonly  byte[]                      zipArchive;
        public readonly  string                      directory;
        
        public SchemaSet (ObjectWriter writer, CreateZip zip, string name, string contentType, Dictionary<string, string> files) {
            this.name           = name;
            this.contentType    = contentType;
            this.files          = files;
            zipArchive          = zip?.Invoke(files);
            directory           = writer.Write(files.Keys.ToList());
        }
    }
}