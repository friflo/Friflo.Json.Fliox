// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;

// Allowed namespaces: .Schema.Definition, .Schema.Doc, .Schema.Utils
namespace Friflo.Json.Fliox.Schema.Language
{
    public sealed class OpenAPI
    {
        private  readonly   Generator                   generator;
        
        private OpenAPI (Generator generator) {
            this.generator  = generator;
        }
        
        public static void Generate(Generator generator) {
            var emitter = new OpenAPI(generator);
            var paths = "";
            foreach (var type in generator.types) {
                if (!type.IsSchema)
                    continue;
                var sb = new StringBuilder();
                emitter.EmitPaths(type, sb);
                paths = sb.ToString();
            }
            var api = $@"
{{
  ""openapi"": ""3.0.0"",
  ""info"": {{
    ""title"":        ""example API"",
    ""description"":  ""example description"",
    ""version"":      ""0.0.0""
  }},
  ""servers"": [
    {{
      ""url"":          ""http://localhost:8010/fliox/rest/main_db/"",
      ""description"":  ""server description""
    }}
  ],
  ""paths"": {{{paths}
  }}   
}}";
            generator.files.Add("openapi.json", api);
        }
        
        private void EmitPaths(TypeDef type, StringBuilder sb) {
            var dbContainers = generator.FindTypeDef("Friflo.Json.Fliox.Hub.DB.Cluster", "DbContainers");
            var dbContainersType = Ref (dbContainers, true, generator);
            EmitPathDatabase(dbContainersType, sb);
            foreach (var container in type.Fields) {
                EmitContainerApi(container, sb);
            }
        }
        
        private void EmitContainerApi(FieldDef container, StringBuilder sb) {
            var name = container.name;
            if (sb.Length > 0)
                sb.Append(",");
            var typeRef = Ref (container.type, true, generator);
            EmitPathContainer(name, $"/{name}", typeRef, sb);
        }
        
        private static void AppendPath(string path, string methods, StringBuilder sb) {
            sb.Append($@"
    ""{path}"": {{");
            sb.Append(methods);
            sb.Append($@"
    }}");
        }
        
        private static void EmitPathDatabase(string typeRef, StringBuilder sb) {
            var methodSb = new StringBuilder();
            EmitMethod("database", "get",    "return all database containers",
                null, new ContentRef(typeRef, false), null, methodSb);
            AppendPath("/", methodSb.ToString(), sb);
        }
        
        private static void EmitPathContainer(string container, string path, string typeRef, StringBuilder sb) {
            var methodSb = new StringBuilder();
            var getParams = new [] {
                new QueryParam("filter", "string",  false),
                new QueryParam("limit",  "integer", false)
            };
            EmitMethod(container, "get",    $"return all records in container {container}",
                null, new ContentRef(typeRef, false), getParams, methodSb);
            EmitMethod(container, "put",    $"create or update records in container {container}",
                new ContentRef(typeRef, true), new ContentText(), null, methodSb);
            EmitMethod(container, "delete", $"delete records in container {container} by id",
                null, new ContentText(), new [] { new QueryParam("ids", "string", true)}, methodSb);
            AppendPath(path, methodSb.ToString(), sb);
        }
        
        private static void EmitMethod(
            string                  tag,
            string                  method,
            string                  summary,
            Content                 request,
            Content                 response,
            ICollection<QueryParam> queryParams,
            StringBuilder sb)
        {
            if (sb.Length > 0)
                sb.Append(",");
            var querySb = new StringBuilder();
            var queryStr = "";
            if (queryParams != null) {
                foreach (var queryParam in queryParams) {
                    if (querySb.Length > 0)
                        querySb.Append(",");
                    var required = queryParam.required ? @"
            ""required"": true," : "";
                    querySb.Append($@"
          {{
            ""in"":       ""query"",
            ""name"":     ""{queryParam.name}"",
            ""schema"":   {{ ""type"": ""{queryParam.type}"" }},{required}
            ""description"": ""---""
          }}");
                }
                queryStr = $@"
        ""parameters"": [{querySb}
        ],";    
            }
            var requestStr = request == null ? "" : $@"
        ""requestBody"": {{          
          ""description"": ""---"",
          ""content"": {request.Get()}
        }},";
            var responseStr = response.Get();
            var methodStr = $@"
      ""{method}"": {{
        ""summary"":    ""{summary}"",
        ""tags"":       [""{tag}""],{queryStr}{requestStr}
        ""responses"": {{
          ""200"": {{             
            ""description"": ""OK"",
            ""content"": {responseStr}
          }}
        }}
      }}";
            sb.Append(methodStr);
        }
        
        private static string Ref(TypeDef type, bool required, Generator generator) {
            var name        = type.Name;
            var typePath    = type.Path;
            var prefix      = $"{typePath}{generator.fileExt}";
            var refType = $"\"$ref\": \"{prefix}#/definitions/{name}\"";
            if (!required)
                return $"\"oneOf\": [{{ {refType} }}, {{\"type\": \"null\"}}]";
            return refType;
        }
    }
    
    internal class QueryParam {
        internal    readonly    string  name;
        internal    readonly    string  type;
        internal    readonly    bool    required;
        
        internal QueryParam(string name, string type, bool required) {
            this.name       = name;
            this.type       = type;
            this.required   = required;
        }
    }
    
    internal abstract class Content {
        private     readonly    string  mimeType;
        
        internal Content(string mimeType) {
            this.mimeType   = mimeType;
        }
        
        internal abstract string Get(); 
    }
    
    internal class ContentText : Content {
        internal ContentText() : base ("text/plain") { }
        
        internal override string Get() {
            return @"{
              ""text/plain"": { }
            }";
        } 
    }

    internal class ContentRef : Content {
        private    readonly    string   type;
        private    readonly    bool     isArray;
        
        internal ContentRef(string type, bool isArray) : base ("application/json") {
            this.type       = type;
            this.isArray    = isArray;
        }
        
        internal override string Get() {
            var typeStr = isArray ? $@"""type"": ""array"",
                  ""items"": {{ {type} }}" : type;
            return $@"{{
              ""application/json"": {{
                ""schema"": {{
                  {typeStr}
                }}
              }}
            }}";
        }
    }

}