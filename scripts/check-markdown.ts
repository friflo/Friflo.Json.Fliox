import * as path            from 'path';
import * as fs              from 'fs';

import { promisify }        from 'util';
import { fromMarkdown }     from 'mdast-util-from-markdown'
import { Content, Parent, Root } from 'mdast-util-from-markdown/lib';


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

type Markdown = {
    path:       string,
    links:      string[],
    anchors:    string[]
}

function textFromNode(node: Parent, texts: string[])  {
    const children = node.children
    for (const name in children) {
        const child = children[name];
        if (child.type == "text") {
            texts.push(child.value);
        }
        const node: any = child;
        if (node.children) {
            textFromNode(node, texts);
        }  
    }
}

function markdownFromTree(tree: Parent, markdown: Markdown) {
    const children = tree.children
    for (const name in children) {
        const child = children[name];
        var node = child as Parent;
        if (node.children) {
            markdownFromTree(node, markdown);
        }
        if (node.type == "link") {
            markdown.links.push(node.url);
        }
        if (node.type == "heading") {    
            const texts: string[] = [];
            textFromNode (node, texts);
            const label = texts.join("").trim();
            markdown.anchors.push(label);
        }    
    }
}

function parseMarkdown(path: string) : Markdown {
    const content       = fs.readFileSync(path, {encoding: 'utf8'});
    const tree: Root    = fromMarkdown(content);
    const markdown = {
        path:       path,
        links:      [],
        anchors:    []
    };
    // if (path != "README.md") { return markdown; }
    markdownFromTree (tree, markdown);
    // get links in markdown
    return markdown;
}

async function main() {
    console.log("cwd:", process.cwd());
    console.log();

    const files = await scanMarkdownFiles("./");

    const markdownMap : { [path: string] : Markdown } = {};
    for (const file of files) {
        const markdown = parseMarkdown(file);
        markdownMap[file] = markdown;
        console.log(markdown);
    }
    // console.log(markdownMap["README.md"]);
    console.log(files);
}

main();
