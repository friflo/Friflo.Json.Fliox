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
    <style>
        .mermaid {            
            width: 1600px;  /** Enable zooming in Mermaid svg **/
        }
    </style>
</head>
<body>
    <script>
        let mermaidContent = `{{mermaidClassDiagram}}
`;
        const search = /\</g;
        mermaidContent = mermaidContent.replace(search, '&lt;');
        const div = document.createElement('div');
        div.classList.add('mermaid');
        div.innerHTML = mermaidContent;
        document.body.append(div);
        mermaid.initialize({ startOnLoad: true});
    </script>
</body>
</html>
";
    }
}