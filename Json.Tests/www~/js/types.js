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
//# sourceMappingURL=types.js.map