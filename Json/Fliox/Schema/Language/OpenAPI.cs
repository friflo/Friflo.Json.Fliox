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
        
        private string GetTypeRef (string @namespace, string name) {
            var typeDef = generator.FindTypeDef(@namespace, name);
            return Ref (typeDef, true, generator);
        }
        
        private void EmitPaths(TypeDef type, StringBuilder sb) {
            var dbContainers     = GetTypeRef("Friflo.Json.Fliox.Hub.DB.Cluster", "DbContainers");
            EmitPathRoot("database", "/",                   "return all database containers", dbContainers, null, sb);

        //  EmitMessages("command", type.Commands, sb);
        //  EmitMessages("message", type.Messages, sb);
            foreach (var container in type.Fields) {
                EmitContainerApi(container, sb);
            }
        }
        
        private void EmitMessages(string messageType, IReadOnlyList<MessageDef> messages, StringBuilder sb) {
            if (messages == null)
                return;
            foreach (var message in messages) {
                EmitMessage(message, messageType, sb);
            }
        }
        
        private void EmitMessage(MessageDef type, string messageType, StringBuilder sb) {
            var queryParams= new [] { new Parameter("query", "param", "string", false) };
            var doc = type.doc ?? "";
            var tag = messageType == "command" ? type.name.StartsWith("std.") ? "standard commands": "commands" : "messages";
            EmitPathRoot(tag, $"/?{messageType}={type.name}",  doc, "" , queryParams, sb);
        }
        
        private void EmitContainerApi(FieldDef container, StringBuilder sb) {
            var name    = container.name;
            var typeRef = Ref (container.type, true, generator);
            EmitPathContainer       (name, $"/{name}",          typeRef, sb);
            EmitPathContainerEntity (name, $"/{name}/{{id}}",   typeRef, sb);
        }
        
        private static void AppendPath(string path, string methods, StringBuilder sb) {
            if (sb.Length > 0)
                sb.Append(",");
            sb.Append($@"
    ""{path}"": {{");
            sb.Append(methods);
            sb.Append($@"
    }}");
        }
        
        private static void EmitPathRoot(
            string                  tag,
            string                  path,
            string                  summary,
            string                  typeRef,
            ICollection<Parameter>  queryParams,
            StringBuilder           sb)
        {
            var methodSb = new StringBuilder();
            EmitMethod(tag, "get",    summary,
                null, new ContentRef(typeRef, false), queryParams, methodSb);
            AppendPath(path, methodSb.ToString(), sb);
        }
        
        private static void EmitPathContainer(string container, string path, string typeRef, StringBuilder sb) {
            var methodSb = new StringBuilder();
            var getParams = new [] {
                new Parameter("query", "filter", "string",  false),
                new Parameter("query", "limit",  "integer", false)
            };
            EmitMethod(container, "get",    $"return all records in container {container}",
                null, new ContentRef(typeRef, false), getParams, methodSb);
            EmitMethod(container, "put",    $"create or update records in container {container}",
                new ContentRef(typeRef, true), new ContentText(), null, methodSb);
            EmitMethod(container, "delete", $"delete records in container {container} by id",
                null, new ContentText(), new [] { new Parameter("query", "ids", "string", true)}, methodSb);
            AppendPath(path, methodSb.ToString(), sb);
        }
        
        private static void EmitPathContainerEntity(string container, string path, string typeRef, StringBuilder sb) {
            var methodSb = new StringBuilder();
            EmitMethod(container, "get",    $"return a single record from container {container}",
                null, new ContentRef(typeRef, false), new [] { new Parameter("path", "id", "string", true)}, methodSb);
            AppendPath(path, methodSb.ToString(), sb);
        }
        
        private static void EmitMethod(
            string                  tag,
            string                  method,
            string                  summary,
            Content                 request,
            Content                 response,
            ICollection<Parameter>  queryParams,
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
                    var param = queryParam.Get();
                    querySb.Append(param);
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
    
    internal class Parameter {
        private     readonly    string  paramType;
        private     readonly    string  name;
        private     readonly    string  type;
        private     readonly    bool    required;
        
        internal Parameter(string paramType, string name, string type, bool required) {
            this.paramType  = paramType;
            this.name       = name;
            this.type       = type;
            this.required   = required;
        }
        
        internal string Get() {
            var requiredStr = required ? @"
            ""required"": true," : "";
            return $@"
          {{
            ""in"":       ""{paramType}"",
            ""name"":     ""{name}"",
            ""schema"":   {{ ""type"": ""{type}"" }},{requiredStr}
            ""description"": ""---""
          }}";
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