// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using Friflo.Json.Fliox.Schema.Definition;

// Allowed namespaces: .Schema.Definition, .Schema.Doc, .Schema.Utils
namespace Friflo.Json.Fliox.Schema.Language
{
    public sealed class OpenApi3
    {
        private  readonly   Generator                   generator;
        
        private OpenApi3 (Generator generator) {
            this.generator  = generator;
        }
        
        public static void Generate(Generator generator) {
            var emitter = new OpenApi3(generator);
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
            foreach (var container in type.Fields) {
                EmitContainerApi(container, sb);
            }
        }
        
        private void EmitContainerApi(FieldDef container, StringBuilder sb) {
            var name = container.name;
            if (sb.Length > 0)
                sb.Append(",");
            var typeRef = Ref (container.type, true, generator);
            EmitPath(name, $"/{name}", "get", typeRef, sb);
        }
        
        private void EmitPath(string tag, string path, string method, string typeRef, StringBuilder sb) {
            var spec = $@"
    ""{path}"": {{
      ""{method}"": {{
        ""summary"":    ""return all records in articles"",
        ""tags"":       [""{tag}""],
        ""responses"": {{
          ""200"": {{             
            ""description"": ""OK"",
            ""content"": {{
              ""application/json"": {{
                ""schema"": {{
                  {typeRef}
                }}
              }}
            }}
          }}
        }}
      }}
    }}";
            sb.Append(spec);
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
}