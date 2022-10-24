/// <reference types="../../../../../node_modules/@types/json-to-ast/index" />


// declare const parse : any; // https://www.npmjs.com/package/json-to-ast
declare function parse(json: string, settings?: jsonToAst.Options): jsonToAst.ValueNode;

export type Resource = {
    readonly database:   string;
    readonly container:  string;
    readonly ids:        string[];
};

export const defaultConfig = {
    showLineNumbers : false,
    showMinimap     : false,
    formatEntities  : false,
    formatResponses : true,
    activeTab       : "explorer",
    showDescription : false,
    filters         : {} as { [database: string]: { [container: string]: string[]}}
};

export type Config     = typeof defaultConfig
export type ConfigKey  = keyof Config;

export function el<T extends HTMLElement> (id: string) : T{
    return document.getElementById(id) as T;
}

export function createEl<K extends keyof HTMLElementTagNameMap>(tagName: K): HTMLElementTagNameMap[K] {
    return document.createElement(tagName);
}

export type Entity = { [key: string] : any };

export type Method = "GET" | "POST" | "PUT" | "DELETE" | "PATCH";

export const parseAst = (value: string) : jsonToAst.ValueNode => {
    try {
        JSON.parse(value);  // early out on invalid JSON
        // 1.) [json-to-ast - npm] https://www.npmjs.com/package/json-to-ast
        // 2.) bundle.js created fom npm module 'json-to-ast' via:
        //     [node.js - How to use npm modules in browser? is possible to use them even in local (PC) ? - javascript - Stack Overflow] https://stackoverflow.com/questions/49562978/how-to-use-npm-modules-in-browser-is-possible-to-use-them-even-in-local-pc
        // 3.) browserify main.js | uglifyjs > bundle.js
        //     [javascript - How to get minified output with browserify? - Stack Overflow] https://stackoverflow.com/questions/15590702/how-to-get-minified-output-with-browserify
        const ast = parse(value, { loc: true });
        // console.log ("AST", ast);
        return ast;
    } catch (error) {
        console.error("parseAst", error);
    }
    return null;
};

const rgbToObject = (rgb: string) : number [] | null => {
    const colors    = rgb.slice(rgb.indexOf("(") + 1, rgb.indexOf(")"));
    const colorArr  = colors.split(", ");
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

export const getColorBasedOnBackground = (color: string) : any => {
    const c = rgbToObject(color);
    if (!c)
        return "#ffffff";
    if (c[0] * 0.299 + c[1] * 0.587 + c[2] * 0.114 > 186)
        return "#000000";
    return "#ffffff";
};

export type MessageCategory = "commands" | "messages";