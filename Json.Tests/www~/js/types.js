/// <reference types="../../../node_modules/@types/json-to-ast/index" />
export const defaultConfig = {
    showLineNumbers: false,
    showMinimap: false,
    formatEntities: false,
    formatResponses: true,
    activeTab: "explorer",
    showDescription: true,
    filters: {}
};
export function el(id) {
    return document.getElementById(id);
}
export function createEl(tagName) {
    return document.createElement(tagName);
}
export const parseAst = (value) => {
    try {
        JSON.parse(value); // early out on invalid JSON
        // 1.) [json-to-ast - npm] https://www.npmjs.com/package/json-to-ast
        // 2.) bundle.js created fom npm module 'json-to-ast' via:
        //     [node.js - How to use npm modules in browser? is possible to use them even in local (PC) ? - javascript - Stack Overflow] https://stackoverflow.com/questions/49562978/how-to-use-npm-modules-in-browser-is-possible-to-use-them-even-in-local-pc
        // 3.) browserify main.js | uglifyjs > bundle.js
        //     [javascript - How to get minified output with browserify? - Stack Overflow] https://stackoverflow.com/questions/15590702/how-to-get-minified-output-with-browserify
        const ast = parse(value, { loc: true });
        // console.log ("AST", ast);
        return ast;
    }
    catch (error) {
        console.error("parseAst", error);
    }
    return null;
};
//# sourceMappingURL=types.js.map