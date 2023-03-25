/// <reference types="../../../../../node_modules/@types/json-to-ast/index" />
export const defaultConfig = {
    showLineNumbers: false,
    showMinimap: false,
    formatEntities: true,
    formatResponses: true,
    activeTab: "explorer",
    showDescription: false,
    filters: {},
    users: { "admin": { token: "admin" }, "unknown": { token: "" } }
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
const rgbToObject = (rgb) => {
    const colors = rgb.slice(rgb.indexOf("(") + 1, rgb.indexOf(")"));
    const colorArr = colors.split(", ");
    if (colorArr.length == 3) {
        const rgbArray = [
            parseInt(colorArr[0]),
            parseInt(colorArr[1]),
            parseInt(colorArr[2]),
        ];
        if (!isNaN(rgbArray[0]) && !isNaN(rgbArray[1]) && !isNaN(rgbArray[2]))
            return rgbArray;
    }
    console.log(`Invalid rgb value: ${rgb}. Expect format: rgb(r, g, b)`);
    return null;
};
export const getColorBasedOnBackground = (color) => {
    const c = rgbToObject(color);
    if (!c)
        return "#ffffff";
    if (c[0] * 0.299 + c[1] * 0.587 + c[2] * 0.114 > 186)
        return "#000000";
    return "#ffffff";
};
//# sourceMappingURL=types.js.map