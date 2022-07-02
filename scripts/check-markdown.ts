import * as path            from 'path';
import * as fs              from 'fs';

import { promisify }        from 'util';
import { fromMarkdown }     from 'mdast-util-from-markdown'
import { Root } from 'mdast-util-from-markdown/lib';


const readdirAsync      = promisify(fs.readdir);
const statAsync         = promisify(fs.stat);


async function scanMarkdownFiles(directoryName: string, results : string[] = []) {
    let files = await readdirAsync(directoryName);
    for (let f of files) {
        let fullPath: string = path.join(directoryName, f);
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
    links:      string[],
    anchors:    string[]
}

function markdownFromTree(tree: Root, markdown: Markdown) {
    for (const child in tree.children) {
        
    }
}

function parseMarkdown(path: string) {
    const content       = fs.readFileSync(path, {encoding: 'utf8'});
    const tree: Root    = fromMarkdown(content);
    const markdown = {
        links:   [],
        anchors: []
    };
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
    }
    console.log(files);
}

main();
