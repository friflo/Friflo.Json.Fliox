// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Database.Remote
{
    public class SchemaHandler : IHttpContextHandler
    {
        private         Dictionary<string, SchemaSet>   schemas;
        public          string                          image = "/Json-Flow-53x43.svg";
        private const   string                          BasePath = "/schema/";
        
        public async Task<bool> HandleContext(HttpListenerContext context, HttpHostDatabase hostDatabase) {
            HttpListenerRequest  req  = context.Request;
            HttpListenerResponse resp = context.Response;
            if (req.HttpMethod == "GET" && req.Url.AbsolutePath.StartsWith(BasePath)) {
                var path = req.Url.AbsolutePath.Substring(BasePath.Length);
                Result result = new Result();
                GetSchemaFile(path, hostDatabase, ref result);
                byte[]  response   = Encoding.UTF8.GetBytes(result.content);
                HttpHostDatabase.SetResponseHeader(resp, result.contentType, HttpStatusCode.OK, response.Length);
                await resp.OutputStream.WriteAsync(response, 0, result.content.Length).ConfigureAwait(false);
                resp.Close();
                return true;
            }
            return false;
        }
        
        private bool GetSchemaFile(string path, HttpHostDatabase hostDatabase, ref Result result) {
            var schema = hostDatabase.local.schema;
            if (schema == null) {
                return result.Error("no schema attached to database", "text/plain");
            }
            if (schemas == null) {
                schemas = GenerateSchemas(schema.typeSchema);
            }
            if (path == "index.html") {
                var sb = new StringBuilder();
                HtmlHeader(sb, new []{"server", "schema"}, "List of generated database schemas / languages");
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
                return result.Error($"invalid path:  {path}", "text/plain");
            }
            var schemaType = path.Substring(0, schemaTypeEnd);
            if (!schemas.TryGetValue(schemaType, out SchemaSet schemaSet)) {
                return result.Error($"unknown schema type: {schemaType}", "text/plain");
            }
            var fileName = path.Substring(schemaTypeEnd + 1);
            if (fileName == "index.html") {
                var sb = new StringBuilder();
                HtmlHeader(sb, new[]{"server", "schema", schemaSet.name}, $"{schemaSet.name} files generated from the database schema");
                sb.AppendLine("<ul>");
                foreach (var file in schemaSet.files.Keys) {
                    sb.AppendLine($"<li><a href='./{file}' target='_blank'>{file}</a></li>");
                }
                sb.AppendLine("</ul>");
                HtmlFooter(sb);
                return result.Set(sb.ToString(), "text/html");
            }
            if (!schemaSet.files.TryGetValue(fileName, out string content)) {
                return result.Error("file not found", "text/plain");
            }
            return result.Set(content, schemaSet.contentType);
        }

        private static Dictionary<string, SchemaSet> GenerateSchemas(TypeSchema typeSchema) {
            var schemas             = new Dictionary<string, SchemaSet>();
            
            var entityTypes         = typeSchema.GetEntityTypes();
            var jsonOptions         = new JsonTypeOptions(typeSchema) { separateTypes = entityTypes };
            var jsonGenerator       = JsonSchemaGenerator.Generate(jsonOptions);
            var jsonSchema          = new SchemaSet ("JSON Schema", "application/json", jsonGenerator.files);
            schemas.Add("json", jsonSchema);
            
            var options             = new JsonTypeOptions(typeSchema);
            var typescriptGenerator = TypescriptGenerator.Generate(options);
            var typescriptSchema    = new SchemaSet ("Typescript", "text/plain", typescriptGenerator.files);
            schemas.Add("typescript", typescriptSchema);
            
            var csharpGenerator     = CSharpGenerator.Generate(options);
            var csharpSchema        = new SchemaSet ("C#", "text/plain", csharpGenerator.files);
            schemas.Add("csharp", csharpSchema);
            
            var kotlinGenerator     = KotlinGenerator.Generate(options);
            var kotlinSchema        = new SchemaSet ("Kotlin", "text/plain", kotlinGenerator.files);
            schemas.Add("kotlin", kotlinSchema);

            return schemas;
        }
        
        public void HtmlHeader(StringBuilder sb, string[] titlePath, string description) {
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
            sb.AppendLine($"<h2><a href='https://github.com/friflo/Friflo.Json.Flow' target='_blank' rel='noopener'><img src='{image}' alt='friflo JSON Flow' /></a>");
            sb.AppendLine($"&nbsp;&nbsp;&nbsp;&nbsp;{titleLinks}</h2>");
            sb.AppendLine($"<p>{description}</p>");
        }
        
        public static void HtmlFooter(StringBuilder sb) {
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
        }
    }
    
    public struct Result {
        public  string  content;
        public  string  contentType;
        
        public bool Set(string  content, string  contentType) {
            this.content        = content;
            this.contentType    = contentType;
            return true;
        }
        
        public bool Error(string  content, string contentType) {
            this.content        = content;
            this.contentType    = contentType;
            return false;
        }
    }

    public class SchemaSet {
        public readonly  string                      name;
        public readonly  string                      contentType;
        public readonly  Dictionary<string, string>  files;
        
        public SchemaSet (string name, string contentType, Dictionary<string, string>  files) {
            this.name           = name;
            this.contentType    = contentType;
            this.files          = files;
        }
    }
}