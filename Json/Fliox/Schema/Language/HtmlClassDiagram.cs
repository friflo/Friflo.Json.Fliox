// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
    <meta name='viewport'     content='width=device-width, initial-scale=1'>
    <meta name='description'  content='Class Diagram: Schema - {{schemaName}}'>
    <meta name='color-scheme' content='dark light'>
    <meta name='generated-by' content='{{generatedByLink}}'>
    <link rel='icon' href='../../../explorer/img/Json-Fliox-53x43.svg' type='image/x-icon'>
    <title>{{schemaName}} - Class Diagram</title>
    <script src='../../../mermaid/dist/mermaid.min.js'></script>
    <style>
        :root {
            --enum-fill:   #f8f8f8;
            --enum-stroke: #d0d0d0;
        }
        @media print {
          .hidden-print {
            display: none !important;
          }
        }
        [data-theme='dark'] {
            --enum-fill:   #3e3e3e;
            --enum-stroke: #606060;
        }
        .mermaid {            
            width: 1440px;  /** Enable zooming in Mermaid svg **/
        }
        .cssSchema > rect {
            stroke-width:2px !important;
            stroke:#ff0000 !important;
            rx: 8;
        }
        .cssEntity > rect {
            stroke-width:2px !important;
            stroke:#0000ff !important;
            rx: 8;
        }
        .cssEnum > rect {
            fill:  var(--enum-fill)   !important;
            stroke:var(--enum-stroke) !important;
        }
        .title      { margin: 0px 0px 0px 20px; height: 26px; padding: 3px 10px; display: inline-flex; box-shadow: 0px 0px  7px  7px #00000014; border-radius: 4px; }
        .diagram    { margin: 0px 20px;         height: 18px; padding: 3px 10px; display: inline-flex; box-shadow: 0px 0px 10px 10px #00000018; border-bottom-left-radius: 4px; border-bottom-right-radius: 4px;            }
        .titleH2    { color: black; font-size: 20px; background-color: #ffffff; border: 1px solid #aaa; margin-top: 10px;align-items: center; }
    </style>
</head>
<body>
    <div style='position: fixed; top: 0; font-family: sans-serif;'>
      <h2 class='title titleH2'>SCHEMA_NAME</h2>
      <div class='diagram hidden-print' style='background-color: #363bff; position: fixed; top: 0;'>
        <a href='./schema.html' style='color: white; text-decoration: none;'>Schema</a>
      </div>
    </div>
    <div id='graphDiv'></div>
    <script>
        let mermaidContent = `{{mermaidClassDiagram}}
`;
        /* const search = /\</g;
        mermaidContent = mermaidContent.replace(search, '&lt;');
        const div = document.createElement('div');
        div.classList.add('mermaid');
        div.innerHTML = mermaidContent;
        document.body.append(div);
        mermaid.initialize({ startOnLoad: true}); */
        let theme = 'default';
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            theme = 'dark';
            document.documentElement.setAttribute('data-theme', 'dark');
        }
        mermaid.mermaidAPI.initialize({ startOnLoad: false, logLevel: 4, theme: theme });
        const mermaidEl = document.getElementById('graphDiv');
        const insertSvg = function (svgCode, bindFunctions) {
            mermaidEl.innerHTML = svgCode;
        };
        mermaid.mermaidAPI.render('graph', mermaidContent, insertSvg);
    </script>
</body>
</html>
";
    }
}