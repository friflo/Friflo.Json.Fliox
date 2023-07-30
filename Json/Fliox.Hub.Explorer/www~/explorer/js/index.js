/// <reference types="../../../../../node_modules/monaco-editor/monaco" />
import { el, defaultConfig, getColorBasedOnBackground } from "./types.js";
import { Schema } from "./schema.js";
import { Explorer } from "./explorer.js";
import { EntityEditor } from "./entity-editor.js";
import { Playground } from "./playground.js";
import { Events, eventsInfo } from "./events.js";
import { ClusterTree } from "./components.js";
const flioxVersionEl = el("flioxVersion");
const projectName = el("projectName");
const projectUrl = el("projectUrl");
const envEl = el("envEl");
const defaultUser = el("user");
const defaultToken = el("token");
const userList = el("userList");
const authState = el("authState");
const clusterExplorer = el("clusterExplorer");
const entityExplorer = el("entityExplorer");
// Playground filter editor
const filterContainer = el("filterContainer");
// Playground request response editor
const requestContainer = el("requestContainer");
const responseContainer = el("responseContainer");
// entity/command/events editor
const commandValue = el("commandValue");
const entityContainer = el("entityContainer");
const eventsContainer = el("eventsContainer");
/** return true if "Control" key is pressed. Windows: Ctrl, MacOS: Command ⌘ */
export function isCtrlKey(event) {
    return event.ctrlKey || event.metaKey;
}
/* if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register("./sw.js").then(registration => {
        console.log("SW registered");
    }).catch(error => {
        console.error(`SW failed: ${error}`);
    });
} */
export const flioxRoot = "./";
export const setClass = function (element, enable, className) {
    const classList = element.classList;
    if (enable) {
        classList.add(className);
        return;
    }
    classList.remove(className);
};
export class App {
    constructor() {
        // --------------------------------------- schema ---------------------------------------
        this.databaseSchemas = {};
        this.schemaLess = '<span title="schema-less database - no type information available" style="opacity:0.5">schema-less</span>';
        // --------------------------------------- filter --------------------------------------- 
        this.filter = {};
        // filterTree example for testing validation in Playground > query-filter task
        this.filterTreeExample = {
            "filterTree": {
                "op": "equal",
                "left": { "op": "field", "name": "o.name" },
                "right": { "op": "string", "value": "Smartphone" }
            }
        };
        this.allMonacoSchemas = [];
        this.config = defaultConfig;
        this.explorer = new Explorer(this.config);
        this.editor = new EntityEditor();
        this.events = new Events();
        this.playground = new Playground();
        this.clusterTree = new ClusterTree();
        window.addEventListener("keydown", event => this.onKeyDown(event), true);
        window.addEventListener("keyup", event => this.onKeyUp(event), true);
    }
    static getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2)
            return parts.pop().split(';').shift();
        return null;
    }
    initUserToken() {
        var _a, _b;
        const user = (_a = App.getCookie("fliox-user")) !== null && _a !== void 0 ? _a : "admin";
        const token = (_b = App.getCookie("fliox-token")) !== null && _b !== void 0 ? _b : "admin";
        this.setUser(user);
        this.setToken(token);
    }
    setUserList() {
        const users = this.getConfig("users");
        userList.innerHTML = "";
        for (const user in users) {
            const div = document.createElement("div");
            const divSelect = document.createElement("span");
            div.onclick = () => { app.selectUser(user); };
            divSelect.innerText = user;
            const divRemove = document.createElement("span");
            divRemove.classList.add("user-list");
            if (user != "admin" && user != "unknown") {
                divRemove.classList.add("remove");
                divRemove.onclick = (ev) => { app.removeUser(ev, user); };
            }
            div.appendChild(divRemove);
            div.appendChild(divSelect);
            userList.appendChild(div);
        }
    }
    setUser(user) {
        defaultUser.value = user;
        document.cookie = `fliox-user=${user};`;
        const users = this.getConfig("users");
        if (users[user]) {
            return;
        }
        this.setConfig("users", users);
        this.setToken(user);
        this.setUserList();
    }
    setToken(token) {
        var _a;
        defaultToken.value = token;
        document.cookie = `fliox-token=${token};`;
        const user = defaultUser.value;
        const users = this.getConfig("users");
        if (((_a = users[user]) === null || _a === void 0 ? void 0 : _a.token) == token) {
            return;
        }
        users[user] = { token: token };
        this.setConfig("users", users);
    }
    selectUser(user) {
        this.setUser(user);
        const users = this.getConfig("users");
        const token = users[user].token;
        this.setToken(token);
        this.checkAuth();
    }
    removeUser(ev, user) {
        const users = this.getConfig("users");
        delete users[user];
        this.setConfig("users", users);
        const element = ev.target.parentNode;
        element.parentNode.removeChild(element);
        ev.stopPropagation();
    }
    togglePassword() {
        const type = defaultToken.type == "password" ? "text" : "password";
        defaultToken.type = type;
        const eyeEl = el("togglePassword");
        eyeEl.style.opacity = type == "password" ? "0.3" : "1";
    }
    checkAuth() {
        authState.className = "";
        authState.classList.add("auth-pending");
        authState.title = "authentication pending ...";
        App.postRequestTasks(undefined, []);
    }
    static setAuthState(user, failed, authError) {
        const success = !authError;
        const authClass = failed ? "auth-failed" : (success ? "auth-success" : "auth-error");
        authState.className = "";
        authState.classList.add(authClass);
        const state = success ? "authentication successful" : authError;
        authState.title = `user: ${user} · ${state}`;
    }
    static getCssRuleByName(name) {
        const cssRules = document.styleSheets[0].cssRules;
        for (let n = 0; n < cssRules.length; n++) {
            const rule = cssRules[n];
            if (rule.selectorText == name)
                return rule;
        }
        return null;
    }
    applyMetaKey(metaKeyPressed) {
        if (this.lastMetaKey == metaKeyPressed)
            return;
        this.lastMetaKey = metaKeyPressed;
        if (!this.refLinkDecoration) {
            const rule = App.getCssRuleByName(".refLinkDecoration:hover");
            this.refLinkDecoration = rule;
        }
        this.refLinkDecoration.style.cursor = metaKeyPressed ? "pointer" : "";
    }
    onKeyUp(event) {
        if (event.key == "Control" || event.key == "Meta") {
            this.applyMetaKey(false);
        }
    }
    onKeyDown(event) {
        if (event.key == "Control" || event.key == "Meta") {
            this.applyMetaKey(true);
        }
        switch (this.config.activeTab) {
            case "playground":
                this.onKeyDownPlayground(event);
                break;
            case "explorer":
                this.onKeyDownExplorer(event);
                break;
        }
        // console.log(`KeyboardEvent: code='${event.code}', ctrl:${event.metaKey}, alt:${event.altKey}`);
    }
    onKeyDownPlayground(event) {
        const ctrlKey = isCtrlKey(event);
        if (event.code == 'Enter' && ctrlKey && event.altKey) {
            this.playground.sendSyncRequest();
            event.preventDefault();
        }
        if (event.code == 'KeyP' && ctrlKey && event.altKey) {
            this.playground.postSyncRequest();
            event.preventDefault();
        }
        if (event.code == 'KeyS' && ctrlKey) {
            // event.preventDefault(); // avoid accidentally opening "Save As" dialog
        }
    }
    onKeyDownExplorer(event) {
        const editor = this.editor;
        const ctrlKey = isCtrlKey(event);
        switch (event.code) {
            case 'KeyS':
                if (!ctrlKey)
                    return;
                switch (editor.activeExplorerEditor) {
                    case "command":
                        this.execute(event, () => editor.sendCommand());
                        return;
                    case "entity":
                        this.execute(event, () => editor.saveEntitiesAction());
                        return;
                }
                break;
            case 'KeyP':
                if (!ctrlKey)
                    return;
                switch (editor.activeExplorerEditor) {
                    case "entity":
                        this.execute(event, () => editor.patchEntitiesAction());
                        return;
                }
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
        const user = App.getCookie("fliox-user");
        try {
            const path = `${flioxRoot}${tag}`;
            const rawResponse = await fetch(path, init);
            const text = await rawResponse.text();
            const json = JSON.parse(text);
            this.setAuthState(user, false, json.authError);
            return { text: text, json: json };
        }
        catch (error) {
            this.setAuthState(user, true, `authentication error: ${error.message}`);
            return {
                text: error.message,
                json: {
                    "msg": "error",
                    "message": error.message
                }
            };
        }
    }
    static async postRequestTasks(database, tasks) {
        const db = database == "main_db" ? undefined : database;
        const sync = {
            "msg": "sync",
            "db": db,
            "tasks": tasks,
            "user": defaultUser.value,
            "token": defaultToken.value
        };
        const request = JSON.stringify(sync);
        const tag = database ? `?${database}` : "";
        return await App.postRequest(request, tag);
    }
    static getRestPath(database, container, query) {
        let path = `${flioxRoot}rest/${database}`;
        if (container)
            path = `${path}/${container}`;
        if (query)
            path = `${path}?${query}`;
        return path;
    }
    static async restRequest(method, body, database, container, query) {
        const path = App.getRestPath(database, container, query);
        const headers = { 'Content-Type': 'application/json' };
        const clientId = app.playground.getClientId();
        if (clientId) {
            headers["fliox-client"] = clientId;
        }
        const init = { method: method, headers: headers, body: body };
        try {
            // authenticate with cookies: "fliox-user" & "fliox-token"
            const response = await fetch(path, init);
            const clientId = response.headers.get("fliox-client");
            app.playground.setClientId(clientId);
            return response;
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
        setClass(document.body, !config.showDescription, "miniHeader");
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
            setClass(tabs[i], isActiveTab, "selected");
        }
        document.body.style.gridTemplateRows = gridTemplateRows.join(" ");
        this.layoutEditors();
        if (tabName != "settings") {
            this.setConfig("activeTab", tabName);
        }
    }
    async loadCluster() {
        const simulateSlowPostResponse = false;
        if (simulateSlowPostResponse) {
            await new Promise(resolve => setTimeout(resolve, 2000));
            console.log("simulate slow server response");
        }
        const tasks = [
            { "task": "cmd", "name": "std.Host" },
            { "task": "query", "cont": "containers" },
            { "task": "query", "cont": "messages" },
            { "task": "query", "cont": "schemas" },
            { "task": "cmd", "name": "ModelFiles", param: { "type": "typescript" } },
        ];
        clusterExplorer.innerHTML = 'read databases <span class="spinner"></span>';
        const response = await App.postRequestTasks("cluster", tasks);
        const content = response.json;
        const error = App.getTaskError(content, 0);
        if (error) {
            clusterExplorer.innerHTML = App.errorAsHtml(error, null);
            return;
        }
        const hubInfoResult = content.tasks[0];
        const containersResult = content.tasks[1];
        const messagesResult = content.tasks[2];
        const schemasResult = content.tasks[3];
        this.hostInfo = hubInfoResult.result;
        const dbContainers = containersResult.set;
        const dbMessages = messagesResult.set;
        const dbSchemas = schemasResult.set;
        //
        const name = this.hostInfo.projectName;
        const hostVersion = this.hostInfo.hostVersion;
        const flioxVersion = this.hostInfo.flioxVersion;
        const website = this.hostInfo.projectWebsite;
        const envName = this.hostInfo.envName;
        const envColor = this.hostInfo.envColor;
        const modelFileResult = content.tasks[4];
        this.modelFiles = modelFileResult.result;
        flioxVersionEl.innerText = "Version " + flioxVersion;
        if (name) {
            projectName.innerText = name;
            document.title = envName ? `${name} · ${envName}` : name;
        }
        const version = hostVersion ? `version: ${hostVersion}\n` : "";
        projectUrl.title = `${version}Open project website in new tab`;
        if (website)
            projectUrl.href = website;
        if (envName)
            envEl.innerText = envName;
        if (envColor && CSS.supports('color', envColor)) {
            envEl.style.backgroundColor = envColor;
            envEl.style.color = getColorBasedOnBackground(envEl.style.backgroundColor);
        }
        const tree = this.clusterTree;
        const ulCluster = tree.createClusterUl(dbContainers, null);
        const firstDb = ulCluster.children[0];
        if (firstDb) {
            firstDb.classList.add("active");
            tree.selectTreeElement(firstDb.firstChild);
        }
        tree.onSelectDatabase = (elem, classList, databaseName) => {
            if (classList.length > 0) {
                return;
            }
            tree.selectTreeElement(elem);
            const messages = dbMessages.find(c => c.id == databaseName);
            const containers = dbContainers.find(c => c.id == databaseName);
            this.editor.listCommands(databaseName, messages, containers);
        };
        tree.onSelectContainer = (elem, classList, databaseName, containerName) => {
            if (classList.length > 0) {
                this.events.toggleContainerSub(databaseName, containerName);
                return;
            }
            tree.selectTreeElement(elem);
            const params = { database: databaseName, container: containerName, ids: [] };
            this.editor.clearEntity(databaseName, containerName);
            this.explorer.loadContainer(params, null);
        };
        this.events.initEvents(dbContainers, dbMessages);
        const schemaMap = Schema.createEntitySchemas(this.databaseSchemas, dbSchemas);
        const monacoSchemas = Object.values(schemaMap);
        this.addSchemas(monacoSchemas);
        clusterExplorer.textContent = "";
        clusterExplorer.appendChild(ulCluster);
        this.editor.listCommands(dbMessages[0].id, dbMessages[0], dbContainers[0]);
    }
    getSchemaType(database) {
        const schema = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;
        return `<a title="open database schema in new tab" href="${flioxRoot}schema/${database}/html/schema.html" target="${schema.schemaName}" class="docLink">${schema.schemaName}</a>`;
    }
    getSchemaCommand(database, category, command) {
        const schema = this.databaseSchemas[database];
        if (!schema)
            return command;
        return `<a title="open ${category} API in new tab" href="${flioxRoot}schema/${database}/html/schema.html#${category}" target="${schema.schemaName}" class="docLink">${command}</a>`;
    }
    getSchemaTypes(database) {
        const schema = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;
        return `<a title="open database schema types in new tab" href="${flioxRoot}schema/${database}/index.html" target="${schema.schemaName}" class="schemaExports">Typescript, C#, Kotlin, JSON Schema / OpenAPI</a>`;
    }
    getSchemaDescription(database) {
        var _a;
        const schema = this.databaseSchemas[database];
        if (!schema)
            return ""; // this.schemaLess;
        return (_a = schema._rootSchema.description) !== null && _a !== void 0 ? _a : "";
    }
    getType(database, def) {
        const schema = this.databaseSchemas[database];
        const ns = def._namespace;
        const name = def._typeName;
        return `<a title="open type definition in new tab" href="${flioxRoot}schema/${database}/html/schema.html#${ns}.${name}" target="${schema.schemaName}" class="docLink">${name}</a>`;
    }
    getEntityType(database, container) {
        const def = this.getContainerSchema(database, container);
        if (!def)
            return this.schemaLess;
        return app.getType(database, def);
    }
    getTypeLabel(database, fieldType) {
        if (!fieldType) {
            return "";
        }
        const typeType = fieldType.type;
        if (typeType) {
            if (Array.isArray(typeType))
                return typeType.join(" | ");
            return typeType;
        }
        const type = Schema.getFieldType(fieldType);
        const def = type.type._resolvedDef;
        if (def) {
            const typeStr = app.getType(database, def);
            const nullStr = type.isNullable ? " | null" : "";
            return `${typeStr}${nullStr}`;
        }
        let result = JSON.stringify(fieldType);
        return result = result == "{}" ? "any" : result;
    }
    static getDatabaseLink(database) {
        return `<a title="open database in new tab" href="./rest/${database}" target="_blank" rel="noopener noreferrer">${database}</a>`;
    }
    static getApiLinks(database, description, hash) {
        hash = hash.replace(".", "_");
        let apiLinks = `<a class="oas" title="${description} as OpenAPI specification (OAS) in new tab "` +
            `href="${flioxRoot}schema/${database}/open-api.html${hash}" target="_blank" rel="noopener noreferrer">OAS</a>`;
        if (app.hostInfo.routes.includes("/graphql")) {
            apiLinks += `&nbsp;<a class="graphql" title="${description} as GraphQL API (GQL) in new tab "` +
                `href="${flioxRoot}graphql/${database}" target="_blank" rel="noopener noreferrer">GQL</a>`;
        }
        return apiLinks;
    }
    static getDiagramLink(database) {
        return `<a class="diagram" title="Open database schema as class diagram in new tab "` +
            `href="${flioxRoot}schema/${database}/html/class-diagram.html" target="_blank" rel="noopener noreferrer">CD</a>`;
    }
    static getMessagesLink(database) {
        const href = `./rest/${database}?cmd=std.Messages`;
        return `<a title="open database commands & messages in new tab" href=${href} target="_blank" rel="noopener noreferrer">${database}</a>`;
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
        const filter = this.explorer.getFilterValue();
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
        if (!filters[database])
            filters[database] = {};
        filters[database][container] = [filter];
        this.setConfig("filters", filters);
    }
    updateFilterLink() {
        const filter = this.explorer.getFilterValue();
        const query = filter.trim() == "" ? "" : `?filter=${encodeURIComponent(filter)}`;
        const url = `./rest/${this.filter.database}/${this.filter.container}${query}`;
        el("filterLink").href = url;
    }
    saveUser(user, token) {
        const users = this.config.users;
        users[user] = { token: token };
        this.setConfig("users", users);
    }
    // --------------------------------------- monaco editor ---------------------------------------
    // [Monaco Editor Playground] https://microsoft.github.io/monaco-editor/playground.html#extending-language-services-configure-json-defaults
    static addSchema(prefix, jsonSchema, schemas) {
        for (const schemaName in jsonSchema) {
            const schema = jsonSchema[schemaName];
            const url = prefix + schemaName;
            const schemaEntry = {
                uri: "http://" + url,
                schema: schema
            };
            schemas.push(schemaEntry);
        }
    }
    static refineFilterTree(jsonSchema) {
        let refinements = 0;
        for (const schemaName in jsonSchema) {
            const schema = jsonSchema[schemaName];
            for (const definitionName in schema.definitions) {
                const definition = schema.definitions[definitionName];
                const properties = definition.properties;
                for (const propertyName in properties) {
                    if (propertyName != "filterTree")
                        continue;
                    refinements++;
                    const url = "http://filter/json-schema/Friflo.Json.Fliox.Transform.FilterOperation.json";
                    properties[propertyName] = { "$ref": url, _resolvedDef: null };
                }
            }
        }
        if (refinements != 2)
            console.error(`expect 2 filterTree refinements. was: ${refinements}`);
    }
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
            const protocolSchemaResponse = await fetch(`${flioxRoot}schema/protocol/json-schema.json`);
            const protocolSchema = await protocolSchemaResponse.json();
            App.addSchema("protocol/json-schema/", protocolSchema, schemas);
            const filterSchemaResponse = await fetch(`${flioxRoot}schema/filter/json-schema.json`);
            const filterSchema = await filterSchemaResponse.json();
            App.addSchema("filter/json-schema/", filterSchema, schemas);
            App.refineFilterTree(protocolSchema);
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
    static findSchema(monacoSchemas, uri) {
        for (let i = 0; i < monacoSchemas.length; i++) {
            if (monacoSchemas[i].uri == uri) {
                return monacoSchemas[i];
            }
        }
        return null;
    }
    async setupEditors() {
        // this.setExplorerEditor("none");
        // --- setup JSON Schema for monaco
        const requestUri = monaco.Uri.parse("request://jsonRequest.json");
        const responseUri = monaco.Uri.parse("request://jsonResponse.json");
        const eventUri = monaco.Uri.parse("request://jsonEvent.json");
        const monacoSchemas = await this.createProtocolSchemas();
        {
            const schema = App.findSchema(monacoSchemas, "http://protocol/json-schema/Friflo.Json.Fliox.Hub.Protocol.ProtocolRequest.json");
            schema.fileMatch = [requestUri.toString()]; // associate with model
        }
        {
            const schema = App.findSchema(monacoSchemas, "http://protocol/json-schema/Friflo.Json.Fliox.Hub.Protocol.ProtocolMessage.json");
            schema.fileMatch = [responseUri.toString()]; // associate with model
        }
        {
            const protocol = "http://protocol/json-schema/Friflo.Json.Fliox.Hub.Protocol.json";
            const protocolSchema = App.findSchema(monacoSchemas, protocol);
            if (!protocolSchema) {
                throw "Friflo.Json.Fliox.Hub.Protocol.json schema not found";
            }
            const syncEventDef = protocolSchema.schema.definitions["SyncEvent"];
            if (!syncEventDef) {
                throw "SyncEvent schema not found";
            }
            const description = "seq of containing EventMessage.\n_seq is not a member of SyncEvent in the Protocol - added only for filtering";
            syncEventDef.properties["_seq"] = { type: "number", description: description, _resolvedDef: null };
            const uri = "http://protocol/json-schema/Friflo.Json.Fliox.Hub.Protocol.json#/definitions/SyncEvent";
            const syncEventSchema = {
                schema: syncEventDef,
                uri: uri,
            };
            monacoSchemas.push(syncEventSchema);
            const eventsArray = { "type": "array", "items": { "$ref": uri } };
            const eventListSchema = {
                schema: eventsArray,
                uri: null,
                fileMatch: [eventUri.toString()]
            };
            monacoSchemas.push(eventListSchema);
        }
        this.addSchemas(monacoSchemas);
        const defaultOpt = {
            fixedOverflowWidgets: true // ensure tooltip's / popups are not clipped by parent elements with style overflow:hidden
        };
        // --- create Explorer filter editor
        if (filterContainer) {
            this.filterEditor = monaco.editor.create(filterContainer, Object.assign({ minimap: { enabled: false }, folding: false, lineNumbers: "off", codeLens: false, renderLineHighlight: "none", lineDecorationsWidth: 5, scrollbar: { vertical: "visible", horizontal: "auto" }, overviewRulerLanes: 0, scrollBeyondLastLine: false, smoothScrolling: true, guides: {
                    indentation: false // hide vertical indentation lines
                }, automaticLayout: true }, defaultOpt));
            this.explorer.initFilterEditor();
        }
        // --- create Playground request editor
        {
            this.requestEditor = monaco.editor.create(requestContainer, defaultOpt);
            this.requestModel = monaco.editor.createModel(null, "json", requestUri);
            this.requestEditor.setModel(this.requestModel);
            const defaultRequest = `{
  "msg": "sync",
  "tasks": [
    {
      "task":  "cmd",
      "name":  "std.Echo",
      "param": "Hello World"
    }
  ]
}`;
            this.requestModel.setValue(defaultRequest);
        }
        // --- create Playground response editor
        {
            this.responseEditor = monaco.editor.create(responseContainer, defaultOpt);
            this.responseModel = monaco.editor.createModel(null, "json", responseUri);
            this.responseEditor.setModel(this.responseModel);
        }
        // --- create entity editor
        {
            this.entityEditor = monaco.editor.create(entityContainer, defaultOpt);
            this.entityEditor.onMouseDown((e) => {
                const ctrlKey = isCtrlKey(e.event);
                if (!ctrlKey)
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
            this.commandValueEditor = monaco.editor.create(commandValue, defaultOpt);
            // this.commandValueModel   = monaco.editor.createModel(null, "json");
            // this.commandValueEditor.setModel(this.commandValueModel);
            //this.commandValueEditor.setValue("{}");
        }
        this.editor.initEditor(this.entityEditor, this.commandValueEditor);
        // --- create subscription event editor
        {
            this.eventsEditor = monaco.editor.create(eventsContainer, defaultOpt);
            const eventModel = monaco.editor.createModel(null, "json", eventUri);
            this.eventsEditor.setModel(eventModel);
            this.eventsEditor.setValue(eventsInfo);
        }
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
        this.eventsEditor.updateOptions(Object.assign({}, editorSettings));
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
        this.initConfigValue("users");
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
                const editors = [
                    { editor: this.entityEditor, elem: entityContainer },
                    { editor: this.commandValueEditor, elem: commandValue },
                ];
                this.layoutMonacoEditors(editors);
                break;
            }
            case "events": {
                const editors = [
                    { editor: this.eventsEditor, elem: eventsContainer },
                ];
                this.layoutMonacoEditors(editors);
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
    static copyGridTemplateStyle(template) {
        const gridEl = el(template);
        if (!gridEl.style.gridTemplateColumns) {
            const cssRules = App.getCssRuleByName(`#${template}`);
            if (!cssRules)
                throw `cssRules not found: #${template}`;
            gridEl.style.gridTemplateColumns = cssRules.style.gridTemplateColumns;
        }
        if (!gridEl.style.gridTemplateRows) {
            const cssRules = App.getCssRuleByName(`#${template}`);
            if (!cssRules)
                throw `cssRules not found: #${template}`;
            gridEl.style.gridTemplateRows = cssRules.style.gridTemplateRows;
        }
    }
    toggleFilterBar() {
        App.copyGridTemplateStyle("explorer");
        const explorerEl = el("explorer");
        const rows = explorerEl.style.gridTemplateRows.split(" ");
        rows[0] = rows[0] == "0px" ? "40px" : "0px";
        explorerEl.style.gridTemplateRows = rows.join(" ");
        this.layoutEditors();
    }
    startDrag(event, template, bar, horizontal) {
        // console.log(`drag start: ${event.offsetX}, ${template}, ${bar}`)
        this.dragHorizontal = horizontal;
        this.dragOffset = horizontal ? event.offsetX : event.offsetY;
        this.dragBar = el(bar);
        this.dragTemplate = el(template);
        App.copyGridTemplateStyle(template);
        document.body.style.cursor = "ew-resize";
        document.body.onpointermove = (event) => app.onDrag(event);
        document.body.onpointerup = (event) => app.endDrag(event);
        this.dragBar.setPointerCapture(event.pointerId);
        event.preventDefault();
    }
    getGridColumns(xy) {
        var _a;
        const prev = this.dragBar.previousElementSibling;
        xy = xy - (this.dragHorizontal ? prev.offsetLeft : prev.offsetTop);
        if (xy < 0)
            xy = 0;
        // console.log (`drag x: ${x}`);
        switch (this.dragTemplate.id) {
            case "playground": return [xy + "px", "var(--bar-width)", "1fr"];
            case "events": return [xy + "px", "var(--bar-width)", "1fr"];
            case "explorer": {
                const col = this.dragTemplate.style.gridTemplateColumns.split(" ");
                const row = this.dragTemplate.style.gridTemplateRows.split(" ");
                switch (this.dragBar.id) { //  [150px var(--bar-width) 200px var(--bar-width) 1fr];
                    case "exBar0": return [xy + "px", row[1], row[2], row[3], row[4], row[5]];
                    case "exBar1": return [xy + "px", col[1], col[2], col[3]];
                    case "exBar2": return [col[0], col[1], xy + "px", col[3]];
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
    endDrag(event) {
        this.dragBar.releasePointerCapture(event.pointerId);
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
        this.setUserList();
        this.initUserToken();
        this.openTab(app.getConfig("activeTab"));
        this.playground.initExampleRequestList();
        // --- methods performing network requests - note: methods are not awaited
        this.loadCluster().then(() => {
            if (this.hostInfo.routes.includes("/examples")) {
                this.playground.addRemoteExamples("./examples");
            }
        });
    }
}
App.bracketValue = /\[(.*?)\]/;
export const app = new App();
//# sourceMappingURL=index.js.map