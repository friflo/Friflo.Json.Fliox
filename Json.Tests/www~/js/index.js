/// <reference types="../../../node_modules/monaco-editor/monaco" />
import { el, createEl, defaultConfig } from "./types.js";
import { Schema } from "./schema.js";
import { Explorer } from "./explorer.js";
import { EntityEditor } from "./entity-editor.js";
import { Playground } from "./playground.js";
const hubInfoEl = el("hubInfo");
const defaultUser = el("user");
const defaultToken = el("token");
const catalogExplorer = el("catalogExplorer");
const entityExplorer = el("entityExplorer");
const entityFilter = el("entityFilter");
// request response editor
const requestContainer = el("requestContainer");
const responseContainer = el("responseContainer");
// entity/command editor
const commandValue = el("commandValue");
const entityContainer = el("entityContainer");
/* if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register("./sw.js").then(registration => {
        console.log("SW registered");
    }).catch(error => {
        console.error(`SW failed: ${error}`);
    });
} */
export class App {
    constructor() {
        // --------------------------------------- schema ---------------------------------------
        this.databaseSchemas = {};
        this.schemaLess = '<span title="missing type definition - schema-less database" style="opacity:0.5">unknown</span>';
        // --------------------------------------- filter --------------------------------------- 
        this.filter = {};
        this.allMonacoSchemas = [];
        this.config = defaultConfig;
        this.explorer = new Explorer(this.config);
        this.editor = new EntityEditor();
        this.playground = new Playground();
        window.addEventListener("keydown", event => this.onKeyDown(event), true);
        window.addEventListener("keyup", event => this.onKeyUp(event), true);
    }
    getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2)
            return parts.pop().split(';').shift();
        return null;
    }
    initUserToken() {
        var _a, _b;
        const user = (_a = this.getCookie("fliox-user")) !== null && _a !== void 0 ? _a : "admin";
        const token = (_b = this.getCookie("fliox-token")) !== null && _b !== void 0 ? _b : "admin";
        this.setUser(user);
        this.setToken(token);
    }
    setUser(user) {
        defaultUser.value = user;
        document.cookie = `fliox-user=${user};`;
    }
    setToken(token) {
        defaultToken.value = token;
        document.cookie = `fliox-token=${token};`;
    }
    selectUser(element) {
        const value = element.innerText;
        this.setUser(value);
        this.setToken(value);
    }
    applyCtrlKey(event) {
        if (this.lastCtrlKey == event.ctrlKey)
            return;
        this.lastCtrlKey = event.ctrlKey;
        if (!this.refLinkDecoration) {
            const cssRules = document.styleSheets[0].cssRules;
            for (let n = 0; n < cssRules.length; n++) {
                const rule = cssRules[n];
                if (rule.selectorText == ".refLinkDecoration:hover")
                    this.refLinkDecoration = rule;
            }
        }
        this.refLinkDecoration.style.cursor = this.lastCtrlKey ? "pointer" : "";
    }
    onKeyUp(event) {
        if (event.code == "ControlLeft")
            this.applyCtrlKey(event);
    }
    onKeyDown(event) {
        const editor = this.editor;
        if (event.code == "ControlLeft")
            this.applyCtrlKey(event);
        switch (this.config.activeTab) {
            case "playground":
                if (event.code == 'Enter' && event.ctrlKey && event.altKey) {
                    this.playground.sendSyncRequest();
                    event.preventDefault();
                }
                if (event.code == 'KeyP' && event.ctrlKey && event.altKey) {
                    this.playground.postSyncRequest();
                    event.preventDefault();
                }
                if (event.code == 'KeyS' && event.ctrlKey) {
                    // event.preventDefault(); // avoid accidentally opening "Save As" dialog
                }
                break;
            case "explorer":
                switch (event.code) {
                    case 'KeyS':
                        if (event.ctrlKey)
                            this.execute(event, () => editor.saveEntitiesAction());
                        break;
                    case 'KeyP':
                        if (event.ctrlKey && event.altKey)
                            this.execute(event, () => editor.sendCommand("POST"));
                        break;
                    case 'ArrowLeft':
                        if (event.altKey)
                            this.execute(event, () => editor.navigateEntity(editor.entityHistoryPos - 1));
                        break;
                    case 'ArrowRight':
                        if (event.altKey)
                            this.execute(event, () => editor.navigateEntity(editor.entityHistoryPos + 1));
                        break;
                    case 'Digit1':
                        if (!event.altKey)
                            break;
                        this.switchTab();
                        break;
                }
        }
        // console.log(`KeyboardEvent: code='${event.code}', ctrl:${event.ctrlKey}, alt:${event.altKey}`);
    }
    switchTab() {
        if (document.activeElement == entityExplorer)
            this.entityEditor.focus();
        else
            entityExplorer.focus();
    }
    execute(event, lambda) {
        lambda();
        event.preventDefault();
    }
    // --------------------------------------- Fliox HTTP --------------------------------------- 
    static async postRequest(request, tag) {
        const init = {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: request
        };
        try {
            const path = `./?${tag}`;
            const rawResponse = await fetch(path, init);
            const text = await rawResponse.text();
            return {
                text: text,
                json: JSON.parse(text)
            };
        }
        catch (error) {
            return {
                text: error.message,
                json: {
                    "msg": "error",
                    "message": error.message
                }
            };
        }
    }
    static async postRequestTasks(database, tasks, tag) {
        const db = database == "main_db" ? undefined : database;
        const sync = {
            "msg": "sync",
            "database": db,
            "tasks": tasks,
            "user": defaultUser.value,
            "token": defaultToken.value
        };
        const request = JSON.stringify(sync);
        tag = tag ? tag : "";
        return await App.postRequest(request, `${database}/${tag}`);
    }
    static getRestPath(database, container, ids, query) {
        let path = `./rest/${database}`;
        if (container)
            path = `${path}/${container}`;
        if (ids) {
            if (Array.isArray(ids)) {
                path = `${path}?ids=${ids.join(',')}`;
            }
            else {
                path = `${path}/${ids}`;
            }
        }
        if (query)
            path = `${path}?${query}`;
        return path;
    }
    static async restRequest(method, body, database, container, ids, query) {
        const path = App.getRestPath(database, container, ids, query);
        const init = {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: body
        };
        try {
            // authenticate with cookies: "fliox-user" & "fliox-token"
            return await fetch(path, init);
        }
        catch (error) {
            const text = () => error.message;
            const ret = {
                ok: false,
                status: 0,
                statusText: "exception",
                text: text,
                json: () => { throw error.message; }
            };
            return ret;
        }
    }
    static getTaskError(content, taskIndex) {
        if (content.msg == "error") {
            return content.message;
        }
        const task = content.tasks[taskIndex];
        if (task.task == "error")
            return "task error:\n" + task.message;
        return undefined;
    }
    static errorAsHtml(message, p) {
        // first line: error type, second line: error message
        const pos = message.indexOf(' > ');
        let error = message;
        if (pos > 0) {
            let reason = message.substring(pos + 3);
            if (reason.startsWith("at ")) {
                const id = reason.match(App.bracketValue)[1];
                if (p && id) {
                    const c = { database: p.database, container: p.container, ids: [id] };
                    const coordinate = JSON.stringify(c);
                    const link = `<a  href="#" onclick='app.loadEntities(${coordinate})'>${id}</a>`;
                    reason = reason.replace(id, link);
                }
                reason = reason.replace("] ", "]<br>");
            }
            error = message.substring(0, pos) + " ><br>" + reason;
        }
        return `<code style="white-space: pre-line; color:red">${error}</code>`;
    }
    // --------------------------------------- general App UI --------------------------------------- 
    toggleDescription() {
        this.changeConfig("showDescription", !this.config.showDescription);
        this.openTab(this.config.activeTab);
    }
    openTab(tabName) {
        const config = this.config;
        config.activeTab = tabName;
        App.setClass(document.body, !config.showDescription, "miniHeader");
        const tabContents = document.getElementsByClassName("tabContent");
        const tabs = document.getElementsByClassName("tab");
        const gridTemplateRows = document.body.style.gridTemplateRows.split(" ");
        const headerHeight = getComputedStyle(document.body).getPropertyValue('--header-height');
        gridTemplateRows[0] = config.showDescription ? headerHeight : "0";
        for (let i = 0; i < tabContents.length; i++) {
            const tabContent = tabContents[i];
            const isActiveContent = tabContent.id == tabName;
            tabContent.style.display = isActiveContent ? "grid" : "none";
            gridTemplateRows[i + 2] = isActiveContent ? "1fr" : "0"; // + 2  ->  "body-header" & "body-tabs"
            const isActiveTab = tabs[i].getAttribute('value') == tabName;
            App.setClass(tabs[i], isActiveTab, "selected");
        }
        document.body.style.gridTemplateRows = gridTemplateRows.join(" ");
        this.layoutEditors();
        if (tabName != "settings") {
            this.setConfig("activeTab", tabName);
        }
    }
    static setClass(element, enable, className) {
        const classList = element.classList;
        if (enable) {
            classList.add(className);
            return;
        }
        classList.remove(className);
    }
    async loadCluster() {
        const tasks = [
            { "task": "query", "container": "containers" },
            { "task": "query", "container": "schemas" },
            { "task": "query", "container": "commands" },
            { "task": "command", "name": "DbHubInfo" }
        ];
        catalogExplorer.innerHTML = 'read databases <span class="spinner"></span>';
        const response = await App.postRequestTasks("cluster", tasks, null);
        const content = response.json;
        const error = App.getTaskError(content, 0);
        if (error) {
            catalogExplorer.innerHTML = App.errorAsHtml(error, null);
            return;
        }
        const dbContainers = content.containers[0].entities;
        const dbSchemas = content.containers[1].entities;
        const dbCommands = content.containers[2].entities;
        const hubInfoResult = content.tasks[3];
        this.hubInfo = hubInfoResult.result;
        //
        let description = this.hubInfo.description;
        const website = this.hubInfo.website;
        if (description || website) {
            if (!description)
                description = "Website";
            hubInfoEl.innerHTML = website ? `<a href="${website}" target="_blank" rel="noopener noreferrer">${description}</a>` : description;
        }
        const ulCatalogs = createEl('ul');
        ulCatalogs.onclick = (ev) => {
            const path = ev.composedPath();
            const selectedElement = path[0];
            if (selectedElement.classList.contains("caret")) {
                path[2].classList.toggle("active");
                return;
            }
            path[1].classList.add("active");
            if (this.selectedCatalog)
                this.selectedCatalog.classList.remove("selected");
            this.selectedCatalog = selectedElement;
            selectedElement.classList.add("selected");
            const databaseName = selectedElement.childNodes[1].textContent;
            const commands = dbCommands.find(c => c.id == databaseName);
            const containers = dbContainers.find(c => c.id == databaseName);
            this.editor.listCommands(databaseName, commands, containers);
        };
        let firstDatabase = true;
        for (const dbContainer of dbContainers) {
            const liCatalog = createEl('li');
            if (firstDatabase) {
                firstDatabase = false;
                liCatalog.classList.add("active");
            }
            const liDatabase = createEl('div');
            const catalogCaret = createEl('div');
            catalogCaret.classList.value = "caret";
            const catalogLabel = createEl('span');
            catalogLabel.innerText = dbContainer.id;
            liDatabase.title = "database";
            catalogLabel.style.pointerEvents = "none";
            liDatabase.append(catalogCaret);
            liDatabase.append(catalogLabel);
            liCatalog.appendChild(liDatabase);
            ulCatalogs.append(liCatalog);
            const ulContainers = createEl('ul');
            ulContainers.onclick = (ev) => {
                ev.stopPropagation();
                const path = ev.composedPath();
                const selectedElement = path[0];
                // in case of a multiline text selection selectedElement is the parent
                if (selectedElement.tagName.toLowerCase() != "div")
                    return;
                if (this.selectedCatalog)
                    this.selectedCatalog.classList.remove("selected");
                this.selectedCatalog = selectedElement;
                this.selectedCatalog.classList.add("selected");
                const containerName = this.selectedCatalog.innerText.trim();
                const databaseName = path[3].childNodes[0].childNodes[1].textContent;
                const params = { database: databaseName, container: containerName, ids: [] };
                this.editor.clearEntity(databaseName, containerName);
                this.explorer.loadContainer(params, null);
            };
            liCatalog.append(ulContainers);
            for (const containerName of dbContainer.containers) {
                const liContainer = createEl('li');
                liContainer.title = "container";
                const containerLabel = createEl('div');
                containerLabel.innerHTML = "&nbsp;" + containerName;
                liContainer.append(containerLabel);
                ulContainers.append(liContainer);
            }
        }
        const schemaMap = Schema.createEntitySchemas(this.databaseSchemas, dbSchemas);
        const monacoSchemas = Object.values(schemaMap);
        this.addSchemas(monacoSchemas);
        catalogExplorer.textContent = "";
        catalogExplorer.appendChild(ulCatalogs);
        this.editor.listCommands(dbCommands[0].id, dbCommands[0], dbContainers[0]);
    }
    getSchemaType(database) {
        const schema = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;
        return `<a title="open database schema in new tab" href="./schema/${database}/html/schema.html" target="${database}">${schema.schemaName}</a>`;
    }
    getSchemaExports(database) {
        const schema = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;
        return `<a title="open database schema in new tab" href="./schema/${database}/index.html" target="${database}">Typescript, C#, Kotlin, JSON Schema, HTML</a>`;
    }
    static getType(database, def) {
        const ns = def._namespace;
        const name = def._typeName;
        return `<a title="open type definition in new tab" href="./schema/${database}/html/schema.html#${ns}.${name}" target="${database}">${name}</a>`;
    }
    getEntityType(database, container) {
        const def = this.getContainerSchema(database, container);
        if (!def)
            return this.schemaLess;
        return App.getType(database, def);
    }
    getTypeLabel(database, type) {
        if (type.type) {
            return type.type;
        }
        const def = type._resolvedDef;
        if (def) {
            return App.getType(database, def);
        }
        let result = JSON.stringify(type);
        return result = result == "{}" ? "any" : result;
    }
    static getDatabaseLink(database) {
        return `<a title="open database in new tab" href="./rest/${database}" target="_blank" rel="noopener noreferrer">${database}</a>`;
    }
    getContainerSchema(database, container) {
        const schema = app.databaseSchemas[database];
        if (schema) {
            return schema._containerSchemas[container];
        }
        return null;
    }
    filterOnKeyDown(event) {
        if (event.code != 'Enter')
            return;
        this.applyFilter();
    }
    applyFilter() {
        const database = this.filter.database;
        const container = this.filter.container;
        const filter = entityFilter.value;
        const query = filter.trim() == "" ? null : `filter=${encodeURIComponent(filter)}`;
        const params = { database: database, container: container, ids: [] };
        this.saveFilter(database, container, filter);
        this.explorer.loadContainer(params, query);
    }
    removeFilter() {
        const params = { database: this.filter.database, container: this.filter.container, ids: [] };
        this.explorer.loadContainer(params, null);
    }
    saveFilter(database, container, filter) {
        const filters = this.config.filters;
        if (filter.trim() == "") {
            const filterDatabase = filters[database];
            if (filterDatabase) {
                delete filterDatabase[container];
            }
        }
        else {
            if (!filters[database])
                filters[database] = {};
            filters[database][container] = [filter];
        }
        this.setConfig("filters", filters);
    }
    updateFilterLink() {
        const filter = entityFilter.value;
        const query = filter.trim() == "" ? "" : `?filter=${encodeURIComponent(filter)}`;
        const url = `./rest/${this.filter.database}/${this.filter.container}${query}`;
        el("filterLink").href = url;
    }
    // --------------------------------------- monaco editor ---------------------------------------
    // [Monaco Editor Playground] https://microsoft.github.io/monaco-editor/playground.html#extending-language-services-configure-json-defaults
    async createProtocolSchemas() {
        // configure the JSON language support with schemas and schema associations
        // var schemaUrlsResponse  = await fetch("/protocol/json-schema/directory");
        // var schemaUrls          = await schemaUrlsResponse.json();
        /* var schemas = [{
                uri: "http://myserver/foo-schema.json", // id of the first schema
                // fileMatch: [modelUri.toString()], // associate with our model
                schema: {
                    type: "object",
                    properties: {
                        p1: {
                            enum: ["v1", "v2"]
                        },
                        p2: {
                            $ref: "http://myserver/bar-schema.json" // reference the second schema
                        }
                    }
                }
            }, {
                uri: "http://myserver/bar-schema.json", // id of the second schema
                schema: {
                    type: "object",
                    properties: {
                        q1: {
                            enum: ["x1", "x2"]
                        }
                    }
                }
            }]; */
        const schemas = [];
        try {
            const jsonSchemaResponse = await fetch("schema/protocol/json-schema.json");
            const jsonSchema = await jsonSchemaResponse.json();
            for (const schemaName in jsonSchema) {
                const schema = jsonSchema[schemaName];
                const url = "protocol/json-schema/" + schemaName;
                const schemaEntry = {
                    uri: "http://" + url,
                    schema: schema
                };
                schemas.push(schemaEntry);
            }
        }
        catch (e) {
            console.error("load json-schema.json failed");
        }
        return schemas;
    }
    addSchemas(monacoSchemas) {
        this.allMonacoSchemas.push(...monacoSchemas);
        // [LanguageServiceDefaults | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.languages.json.LanguageServiceDefaults.html
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
            validate: true,
            schemas: this.allMonacoSchemas
        });
    }
    async setupEditors() {
        // this.setExplorerEditor("none");
        // --- setup JSON Schema for monaco
        const requestUri = monaco.Uri.parse("request://jsonRequest.json"); // a made up unique URI for our model
        const responseUri = monaco.Uri.parse("request://jsonResponse.json"); // a made up unique URI for our model
        const monacoSchemas = await this.createProtocolSchemas();
        for (let i = 0; i < monacoSchemas.length; i++) {
            if (monacoSchemas[i].uri == "http://protocol/json-schema/Friflo.Json.Fliox.Hub.Protocol.ProtocolRequest.json") {
                monacoSchemas[i].fileMatch = [requestUri.toString()]; // associate with our model
            }
            if (monacoSchemas[i].uri == "http://protocol/json-schema/Friflo.Json.Fliox.Hub.Protocol.ProtocolMessage.json") {
                monacoSchemas[i].fileMatch = [responseUri.toString()]; // associate with our model
            }
        }
        this.addSchemas(monacoSchemas);
        // --- create request editor
        {
            this.requestEditor = monaco.editor.create(requestContainer, { /* model: model */});
            this.requestModel = monaco.editor.createModel(null, "json", requestUri);
            this.requestEditor.setModel(this.requestModel);
            const defaultRequest = `{
    "msg": "sync",
    "tasks": [
        {
        "task":  "command",
        "name":  "DbEcho",
        "value": "Hello World"
        }
    ]
}`;
            this.requestModel.setValue(defaultRequest);
        }
        // --- create response editor
        {
            this.responseEditor = monaco.editor.create(responseContainer, { /* model: model */});
            this.responseModel = monaco.editor.createModel(null, "json", responseUri);
            this.responseEditor.setModel(this.responseModel);
        }
        // --- create entity editor
        {
            this.entityEditor = monaco.editor.create(entityContainer, {});
            this.entityEditor.onMouseDown((e) => {
                if (!e.event.ctrlKey)
                    return;
                if (this.editor.activeExplorerEditor != "entity")
                    return;
                // console.log('mousedown - ', e);
                const value = this.entityEditor.getValue();
                const column = e.target.position.column;
                const line = e.target.position.lineNumber;
                window.setTimeout(() => { this.editor.tryFollowLink(value, column, line); }, 1);
            });
        }
        // --- create command value editor
        {
            this.commandValueEditor = monaco.editor.create(commandValue, {});
            // this.commandValueModel   = monaco.editor.createModel(null, "json");
            // this.commandValueEditor.setModel(this.commandValueModel);
            //this.commandValueEditor.setValue("{}");
        }
        this.editor.initEditor(this.entityEditor, this.commandValueEditor);
        // this.commandResponseModel = monaco.editor.createModel(null, "json");
        this.setEditorOptions();
        window.onresize = () => {
            this.layoutEditors();
        };
    }
    setEditorOptions() {
        const editorSettings = {
            lineNumbers: this.config.showLineNumbers ? "on" : "off",
            minimap: { enabled: this.config.showMinimap ? true : false },
            theme: window.appConfig.monacoTheme,
            mouseWheelZoom: true
        };
        this.requestEditor.updateOptions(Object.assign({}, editorSettings));
        this.responseEditor.updateOptions(Object.assign({}, editorSettings));
        this.entityEditor.updateOptions(Object.assign({}, editorSettings));
        this.commandValueEditor.updateOptions(Object.assign({}, editorSettings));
    }
    // -------------------------------------- config --------------------------------------------
    setConfig(key, value) {
        this.config[key] = value;
        const elem = el(key);
        if (elem instanceof HTMLInputElement) {
            elem.value = value;
            elem.checked = value;
        }
        const valueStr = JSON.stringify(value, null, 2);
        window.localStorage.setItem(key, valueStr);
    }
    getConfig(key) {
        const valueStr = window.localStorage.getItem(key);
        try {
            return JSON.parse(valueStr);
        }
        catch (e) { }
        return undefined;
    }
    initConfigValue(key) {
        const value = this.getConfig(key);
        if (value == undefined) {
            this.setConfig(key, this.config[key]);
            return;
        }
        this.setConfig(key, value);
    }
    loadConfig() {
        this.initConfigValue("showLineNumbers");
        this.initConfigValue("showMinimap");
        this.initConfigValue("formatEntities");
        this.initConfigValue("formatResponses");
        this.initConfigValue("activeTab");
        this.initConfigValue("showDescription");
        this.initConfigValue("filters");
    }
    changeConfig(key, value) {
        this.setConfig(key, value);
        switch (key) {
            case "showLineNumbers":
            case "showMinimap":
                this.setEditorOptions();
                break;
        }
    }
    formatJson(format, text) {
        if (format) {
            try {
                // const action = editor.getAction("editor.action.formatDocument");
                // action.run();
                const obj = JSON.parse(text);
                const formatted = JSON.stringify(obj, null, 4);
                if (!Array.isArray(obj))
                    return formatted;
                let lines = formatted.split('\n');
                lines = lines.slice(1, lines.length - 1);
                lines = lines.map(l => l.substring(4)); // remove 4 leading spaces
                return `[${lines.join('\n')}]`;
            }
            catch (error) { }
        }
        return text;
    }
    layoutEditors() {
        // console.log("layoutEditors - activeTab: " + activeTab)
        switch (this.config.activeTab) {
            case "playground": {
                const editors = [
                    { editor: this.responseEditor, elem: responseContainer },
                    { editor: this.requestEditor, elem: requestContainer },
                ];
                this.layoutMonacoEditors(editors);
                break;
            }
            case "explorer": {
                // layout from right to left. Otherwise commandValueEditor.clientWidth is 0px;
                const editors2 = [
                    { editor: this.entityEditor, elem: entityContainer },
                    { editor: this.commandValueEditor, elem: commandValue },
                ];
                this.layoutMonacoEditors(editors2);
                break;
            }
        }
    }
    layoutMonacoEditors(pairs) {
        for (let n = pairs.length - 1; n >= 0; n--) {
            const pair = pairs[n];
            if (!pair.editor || !pair.elem.children[0]) {
                pairs.splice(n, 1);
            }
        }
        for (const pair of pairs) {
            const child = pair.elem.children[0];
            child.style.width = "0px"; // required to shrink width.  Found no alternative solution right now.
            child.style.height = "0px"; // required to shrink height. Found no alternative solution right now.
        }
        for (const pair of pairs) {
            pair.editor.layout();
        }
        // set editor width/height to their container width/height
        for (const pair of pairs) {
            const child = pair.elem.children[0];
            child.style.width = pair.elem.clientWidth + "px";
            child.style.height = pair.elem.clientHeight + "px";
        }
    }
    startDrag(event, template, bar, horizontal) {
        // console.log(`drag start: ${event.offsetX}, ${template}, ${bar}`)
        this.dragHorizontal = horizontal;
        this.dragOffset = horizontal ? event.offsetX : event.offsetY;
        this.dragTemplate = el(template);
        this.dragBar = el(bar);
        document.body.style.cursor = "ew-resize";
        document.body.onmousemove = (event) => app.onDrag(event);
        document.body.onmouseup = () => app.endDrag();
        event.preventDefault();
    }
    getGridColumns(xy) {
        var _a;
        const prev = this.dragBar.previousElementSibling;
        xy = xy - (this.dragHorizontal ? prev.offsetLeft : prev.offsetTop);
        if (xy < 20)
            xy = 20;
        // console.log (`drag x: ${x}`);
        switch (this.dragTemplate.id) {
            case "playground": return [xy + "px", "var(--bar-width)", "1fr"];
            case "explorer": {
                const cols = this.dragTemplate.style.gridTemplateColumns.split(" ");
                switch (this.dragBar.id) { //  [150px var(--bar-width) 200px var(--bar-width) 1fr];
                    case "exBar1": return [xy + "px", cols[1], cols[2], cols[3]];
                    case "exBar2": return [cols[0], cols[1], xy + "px", cols[3]];
                }
                break;
            }
            case "explorerEdit":
                this.editor.commandEditWidth = xy + "px";
                return [this.editor.commandEditWidth, "var(--vbar-width)", "1fr"];
        }
        throw `unhandled condition in getGridColumns() id: ${(_a = this.dragTemplate) === null || _a === void 0 ? void 0 : _a.id}`;
    }
    onDrag(event) {
        if (!this.dragTemplate)
            return;
        // console.log(`  drag: ${event.clientX}`);
        const clientXY = this.dragHorizontal ? event.clientX : event.clientY;
        const xy = clientXY - this.dragOffset;
        const cols = this.getGridColumns(xy);
        if (this.dragHorizontal) {
            this.dragTemplate.style.gridTemplateColumns = cols.join(" ");
        }
        else {
            this.dragTemplate.style.gridTemplateRows = cols.join(" ");
        }
        this.layoutEditors();
        event.preventDefault();
    }
    endDrag() {
        if (!this.dragTemplate)
            return;
        document.body.onmousemove = undefined;
        document.body.onmouseup = undefined;
        this.dragTemplate = undefined;
        document.body.style.cursor = "auto";
    }
    toggleTheme() {
        let mode = document.documentElement.getAttribute('data-theme');
        mode = mode == 'dark' ? 'light' : 'dark';
        window.setTheme(mode);
        this.setEditorOptions();
    }
    initApp() {
        // --- methods without network requests
        this.loadConfig();
        this.initUserToken();
        this.openTab(app.getConfig("activeTab"));
        // --- methods performing network requests - note: methods are not awaited
        this.playground.loadExampleRequestList();
        this.loadCluster();
    }
}
App.bracketValue = /\[(.*?)\]/;
export const app = new App();
//# sourceMappingURL=index.js.map