// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Schema.Language
{
    public sealed partial class HtmlGenerator
    {
        private const string Mermaid =
@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <meta name='description' content='Schema - {{schemaName}}'>
    <link rel='icon' href='../../../explorer/img/Json-Fliox-53x43.svg' type='image/x-icon'>
    <title>{{schemaName}} - ER Diagram</title>
    <script src='https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js'></script>
</head>
<body>
    <div class='mermaid'>
{{mermaidClassDiagram}}
    </div>
    <script>
        mermaid.initialize({ startOnLoad: true});
    </script>
</body>
</html>
";
    }
}