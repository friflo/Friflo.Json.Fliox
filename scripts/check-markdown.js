import * as path            from 'path';
import * as fs              from 'fs';

import { promisify }        from 'util';
import { fromMarkdown }     from 'mdast-util-from-markdown'

const readdirAsync      = promisify(fs.readdir);
const statAsync         = promisify(fs.stat);


async function scan(directoryName = './data', results = []) {
    let files = await readdirAsync(directoryName);
    for (let f of files) {
        let fullPath = path.join(directoryName, f);
        let stat = await statAsync(fullPath);
        if (stat.isDirectory()) {
            if (directoryName == "node_modules") {
                continue;
            }
            await scan(fullPath, results);
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


function checkMarkdown(path) {

    const content   = fs.readFileSync(path, {encoding: 'utf8'});
    const tree      = fromMarkdown(content)
    // get links in markdown
    return {};
}

async function main() {
    console.log("cwd:", process.cwd());
    console.log();

    const files = await scan("./");
    const markdownMap = {};
    for (const file of files) {
        const markdown = checkMarkdown(file);
        markdownMap[file] = markdown;
    }
    console.log(files);
}

main();
