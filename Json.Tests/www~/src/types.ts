

export type Resource = {
    database:   string;
    container:  string;
    ids:        string[];
};

export const defaultConfig = {
    showLineNumbers : false,
    showMinimap     : false,
    formatEntities  : false,
    formatResponses : true,
    activeTab       : "explorer",
    showDescription : true,
    filters         : {} as { [database: string]: { [container: string]: string[]}}
}

export type Config     = typeof defaultConfig
export type ConfigKey  = keyof Config;

export function el<T extends HTMLElement> (id: string) : T{
    return document.getElementById(id) as T;
}

export function createEl<K extends keyof HTMLElementTagNameMap>(tagName: K): HTMLElementTagNameMap[K] {
    return document.createElement(tagName);
}

export type Entity = { [key: string] : any };

export type Method = "GET" | "POST" | "PUT" | "DELETE";
