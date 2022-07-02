import * as path            from 'path';
import * as fs              from 'fs';

import { promisify }        from 'util';
import { fromMarkdown }     from 'mdast-util-from-markdown'
import { Parent, Root }     from 'mdast-util-from-markdown/lib';


const readdirAsync      = promisify(fs.readdir);
const statAsync         = promisify(fs.stat);


async function scanMarkdownFiles(directoryName: string, results : string[] = []) {
    let files = await readdirAsync(directoryName);
    for (let f of files) {
        let fullPath: string = path.join(directoryName, f);
        fullPath = fullPath.replaceAll("\\", "/");
        let stat = await statAsync(fullPath);
        if (stat.isDirectory()) {
            if (directoryName == "node_modules") {
                continue;
            }
            await scanMarkdownFiles(fullPath, results);
        } else {
            if (f == "class-diagram.md") {
                continue;
            }            
            if (fullPath.endsWith(".md")) {
                results.push(fullPath);
            }
        }
    }
    return results;
}

type MdLink = {
    url:    string;
    line:   number;
    column:    number;
}

type Markdown = {
    path:       string,
    folder:     string,
    links:      MdLink[],
    anchors:    string[]
}

type MarkdownMap = { [path: string] : Markdown };

function textFromNode(node: Parent, texts: string[])  {
    const children = node.children
    for (const name in children) {
        const child = children[name];
        const node  = child as Parent;
        if (child.type == "text") {
            texts.push(child.value);
        }
        if (node.children) {
            textFromNode(node, texts);
        }  
    }
}

function markdownFromTree(tree: Parent, markdown: Markdown) {
    const children = tree.children
    for (const name in children) {
        const child = children[name];
        const node  = child as Parent;
        if (node.children) {
            markdownFromTree(node, markdown);
        }
        if (node.type == "link") {
            const start = node.position.start;
            const link: MdLink = {
                url:    node.url,
                line:   start.line,
                column: start.column
            }
            markdown.links.push(link);
        }
        if (node.type == "heading") {    
            const texts: string[] = [];
            textFromNode (node, texts);
            const label = texts.join("").trim();
            markdown.anchors.push(label);
        }    
    }
}

function parseMarkdown(filePath: string) : Markdown {
    const content       = fs.readFileSync(filePath, {encoding: 'utf8'});
    const tree: Root    = fromMarkdown(content);
    const folder        = path.dirname(filePath);

    const markdown: Markdown = {
        path:       filePath,
        folder:     folder,
        links:      [],
        anchors:    []
    };
    // if (path != "README.md") { return markdown; }
    markdownFromTree (tree, markdown);
    return markdown;
}

function checkLinks (cwd: string, markdown: Markdown, markdownMap: MarkdownMap) {
    for (const link of markdown.links) {
        const url = link.url;
        if (url.startsWith("#")         ||
            url.startsWith("http://")   ||
            url.startsWith("https://")
        ) {
            continue;
        }
        const target = path.normalize(cwd + markdown.folder + "/" + url);

        fs.access(target, fs.constants.R_OK, (err) => {
            if (err == null)
                return;
            console.log(`${cwd + markdown.path}:${link.line}:${link.column} error: broken link - ${target}`);
        });
    }
}

async function main() {
    const cwd = process.cwd();
    console.log("cwd:", cwd);
    console.log();

    const files = await scanMarkdownFiles("./");

    const markdownMap : MarkdownMap = {};
    for (const file of files) {
        const markdown = parseMarkdown(file);
        markdownMap[file] = markdown;
        // console.log(markdown);
    }
    // console.log(markdownMap["README.md"]);
    // console.log(files);
    checkLinks(cwd + "/", markdownMap["README.md"], markdownMap);
    for (const path in markdownMap) {
        const markdown = markdownMap[path];
        checkLinks(cwd + "/", markdown, markdownMap);
    }
}

main();
