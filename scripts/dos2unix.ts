import * as path            from 'path';
import * as fs              from 'fs';

import { promisify }        from 'util';


const readdirAsync      = promisify(fs.readdir);
const statAsync         = promisify(fs.stat);
const readFileAsync     = promisify(fs.readFile);
const writeFileAsync    = promisify(fs.writeFile);

const ignoreFolders = [
    "node_modules",
    ".bin",
    ".obj",
    ".git",
    ".idea",
    ".vs",
    ".run"
];

const fileExtensions = [
    ".cs",
    ".json",
    ".html",
    ".http",
    ".ts",
    ".graphql",
    ".md"
];

async function scanFiles(directoryPath: string, results : string[]) {
    const files = await readdirAsync(directoryPath);
    // console.log(`${directoryPath}    ${files.length}`)

    for (const filename of files) {
        let fullPath: string = path.join(directoryPath, filename);
        fullPath = fullPath.replaceAll("\\", "/");
        const stat = await statAsync(fullPath);
        if (stat.isDirectory()) {
            if (ignoreFolders.indexOf(filename) != - 1) {
                continue;
            }
            await scanFiles(fullPath, results);
        } else {   
            for (const ext of fileExtensions) {
                if (fullPath.endsWith(ext)) {
                    results.push(fullPath);
                }
            }
        }
    }
    return results;
}


async function main() : Promise<void> {
    // --- scan files in folder
    const allFiles = []
    await scanFiles("./Json/",          allFiles);
    await scanFiles("./Json.Tests/",    allFiles);

    const extensions: { [ext: string]: string[]} = { };

    for (const file of allFiles) {
        const ext = path.extname(file);
        if (extensions[ext] == undefined)
            extensions[ext] = [file];
        else
            extensions[ext].push(file);
    }
    for (const ext in extensions) {
        const files = extensions[ext]
        console.log(`--- *${ext} files: ${files.length}`);
        let count = 0;
        for (const file of files) {
            const content = await readFileAsync(file, {encoding: 'utf8'}); // first character is the BOM
            const contentLF = content.replace(/\r\n/g, "\n");
            if (content != contentLF) {
                console.log(`${file}`);
                await writeFileAsync(file, contentLF, { encoding: 'utf8'});
                count++;
            }
        }
        if (count > 0) {
            console.log (`--- converted files to LF ending. count: ${count}`);
        }
    }
}

await main();
console.log ("");

