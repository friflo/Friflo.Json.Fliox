// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Language;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    internal sealed class SchemaResource {
        private  readonly   string                              database;
        private  readonly   TypeSchema                          typeSchema;
        private  readonly   ICollection<TypeDef>                separateTypes;
        /// key: <see cref="SchemaModel.type"/>  (csharp, typescript, ...)
        private             Dictionary<string, ModelResource>   modelResources;
        private  readonly   string                              schemaName;

        public   override   string                              ToString() => $"{database} - {schemaName}";

        internal SchemaResource(string database, TypeSchema typeSchema, ICollection<TypeDef> separateTypes) {
            this.database       = database;
            this.schemaName     = typeSchema.RootType.Name;
            this.typeSchema     = typeSchema;
            this.separateTypes  = separateTypes;
        }

        internal Result GetSchemaFile(string path, SchemaHandler handler, RequestContext context) {
            if (typeSchema == null) {
                return Result.Error("no schema attached to database");
            }
            var storeName = typeSchema.RootType.Name;
            if (modelResources == null) {
                var generators      = handler.Generators;
                var databaseUrl     = $"/fliox/rest/{database}";
                var schemaModels    = SchemaModel.GenerateSchemaModels(typeSchema, separateTypes, generators, databaseUrl);
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
                return Result.Success(sb.ToString(), "text/html");
            }
            if (path == "json-schema.json") {
                var jsonSchemaModel = modelResources["json-schema"];
                return Result.Success(jsonSchemaModel.fullSchema, jsonSchemaModel.schemaModel.contentType);
            }
            if (path == "open-api.html") {
                var swaggerIndex = GetSwaggerIndex(storeName);
                return Result.Success(swaggerIndex, "text/html");
            }
            var schemaTypeEnd = path.IndexOf('/');
            if (schemaTypeEnd <= 0) {
                return Result.Error($"invalid path:  {path}");
            }
            var schemaType = path.Substring(0, schemaTypeEnd);
            if (!modelResources.TryGetValue(schemaType, out ModelResource modelResource)) {
                return Result.Error($"unknown schema type: {schemaType}");
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
                return Result.Success(sb.ToString(), "text/html");
            }
            if (fileName.StartsWith(storeName) && fileName.EndsWith(modelResource.zipNameSuffix)) {
                var bytes = new JsonValue(modelResource.GetZipArchive(handler.zip));
                if (bytes.IsNull())
                    return Result.Error("ZipArchive not supported (Unity)");
                return Result.Success(bytes, "application/zip");
            }
            if (fileName == "directory") {
                var pool = context.Pool;
                using (var pooled = pool.ObjectMapper.Get()) {
                    var writer      = pooled.instance.writer;
                    writer.Pretty   = true;
                    var directory   = writer.Write(files.Keys.ToList());
                    return Result.Success(directory, "application/json");
                }
            }
            if (!files.TryGetValue(fileName, out string content)) {
                return Result.Error($"file not found: '{fileName}'");
            }
            return Result.Success(content, schemaModel.contentType);
        }

        private static void HtmlHeader(StringBuilder sb, string[] titlePath, string description, SchemaHandler handler) {
            var title           = string.Join(" · ", titlePath);
            var titleElements   = new List<string>();
            int n               = titlePath.Length - 1;
            for (int o = 0; o < titlePath.Length; o++) {
                var titleSection    = titlePath[o];
                var indexOffset     = o == 0 ? 1 : 0;
                var target          = o == 0 ? "target='_blank' rel='noopener' " : "";
                var path            = o == 0 ? "" : "index.html";
                var link            = string.Join("", Enumerable.Repeat("../", indexOffset + n--));
                titleElements.Add($"<a {target}href='{link}{path}'>{titleSection}</a>");
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
        
        private static  JsonValue GetFullJsonSchema(SchemaModel schemaModel, RequestContext context) {
            if (schemaModel.type != "json-schema")
                return default;
            var jsonSchemaMap = new Dictionary<string, JsonValue>(schemaModel.files.Count);
            foreach (var pair in schemaModel.files) {
                var file = pair.Value;
                jsonSchemaMap.Add(pair.Key, new JsonValue(file));
            }
            using (var pooled = context.Pool.ObjectMapper.Get()) {
                var writer = pooled.instance.writer;
                return new JsonValue(writer.WriteAsArray(jsonSchemaMap));
            }
        }
        
        private static string GetSwaggerIndex(string storeName) {
            var relBase = "../../swagger";
            // --------------- swagger-initializer.js ---------------
            var swaggerInitializer = $@"
const loc       = window.location;
const path      = loc.pathname.substring(0, loc.pathname.lastIndexOf('/') + 1);
const apiPath   = loc.origin + path + 'json-schema/openapi.json';

window.onload = function() {{
  window.ui = SwaggerUIBundle({{
    url:            apiPath,
    dom_id:         '#swagger-ui',
    deepLinking:    true,
    docExpansion:   'none',
    validatorUrl:   null, // disable request to https://validator.swagger.io/validator sending the OpenAPI schema
    presets: [
      SwaggerUIBundle.presets.apis,
      SwaggerUIStandalonePreset
    ],
    plugins: [
      SwaggerUIBundle.plugins.DownloadUrl
    ],
    layout:         'StandaloneLayout'
  }});
}};";
            // --------------- index.html ---------------
            return $@"<!DOCTYPE html>
<html lang='en'>
  <head>
    <meta charset='UTF-8'>
    <title>{storeName} - Swagger UI</title>
    <link rel='stylesheet'  type='text/css'     href='{relBase}/swagger-ui.css' />
    <link rel='stylesheet'  type='text/css'     href='{relBase}/index.css' />
    <link rel='icon'        type='image/png'    href='{relBase}/favicon-32x32.png' sizes='32x32' />
    <link rel='icon'        type='image/png'    href='{relBase}/favicon-16x16.png' sizes='16x16' />
  </head>

  <body>
    <div id='swagger-ui'></div>
    <script src='{relBase}/swagger-ui-bundle.js'            charset='UTF-8'> </script>
    <script src='{relBase}/swagger-ui-standalone-preset.js' charset='UTF-8'> </script>
<!--<script src='{relBase}/swagger-initializer.js'          charset='UTF-8'> </script> -->
    <script>{swaggerInitializer}
    </script>
    <style>
        div.renderedMarkdown * > a {{ text-decoration:  none; font-weight: bold; }}
    </style>
  </body>
</html>";
        }
    }

}