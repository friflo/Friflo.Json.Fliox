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
                    --disc:     #2b9aa7;
                    --value:    #1d52a7;
                    --type:     #0f54d6;
                    --keyword:  #6f6f6f;
                    --bg:       #fff;
                    --selected: #eee;
                }
        body::after {
            content: '';
            background-image: url('../paint-splatter.svg'); /* from https://freesvg.org/ */
            background-repeat: repeat; background-position: 5% 5%;
            opacity: 1;
            top: 0; left: 0; bottom: 0; right: 0;
            position: absolute;
            z-index: -1;   
        }
        body    {
                    display: grid;
                    grid-template-areas: 
                        'body-head  body-head'
                        'body-nav   body-docs-border'
                        'body-nav   body-docs';
                    grid-template-columns:  300px 1fr;
                    grid-template-rows:     40px  10px 1fr;
                    grid-gap:   0px;
                    margin:     0;
                    height:     100vh;
                    width:      100vw;
                    font-family: sans-serif
                }
        .head       { overflow: hidden; }
        .nav        { overflow: auto; background: var(--bg); }
        .docs-border{ overflow: auto; background: var(--bg); }
        .docs       { overflow: auto; background: var(--bg);  padding-left: 30px; }

        .title      { margin: 6px 30px; padding: 0px 5px; display: inline-flex; background: var(--bg); }
        type        { color: var(--type) }
        keyword     { font-size: 13px; font-weight: normal; color: var(--keyword); }
        chapter     { font-size: 13px; font-weight: normal; color: var(--keyword); margin-left: 60px; }
        extends     { font-size: 13px; }
        cmd         { color: var(--value);    font-family: var(--mono-font); }
        key         { color: var(--key);      font-family: var(--mono-font); margin-left: -3px; padding: 0 2px; border-radius: 2px; border: 1px solid var(--key);   background: var(--bg); }
        refType     { color: var(--key); }
        ref         { color: var(--field);    font-family: var(--mono-font);  }        
        disc        { color: var(--field);    font-family: var(--mono-font); margin-left: -3px; padding: 0 2px; border-radius: 2px; border: 1px solid var(--field); background: var(--bg); }
        field       { color: var(--field);    font-family: var(--mono-font); }
        discriminant{ color: var(--value);    font-family: var(--mono-font); }

        desc        { margin-left: 50px; }

        .namespace  { margin-bottom: 100px; }
        ul.enum li  { margin-left: 50px; font-family: var(--mono-font); color: var(--value) }
        div.nav > ul > li > a { color: var(--keyword); font-size: 13px; }

        h3      { margin-left: 30px; margin-bottom: 5px; }
        h2.selected     { background: var(--selected); }
        h3.selected     { background: var(--selected); }


        a                               { text-decoration:  none; }
        ul                              { margin: 5px; padding-left: 8px; list-style-type: none; }
        ul > li > a:hover               { background: var(--selected); }
        ul > li > ul > li > a>div > key { font-size: 12px; }
        ul > li > ul > li > a>div > disc{ font-size: 12px; }
        ul > li > ul > li:hover         { background: var(--selected); }

        table.type                      { margin-left: 70px; }
        table.type tr td:nth-child(1)   { width: 150px; vertical-align: baseline; }
    </style>
    <script>
        var docsSelection;
        function scrollTo(id) {
            var element = undefined;
            if (id == '') {
                element = document.querySelector('.docs').firstElementChild;
            }
            if (!element) {
                element = document.getElementById(id);
            }
            if (element) {
                docsSelection?.classList.remove('selected');
                docsSelection = element;
                element.classList.add('selected');
                const parent = element.parentElement;
                parent.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            }
        }
        // Required to support browser 'go back' 
        window.addEventListener('hashchange', function(event) {
            const id = window.location.hash.substring(1);
            // console.log('hashchange', id);
            scrollTo(id);
        });
        window.addEventListener('click', function(event) {
            const path      = event.composedPath();
            const anchor    = path.find(el => el.tagName == 'A');
            if (anchor && anchor.hash) {
                event.preventDefault();
                const id = anchor.hash.substring(1);
                scrollTo(id);
                window.history.pushState(null, anchor.href, anchor.href);
            }
        });
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

<!-- ---------------------------- docs-border --------------------------- -->
<div style='grid-area: body-docs-border;' class='docs-border'></div>

<!-- ------------------------------- docs ------------------------------- -->
<div style='grid-area: body-docs;' class='docs'>
{{documentation}}
</div>

</body>
</html>
";
    }
}