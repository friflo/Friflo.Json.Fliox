// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Schema.Language
{
    public sealed partial class HtmlGenerator
    {
        private const string Template =
@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport'     content='width=device-width, initial-scale=1'>
    <meta name='description'  content='Schema - {{schemaName}}'>
    <meta name='color-scheme' content='dark light'>
    <meta name='generated-by' content='{{generatedByLink}}'>
    <link rel='icon' href='../../../explorer/img/Json-Fliox-53x43.svg' type='image/x-icon'>
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
            --bg:       #fff;       --bg-highlight:     #e8e8ff;
            --bg2:      #fff;
            --bg-border:#ccc;
            --selected: #f0f0f0;    --selected-shadow:  #f0f0f0;    --selected-offset: 0px; --selected-radius: 1px;
            --link:     #0000ee;
            --visited:  #551a8b;
            --oas:      #55cf42;
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
            --oas:      #009900;
        }
        body::after {
            content: '';
            background-image: url('../../../explorer/img/paint-splatter-docs.svg'); /* from https://svgsilh.com/ */
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
                    grid-template-columns:  max-content 1fr;
                    grid-template-rows:     40px  10px 1fr;
                    grid-gap:   0px;
                    margin:     0;
                    height:     100vh;
                    width:      100vw;
                    font-family: sans-serif
                }
        @media print {
            div.docs  { display: inline-table !important; }
            div.namespace,
            div.type  { page-break-inside: avoid; }
            body {
                    display: grid;
                    grid-template-areas:  'body-docs';
                    font-family: sans-serif
            }
        }

        .head       { overflow: hidden; }
        .nav        { overflow-x: hidden; background: var(--bg2); overflow-y: visisble; scrollbar-width: thin; }
    /*  .nav::-webkit-scrollbar { width: 6px; display: visible; } no effect */
        .docs-border{ overflow: auto; background: var(--bg2); }
        .docs       { overflow: auto; background: var(--bg2);  padding-left: 5px; }

        .title      { margin: 0px 0px 0px 20px; height: 26px; padding: 3px 10px; display: inline-flex; background: var(--bg2); box-shadow: 0px 0px  7px  7px #0000000a; border-top-left-radius: 4px; border-top-right-radius: 4px; cursor: pointer; }
        .diagram    { margin: 0px 20px;         height: 18px; padding: 3px 10px; display: inline-flex; background: var(--bg2); box-shadow: 0px 0px 10px 10px #00000018; border-bottom-left-radius: 4px; border-bottom-right-radius: 4px; }
        .languages  { margin: 0px 20px;         height: 18px; padding: 3px 10px; display: inline-flex; background: var(--bg2); box-shadow: 0px 0px 10px 10px #00000018; border-bottom-left-radius: 4px; border-bottom-right-radius: 4px; }
        .toggleTheme{ margin: 5px 20px;         height: 24px; padding: 3px 5px;  display: inline-flex; background: var(--bg2); border-radius: 2px; cursor: pointer;  }

        type        { white-space: nowrap; }
        predef      { color: var(--type); }
        keyword     { font-size: 13px; font-weight: normal; color: var(--keyword); }
        chapter     { font-size: 13px; font-weight: normal; display: block; margin-left: 30px; margin-top: 10px; }
        chapter a:link,
        chapter a:visited { color: var(--keyword); }
        
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

        desc        { margin-left: 40px; margin-bottom: 10px; display: block; opacity: 0.7; }
        docs        { margin-left: 10px; display: block; opacity: 0.7; }
        code        { white-space: pre; display: block; padding-left: 3px;  padding-right: 3px; font-size: 16px; }
        oas         { margin-left: 5px; background: var(--oas); font-size: 12px; font-weight: bold; border-radius: 3px; padding: 0px 3px; }
        oas a:visited,
        oas a:link  { color: white; }

        .namespace  { margin-bottom: 100px; }
        div.nav > ul > li > a { color: var(--keyword); font-size: 13px; }
        div.nav ul li ul li a div                  { display: flex; align-items: center; }
        div.nav ul li ul li a div span             { margin-right: 10px; }
        div.nav ul li ul li a div *:nth-child(1)   { flex-grow:  1; }

        h2      { margin-right: 30px; font-size: 20px; }
        h3      { margin-right: 20px; margin-left: 20px;  margin-bottom: 5px; }
        h2.selected     { background: var(--selected); box-shadow: var(--selected-offset) var(--selected-offset) var(--selected-radius) var(--selected-radius) var(--selected-shadow); border-radius: 2px; }
        h3.selected     { background: var(--selected); box-shadow: var(--selected-offset) var(--selected-offset) var(--selected-radius) var(--selected-radius) var(--selected-shadow); border-radius: 2px; }

        a.highlight, a.highlight div    { background-color: var(--bg-highlight) !important; color: var(--highlight) !important; }
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
        div.type table                      { margin-left: 40px; }
        div.type table td table             { margin-left: 10px; } /* discriminants table */
        div.type table tr td:nth-child(1)   { width: 150px; vertical-align: baseline; }
        div.type table tr td:nth-child(2)   { width: 150px; }

        div.enum table tr td:nth-child(1)   { font-family: var(--mono-font); color: var(--value) }
        div.enum table tr td:nth-child(2)   { width: auto; }

        .commands tr                        { vertical-align: top; }
        .fields tr                          { vertical-align: top; }
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
                const isChapter = id == 'commands' || id == 'messages' || id == 'containers';
                const parent    = isChapter ? element : element.parentElement;
                const block     = element.tagName == 'H3' ? 'nearest' : 'start'; // align only namespace to top
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
            if (!anchor || anchor.target)
                return;
            const hash = anchor.hash;
            if (hash != undefined && hash.startsWith('#')) {
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
                if (anchor.target)
                    return;
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
        <div class='diagram' style='background-color: #363bff;'>
            <a style='color: white;' href='class-diagram.html' target='_blank' rel='noopener'>class diagram</a>            
        </div>
        <div style='flex-grow: 1;'></div>
        <div class='languages' style='background-color: #363bff;'>
            <a style='color: white;' href='../index.html'>Typescript, C#, Kotlin, JSON Schema / OpenAPI</a>
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