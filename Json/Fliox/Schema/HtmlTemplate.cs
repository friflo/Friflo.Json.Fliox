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
    <meta name='description' content='Schema - {{schemaName}}'>
    <meta name='color-scheme' content='dark light'>
    <title>{{schemaName}} - schema</title>
    <style>
        :root {
            --mono-font: 'Consolas', 'Courier New', Courier, monospace;
            --field:    #9c110e;    --field-border:     #9c110e80;
            --key:      #d400b8;    --key-border:       #d400b880;
            --disc:     #2b9aa7;
            --value:    #1d52a7;
            --type:     #0f54d6;
            --keyword:  #6f6f6f;
            --color:    #000;       --highlight:        #008;
            --bg:       #fff;       --bg-highlight:     #e0e0ff;
            --bg2:      #fff;
            --bg-border:#ccc;
            --selected: #f0f0f0;    --selected-shadow:  #f0f0f0;    --selected-offset: 0px; --selected-radius: 1px;
            --link:     #0000ee;
            --visited:  #551a8b;
        }
        [data-theme='dark'] {
            --field:    #a8ddfc;     --field-border:    #a8ddfca0;
            --key:      #e64bd1;     --key-border:      #e64bd1a0;
            --disc:     #2b9aa7;
            --value:    #c7907a;
            --type:     #5a7eff;
            --keyword:  #bbb;
            --color:    #fff;       --highlight:        #fff;
            --bg:       #000;       --bg-highlight:     #4040c0;
            --bg2:      #000;
            --bg-border:#666;
            --selected: #303030;    --selected-shadow:  #303030;    --selected-offset: 0px; --selected-radius: 1px;
            --link:   	#d0adf0;
            --visited:  #9e9eff;
        }
        body::after {
            content: '';
            background-image: url('../../../img/paint-splatter-docs.svg'); /* from https://svgsilh.com/ */
            background-repeat: repeat; background-position: 5% 5%;
            opacity: 1;
            top: 0; left: 0; bottom: 0; right: 0;
            position: absolute;
            z-index: -1;   
        }
        a,         a > *           { color: var(--link) }
        a:visited, a:visited > *   { color: var(--visited) }

        body {
            background: var(--bg);            
            color:      var(--color);
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
        .nav        { overflow: auto; background: var(--bg2); }
        .docs-border{ overflow: auto; background: var(--bg2); }
        .docs       { overflow: auto; background: var(--bg2);  padding-left: 30px; }

        .title      { margin: 0px 0px 0px 20px; height: 26px; padding: 3px 10px; display: inline-flex; background: var(--bg2); box-shadow: 0px 0px  7px  7px #0000000a; border-top-left-radius: 4px; border-top-right-radius: 4px; cursor: pointer; }
        .languages  { margin: 0px 20px;         height: 18px; padding: 3px 10px; display: inline-flex; background: var(--bg2); box-shadow: 0px 0px 10px 10px #00000018; border-bottom-left-radius: 4px; border-bottom-right-radius: 4px; }
        .toggleTheme{ margin: 5px 20px;         height: 24px; padding: 3px 5px;  display: inline-flex; background: var(--bg2); border-radius: 2px; cursor: pointer;  }

        type        { color: var(--type) }
        keyword     { font-size: 13px; font-weight: normal; color: var(--keyword); }
        chapter     { font-size: 13px; font-weight: normal; color: var(--keyword); margin-left: 60px; }
        extends     { font-size: 13px; }
        cmd         { color: var(--value);    font-family: var(--mono-font); }
        sig         { white-space: nowrap; }
        key         { color: var(--key);      font-family: var(--mono-font); margin-left: -3px; padding: 0 2px; border-radius: 2px; border: 1px solid var(--key-border);   background: var(--bg); }
        rel         { color: var(--key);  }
        rel::before { content: ' ➞ '; }

        ref         { color: var(--field);    font-family: var(--mono-font);  }
        discUnion   { color: var(--bg);       font-family: var(--mono-font); margin-left: -3px; padding: 0 2px; border-radius: 2px; border: 1px solid var(--field-border); background: var(--field); }        
        disc        { color: var(--field);    font-family: var(--mono-font); margin-left: -3px; padding: 0 2px; border-radius: 2px; border: 1px solid var(--field-border); background: var(--bg); }
        field       { color: var(--field);    font-family: var(--mono-font); }
        discriminant{ color: var(--value);    font-family: var(--mono-font); }

        desc        { margin-left: 50px; display: block; }
        docs        { margin-left: 10px; display: block; }

        .namespace  { margin-bottom: 100px; }
        ul.enum li  { margin-left: 50px; font-family: var(--mono-font); color: var(--value) }
        div.nav > ul > li > a { color: var(--keyword); font-size: 13px; }

        h2      { margin-right: 30px;  }
        h3      { margin-right: 30px; margin-left: 30px;  margin-bottom: 5px; }
        h2.selected     { background: var(--selected); box-shadow: var(--selected-offset) var(--selected-offset) var(--selected-radius) var(--selected-radius) var(--selected-shadow); border-radius: 2px; }
        h3.selected     { background: var(--selected); box-shadow: var(--selected-offset) var(--selected-offset) var(--selected-radius) var(--selected-radius) var(--selected-shadow); border-radius: 2px; }

        a.highlight, a.highlight div    { background: var(--bg-highlight) !important; color: var(--highlight) !important; }
        a                               { text-decoration:  none; }
        ul                              { margin: 5px; padding-left: 8px; list-style-type: none; }

        ul > li > a                     { display: block; }
        ul > li > a:hover               { background: var(--selected); }

        ul > li > ul > li > a           { display: block; }
        ul > li > ul > li > a>div > key,
        ul > li > ul > li > a>div > discUnion,
        ul > li > ul > li > a>div > disc{ font-size: 12px; }
        
        ul > li > ul > li:hover         { background: var(--selected); }

        .namespace                          { scroll-margin-top: 5px; }
        .type                               { scroll-margin-top: 5px; scroll-margin-bottom: 100px;  } /* enable scrolling to next type without aligning next element on top */
        div.type table                      { margin-left: 70px; }
        div.type table tr td:nth-child(1)   { width: 150px; vertical-align: baseline; }
    </style>
    <script>
        // ----------------- theme dark / light
        function setTheme (mode) {
            console.log(`toggleTheme(${mode})`);
            document.documentElement.setAttribute('data-theme', mode);
            window.localStorage.setItem('theme', mode);
        }
        var toggleTheme = () => {
            let mode = document.documentElement.getAttribute('data-theme');
            mode = mode == 'dark' ? 'light' : 'dark'
            setTheme(mode)
        }
        var theme = window.localStorage.getItem('theme');
        if (!theme) {
            if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
                theme = 'dark';
            }
        }
        setTheme(theme);

        // ----------------- scrolling / selection highlighting 
        var docsSelection;
        function scrollTo(id) {
            var element = undefined;
            if (id == '') {
                element = document.querySelector('.docs').firstElementChild.firstElementChild;
            }
            if (!element) {
                element = document.getElementById(id);
            }
            if (element) {
                docsSelection?.classList.remove('selected');
                docsSelection = element;
                element.classList.add('selected');
                const parent = element.parentElement;
                const block = element.tagName == 'H3' ? 'nearest' : 'start'; // align only namespace to top
                parent.scrollIntoView({ behavior: 'smooth', block: block });

                // set focus after scrolling finished. Calling focus() directly cancel the scroll animation
                const anchors  = element.querySelectorAll(`a`);
                function scrollFinished() {
                    docs.onscroll = undefined;
                    anchors[0].focus();
                    // console.log('scroll finished');
                }
                var scrollTimeout = setTimeout(scrollFinished, 50);
                docs.onscroll = function () {
                    clearTimeout(scrollTimeout);
                    scrollTimeout = setTimeout(scrollFinished, 50);
                }
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
            if (anchor && anchor.hash != undefined && anchor.hash.startsWith('#')) {
                event.preventDefault();
                const id = anchor.hash.substring(1);
                scrollTo(id);
                window.history.pushState(null, anchor.href, anchor.href);
            }
        });

        // ----------------- highlight hovered links
        var hoveredLinkHash;
        var highlightedLinks = [];

        window.addEventListener('mousemove', function(event) {
            const path      = event.composedPath();
            const anchor    = path.find(el => el.tagName == 'A');
            if (anchor) {
                if (hoveredLinkHash == anchor.hash)
                    return;
                hoveredLinkHash = anchor.hash;
                const query = `a[href='${hoveredLinkHash}']`;
                const links  = document.body.querySelectorAll(query);
                // console.log(`hovered Link ${links.length} ${hoveredLinkHash}`);
                for (var link of highlightedLinks)  { link.classList.remove('highlight'); }
                for (var link of links)             { link.classList.add('highlight'); }
                highlightedLinks = links
            } else {
                if (!hoveredLinkHash)
                    return;
                // console.log(`hovered Link NONE`);
                hoveredLinkHash = undefined;
                for (var link of highlightedLinks)  { link.classList.remove('highlight'); }
            }
        });
    </script>
</head>

<body>

<!-- ------------------------------- head ------------------------------- -->
<div style='grid-area: body-head;' class='head'>
    <div style='display:flex; margin-left: 10px;'>
        <div style='align-self: self-end;'>
            <div style='flex-grow: 1;'></div>
            <h2 class='title'><a style='color: var(--color)' href='#'>{{schemaName}}</a></h2>
        </div>
        <div style='flex-grow: 1;'></div>
        <div class='languages' style='background-color: #363bff;'>
            <a style='color: white;' href='../index.html'>Typescript, C#, Kotlin, JSON Schema</a>
        </div>
        <!--  🌣 ☀ 🌞︎ ☾ ☽︎ 🌓︎ 🌘︎ 🌒︎ 🌖︎ 🌚︎ 🌙 🌕 🌞 🌛 🔅 -->
        <div class='toggleTheme' onclick='toggleTheme()'>☀ 🌘︎</div>
    </div>
</div>

<!-- ------------------------------- nav ------------------------------- -->
<div style='grid-area: body-nav;' class='nav'>
{{navigation}}
</div>

<!-- ---------------------------- docs-border --------------------------- -->
<div style='grid-area: body-docs-border;' class='docs-border'></div>

<!-- ------------------------------- docs ------------------------------- -->
<div id='docs' style='grid-area: body-docs;' class='docs'>
{{documentation}}
</div>

</body>
</html>
";
    }
}