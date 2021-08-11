// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Database.Remote
{
    public class SchemaHandler : IHttpContextHandler
    {
        private Dictionary<string, SchemaSet> schemas;

        private const string BasePath = "/schema/";
        
        public async Task<bool> HandleContext(HttpListenerContext context, HttpHostDatabase hostDatabase) {
            HttpListenerRequest  req  = context.Request;
            HttpListenerResponse resp = context.Response;
            if (req.HttpMethod == "GET" && req.Url.AbsolutePath.StartsWith(BasePath)) {
                var path = req.Url.AbsolutePath.Substring(BasePath.Length); 
                GetSchemaFile(path, hostDatabase, out string content, out string contentType);
                byte[]  response   = Encoding.UTF8.GetBytes(content);
                HttpHostDatabase.SetResponseHeader(resp, contentType, HttpStatusCode.OK, response.Length);
                await resp.OutputStream.WriteAsync(response, 0, content.Length).ConfigureAwait(false);
                resp.Close();
                return true;
            }
            return false;
        }
        
        private void GetSchemaFile(string path, HttpHostDatabase hostDatabase, out string content, out string contentType) {
            var schema = hostDatabase.local.schema;
            if (schema == null) {
                content     = "no schema attached to database";
                contentType = "text/plain";
                return;
            }
            if (schemas == null) {
                schemas = GenerateSchemas(schema.typeSchema);
            }
            if (path == "") {
                var sb = new StringBuilder();
                foreach (var type in schemas.Keys) {
                    sb.AppendLine($"<a href=\"./{type}/\">{type}</a><br>");
                }
                content = sb.ToString();
                contentType = "text/html";
                return;
            }
            var schemaTypeEnd = path.IndexOf('/');
            var schemaType = path.Substring(0, schemaTypeEnd);
            if (!schemas.TryGetValue(schemaType, out SchemaSet schemaSet)) {
                content     = $"unknown schema type: {schemaType}";
                contentType = "text/plain";
                return;
            }
            var fileName = path.Substring(schemaTypeEnd + 1);
            if (fileName == "") {
                var sb = new StringBuilder();
                foreach (var file in schemaSet.files.Keys) {
                    sb.AppendLine($"<a href=\"./{file}\">{file}</a><br>");
                }
                content = sb.ToString();
                contentType = "text/html";
                return;
            }
            if (!schemaSet.files.TryGetValue(fileName, out content)) {
                content     = "file not found";
                contentType = "text/plain";
                return;
            }
            contentType = schemaSet.contentType;
        }

        private static Dictionary<string, SchemaSet> GenerateSchemas(TypeSchema typeSchema) {
            var schemas             = new Dictionary<string, SchemaSet>();
            
            var options             = new JsonTypeOptions(typeSchema);
            var jsonGenerator       = JsonSchemaGenerator.Generate(options);
            var jsonSchema          = new SchemaSet ("application/json", jsonGenerator.files);
            schemas.Add("json", jsonSchema);
            
            var typescriptGenerator = TypescriptGenerator.Generate(options);
            var typescriptSchema    = new SchemaSet ("text/plain", typescriptGenerator.files);
            schemas.Add("typescript", typescriptSchema);
            
            var csharpGenerator     = CSharpGenerator.Generate(options);
            var csharpSchema        = new SchemaSet ("text/plain", csharpGenerator.files);
            schemas.Add("csharp", csharpSchema);
            
            var kotlinGenerator     = KotlinGenerator.Generate(options);
            var kotlinSchema        = new SchemaSet ("text/plain", kotlinGenerator.files);
            schemas.Add("kotlin", kotlinSchema);

            return schemas;
        }
    }
    
    public class SchemaSet
    {
        public readonly  string                      contentType;
        public readonly  Dictionary<string, string>  files;
        
        public SchemaSet (string contentType, Dictionary<string, string>  files) {
            this.contentType    = contentType;
            this.files          = files;
        }
    }
}