// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Schema
{
    public sealed partial class HtmlGenerator
    {
        private const string Template =
@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <meta name='description' content='Schema {{schemaName}}'>
    <title>{{schemaName}} Schema</title>
    <style>
        :root   {
                    --mono-font: 'Consolas', 'Courier New', Courier, monospace;
                    --field:    #9c110e;
                    --key:      #d400b8;
                    --disc:     #c9c9c9;
                    --value:    #1d52a7;
                    --type:     #0f54d6;
                }
        body    {
                    display: grid;
                    grid-template-areas: 
                        'body-head  body-head'
                        'body-nav   body-docs';
                    grid-template-columns:  300px 1fr;
                    grid-template-rows:     60px  1fr;
                    grid-gap:   0px;
                    margin:     0;
                    height:     100vh;
                    width:      100vw;
                    font-family: sans-serif
                }
        .head       { overflow: hidden; background: #f8f8f8; }
        .nav        { overflow: auto;   background: #f8f8f8;  }
        .docs       { overflow: auto;   margin-left: 30px; }

        .title      { margin-left: 30px; }
        type        { color: var(--type) }
        keyword     { font-size: 14px; font-weight: normal; opacity: 0.6; }
        extends     { font-size: 14px; }
        cmd         { color: var(--value);  font-family: var(--mono-font); }
        key         { background: var(--key);  font-size: 14px; padding: 0 1px; border-radius: 2px; color: white; }
        ref         { color: var(--key);       font-size: 14px; padding: 0 3px; border-radius: 2px; border: 1px solid var(--key); }
        disc        { background: var(--disc); font-size: 14px; padding: 0 1px; border-radius: 2px; color: white; }
        field       { color: var(--field);  font-family: var(--mono-font); }
        discriminant{ color: var(--value);  font-family: var(--mono-font); }

        desc        { margin-left: 50px; }

        .namespace  { margin-bottom: 100px; }
        ul.enum li  { margin-left: 50px; font-family: var(--mono-font); color: var(--value) }
        div.nav > ul > li > a { color: #555; font-size: 14px; }

        h3      { margin-left: 30px; margin-bottom: 5px; }


        a       { text-decoration:  none; }
        ul      { margin: 5px; padding-left: 8px; list-style-type: none; }

        table.type                      { margin-left: 30px; }
        table.type tr td:nth-child(1)   { width: 30px; vertical-align: baseline;  }
        table.type tr td:nth-child(2)   { width: 150px; vertical-align: baseline; }
    </style>
    <script>
        // Required to support browser 'go back' 
        window.addEventListener('hashchange', function(event) {
            const id = window.location.hash.substring(1);                        
            console.log('hashchange', id);
            if (id == '') {
                var docsElement = document.querySelector('.docs');
                docsElement.firstElementChild.scrollIntoView();
            }
            const element = document.getElementById(id);
            if (element) {
                element.scrollIntoView();
            }
        }, false);
    </script>
</head>

<body>

<!-- ------------------------------- head ------------------------------- -->
<div style='grid-area: body-head;' class='head'>
<h2 class='title'>{{schemaName}} schema</h2>
</div>

<!-- ------------------------------- nav ------------------------------- -->
<div style='grid-area: body-nav;' class='nav'>
{{navigation}}
</div>

<!-- ------------------------------- docs ------------------------------- -->
<div style='grid-area: body-docs;' class='docs'>
{{documentation}}
</div>

</body>
</html>
";
    }
}