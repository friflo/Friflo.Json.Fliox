// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

namespace Friflo.Json.Fliox.Hub.Remote.Schema
{
    internal static class SchemaSwagger
    {
        internal static string Get(string storeName) {
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