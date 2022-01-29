/// <reference types="../../../node_modules/monaco-editor/monaco" />
/// <reference types="../../../node_modules/@types/json-to-ast/index" />


import { CommandType, FieldType, JsonSchema, JsonType }
            from "../../assets~/Schema/Typescript/JsonSchema/Friflo.Json.Fliox.Schema.JSON";

import { DbSchema, DbContainers, DbCommands, DbHubInfo }
            from "../../assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster";

import { SyncRequest, SyncResponse, ProtocolResponse_Union }
            from "../../assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol";
import { SyncRequestTask_Union, SendCommandResult }
            from "../../assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol.Tasks";

// declare const parse : any; // https://www.npmjs.com/package/json-to-ast
declare function parse(json: string, settings?: jsonToAst.Options): jsonToAst.ValueNode;

type AddRelation = (value: jsonToAst.ValueNode, container: string) => void;

declare global {
    interface Window {
        appConfig: { monacoTheme: string };
        setTheme(mode: string) : void;
        app: App;
    }
}

declare module "../../assets~/Schema/Typescript/JsonSchema/Friflo.Json.Fliox.Schema.JSON" {
    interface JsonType {
        _typeName:  string;
        _namespace: string;
        _resolvedDef: JsonType;
    }

    interface JsonSchema {
        _resolvedDef: JsonType;
    }

    interface FieldType {
        _resolvedDef: JsonType;
    }
}

declare module "../../assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster" {
    interface DbSchema {
        _rootSchema:        JsonType;
        _containerSchemas : { [key: string] : JsonType };        
    }
}

type MonacoSchema = {
    readonly uri: string;
    /**
     * A list of glob patterns that describe for which file URIs the JSON schema will be used.
     * '*' and '**' wildcards are supported. Exclusion patterns start with '!'.
     * For example '*.schema.json', 'package.json', '!foo*.schema.json', 'foo/**\/BADRESP.json'.
     * A match succeeds when there is at least one pattern matching and last matching pattern does not start with '!'.
     */
    fileMatch?: string[];
    /**
     * The schema for the given URI.
     */
    readonly schema?: any;

    _resolvedDef?: any
}

type FindRange = {
    entity: monaco.Range | null;
    value:  monaco.Range | null;
}


type Method = "GET" | "POST" | "PUT" | "DELETE";
type Resource = {
    database:   string;
    container:  string;
    ids:        string[];
};

type ExplorerEditor = "command" | "entity" | "dbInfo"

const defaultConfig = {
    showLineNumbers : false,
    showMinimap     : false,
    formatEntities  : false,
    formatResponses : true,
    activeTab       : "explorer",
    showDescription : true,
    filters         : {} as { [database: string]: { [container: string]: string[]}}
}

type Config     = typeof defaultConfig
type ConfigKey  = keyof Config;

type Column = {
    width:  number,
    name:   string,
    path:   string[],
    th?:    HTMLTableCellElement
}

type TypeName   = "null" | "object" | "string" | "boolean" | "number" | "integer" | "array";

type DataType   = {
    typeName:   TypeName,
    jsonType?:  JsonType
}



function createMeasureTextWidth(width: number) : HTMLElement {
    const div = document.createElement("div");
    document.body.appendChild(div);
    const style = div.style;
    style.fontSize      = `${width}px`;
    style.height        = "auto";
    style.width         = "auto";
    style.maxWidth      = "1000px"; // ensure not measuring crazy long texts
    style.position      = "absolute";
    style.whiteSpace    = "no-wrap";
    style.visibility    = "hidden";
    return div;
}
const measureTextWidth = createMeasureTextWidth (14);

type Entity = { [key: string] : any };
type CellData = {
    value?:         string,
    count?:         number,
    isObjectArray?: boolean
};
// --------------------------------------- WebSocket ---------------------------------------
let connection:         WebSocket;
let websocketCount      = 0;
let req                 = 1;
let clt: string | null  = null;
let requestStart: number;
let subSeq              = 0;
let subCount            = 0;

function el<T extends HTMLElement> (id: string) : T{
    return document.getElementById(id) as T;
}

function createEl<K extends keyof HTMLElementTagNameMap>(tagName: K): HTMLElementTagNameMap[K] {
    return document.createElement(tagName);
}

const hubInfoEl         = el("hubInfo");
const responseState     = el("response-state");
const subscriptionCount = el("subscriptionCount");
const subscriptionSeq   = el("subscriptionSeq");
const selectExample     = el("example")         as HTMLSelectElement;
const socketStatus      = el("socketStatus");
const reqIdElement      = el("req");
const ackElement        = el("ack");
const cltElement        = el("clt");
const defaultUser       = el("user")            as HTMLInputElement;
const defaultToken      = el("token")           as HTMLInputElement;
const catalogExplorer   = el("catalogExplorer");
const entityExplorer    = el("entityExplorer");
const writeResult       = el("writeResult");
const readEntitiesDB    = el("readEntitiesDB");
const readEntities      = el("readEntities");
const catalogSchema     = el("catalogSchema");
const entityType        = el("entityType");
const entityIdsContainer= el("entityIdsContainer");
const entityIdsCount    = el("entityIdsCount");
const entityIdsGET      = el("entityIdsGET")    as HTMLAnchorElement;
const entityIdsInput    = el("entityIdsInput")  as HTMLInputElement;
const entityIdsReload   = el("entityIdsReload");


const entityFilter      = el("entityFilter")    as HTMLInputElement;
const filterRow         = el("filterRow");
const commandSignature  = el("commandSignature");
const commandLink       = el("commandLink");

// request response editor
const requestContainer       = el("requestContainer");
const responseContainer      = el("responseContainer")
// entity/command editor
const commandValueContainer  = el("commandValueContainer");
const commandParamBar        = el("commandParamBar");
const commandValue           = el("commandValue");
const entityContainer        = el("entityContainer");

/* if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register("./sw.js").then(registration => {
        console.log("SW registered");
    }).catch(error => {
        console.error(`SW failed: ${error}`);
    });
} */


class App {

    connectWebsocket () {
        if (connection) {
            connection.close();
            connection = null;
        }
        const loc     = window.location;
        const nr      = ("" + (++websocketCount)).padStart(3, "0");
        const uri     = `ws://${loc.host}/ws-${nr}`;
        // const uri  = `ws://google.com:8080/`; // test connection timeout
        socketStatus.innerHTML = 'connecting <span class="spinner"></span>';
        try {
            connection = new WebSocket(uri);
        } catch (err) {
            socketStatus.innerText = "connect failed: err";
            return;
        }
        connection.onopen = () => {
            socketStatus.innerHTML = "connected <small>🟢</small>";
            console.log('WebSocket connected');
            req         = 1;
            subCount    = 0;
        };

        connection.onclose = (e) => {
            socketStatus.innerText = "closed (code: " + e.code + ")";
            responseState.innerText = "";
            console.log('WebSocket closed');
        };

        // Log errors
        connection.onerror = (error) => {
            socketStatus.innerText = "error";
            console.log('WebSocket Error ' + error);
        };

        // Log messages from the server
        connection.onmessage = (e) => {
            const duration = new Date().getTime() - requestStart;
            const data = JSON.parse(e.data);
            // console.log('server:', e.data);
            switch (data.msg) {
            case "resp":
            case "error":
                clt = data.clt;
                cltElement.innerText    = clt ?? " - ";
                const content           = this.formatJson(this.config.formatResponses, e.data);
                this.responseModel.setValue(content)
                responseState.innerHTML = `· ${duration} ms`;
                break;
            case "ev":
                subscriptionCount.innerText = String(++subCount);
                subSeq = data.seq;
                // multiple clients can use the same WebSocket. Use the latest
                if (clt == data.clt) {
                    subscriptionSeq.innerText   = subSeq ? String(subSeq) : " - ";
                    ackElement.innerText        = subSeq ? String(subSeq) : " - ";
                }
                break;
            }
        };
    }

    closeWebsocket  () {
        connection.close();
    }

    getCookie  (name: string) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2)
            return parts.pop().split(';').shift();
        return null;
    }

    initUserToken  () {
        const user    = this.getCookie("fliox-user")   ?? "admin";
        const token   = this.getCookie("fliox-token")  ?? "admin";
        this.setUser(user);
        this.setToken(token);
    }

    setUser (user: string) {
        defaultUser.value   = user;
        document.cookie = `fliox-user=${user};`;
    }

    setToken  (token: string) {
        defaultToken.value  = token;
        document.cookie = `fliox-token=${token};`;
    }

    selectUser (element: HTMLElement) {
        let value = element.innerText;
        this.setUser(value);
        this.setToken(value);
    };

    addUserToken (jsonRequest: string) {
        const endBracket  = jsonRequest.lastIndexOf("}");
        if (endBracket == -1)
            return jsonRequest;
        const before      = jsonRequest.substring(0, endBracket);
        const after       = jsonRequest.substring(endBracket);
        let   userToken   = JSON.stringify({ user: defaultUser.value, token: defaultToken.value});
        userToken       = userToken.substring(1, userToken.length - 1);
        return `${before},${userToken}${after}`;
    }

    sendSyncRequest () {
        if (!connection || connection.readyState != 1) { // 1 == OPEN {
            this.responseModel.setValue(`Request ${req} failed. WebSocket not connected`)
            responseState.innerHTML = "";
        } else {
            let jsonRequest = this.requestModel.getValue();
            jsonRequest = this.addUserToken(jsonRequest);
            try {
                const request     = JSON.parse(jsonRequest);
                if (request) {
                    // Enable overrides of WebSocket specific members
                    if (request.req !== undefined) { req      = request.req; }
                    if (request.ack !== undefined) { subSeq   = request.ack; }
                    if (request.clt !== undefined) { clt      = request.clt; }
                    
                    // Add WebSocket specific members to request
                    request.req     = req;
                    request.ack     = subSeq;
                    if (clt) {
                        request.clt     = clt;
                    }
                }
                jsonRequest = JSON.stringify(request);                
            } catch { }
            responseState.innerHTML = '<span class="spinner"></span>';
            connection.send(jsonRequest);
            requestStart = new Date().getTime();
        }
        req++;
        reqIdElement.innerText  =  String(req);
    }

    async postSyncRequest () {
        let jsonRequest         = this.requestModel.getValue();
        jsonRequest             = this.addUserToken(jsonRequest);
        responseState.innerHTML = '<span class="spinner"></span>';
        let start = new Date().getTime();
        let duration: number;
        try {
            const response  = await this.postRequest(jsonRequest, "POST");
            let content     = await response.text;
            content         = this.formatJson(this.config.formatResponses, content);
            duration        = new Date().getTime() - start;
            this.responseModel.setValue(content);
        } catch(error) {
            duration = new Date().getTime() - start;
            this.responseModel.setValue("POST error: " + error.message);
        }
        responseState.innerHTML = `· ${duration} ms`;
    }

    lastCtrlKey:        boolean;
    refLinkDecoration:  CSSStyleRule;

    applyCtrlKey(event: KeyboardEvent) {
        if (this.lastCtrlKey == event.ctrlKey)
            return;
        this.lastCtrlKey = event.ctrlKey;
        if (!this.refLinkDecoration) {
            const cssRules = document.styleSheets[0].cssRules;
            for (let n = 0; n < cssRules.length; n++) {
                const rule = cssRules[n] as CSSStyleRule;
                if (rule.selectorText == ".refLinkDecoration:hover")
                    this.refLinkDecoration = rule;
            }
        }
        this.refLinkDecoration.style.cursor = this.lastCtrlKey ? "pointer" : "";
    }

    onKeyUp (event: KeyboardEvent) {
        if (event.code == "ControlLeft")
            this.applyCtrlKey(event);
    }

    onKeyDown (event: KeyboardEvent) {
        if (event.code == "ControlLeft")
            this.applyCtrlKey(event);

        switch (this.config.activeTab) {
        case "playground":
            if (event.code == 'Enter' && event.ctrlKey && event.altKey) {
                this.sendSyncRequest();
                event.preventDefault();
            }
            if (event.code == 'KeyP' && event.ctrlKey && event.altKey) {
                this.postSyncRequest();
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
                        this.execute(event, () => this.saveEntitiesAction());
                    break;
                case 'KeyP':
                    if (event.ctrlKey && event.altKey)
                        this.execute(event, () => this.sendCommand("POST"));
                    break;
                case 'ArrowLeft':
                    if (event.altKey)
                        this.execute(event, () => this.navigateEntity(this.entityHistoryPos - 1));
                    break;        
                case 'ArrowRight':
                    if (event.altKey)
                        this.execute(event, () => this.navigateEntity(this.entityHistoryPos + 1));
                    break;        
                }
        }
        // console.log(`KeyboardEvent: code='${event.code}', ctrl:${event.ctrlKey}, alt:${event.altKey}`);
    }

    execute(event: KeyboardEvent, lambda: () => void) {
        lambda();
        event.preventDefault();
    }

    // --------------------------------------- example requests ---------------------------------------
    async onExampleChange () {
        const exampleName = selectExample.value;
        if (exampleName == "") {
            this.requestModel.setValue("")
            return;
        }
        const response = await fetch(exampleName);
        const example = await response.text();
        this.requestModel.setValue(example)
    }

    async loadExampleRequestList () {
        // [html - How do I make a placeholder for a 'select' box? - Stack Overflow] https://stackoverflow.com/questions/5805059/how-do-i-make-a-placeholder-for-a-select-box
        let option      = createEl("option");
        option.value    = "";
        option.disabled = true;
        option.selected = true;
        option.hidden   = true;
        option.text     = "Select request ...";
        selectExample.add(option);

        const folder    = './example-requests'
        const response  = await fetch(folder);
        if (!response.ok)
            return;
        const exampleRequests   = await response.json();
        let   groupPrefix       = "0";
        let   groupCount        = 0;
        for (const example of exampleRequests) {
            if (!example.endsWith(".json"))
                continue;
            const name = example.substring(folder.length).replace(".sync.json", "");
            if (groupPrefix != name[0]) {
                groupPrefix = name[0];
                groupCount++;
            }
            option = createEl("option");
            option.value                    = example;
            option.text                     = (groupCount % 2 ? "\xA0\xA0" : "") + name;
            option.style.backgroundColor    = groupCount % 2 ? "#ffffff" : "#eeeeff";
            selectExample.add(option);
        }
    }
    // --------------------------------------- Explorer ---------------------------------------
  

    async postRequest (request: string, tag: string) {
        let init = {        
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    request
        }
        try {
            const path          = `./?${tag}`;
            const rawResponse   = await fetch(path, init);
            const text          = await rawResponse.text();
            return {
                text: text,
                json: JSON.parse(text)
            };            
        } catch (error) {
            return {
                text: error.message,
                json: {
                    "msg":    "error",
                    "message": error.message
                }
            };
        }
    }

    async postRequestTasks (database: string, tasks: SyncRequestTask_Union[], tag: string) {
        const db = database == "main_db" ? undefined : database;
        const sync: SyncRequest = {
            "msg":      "sync",
            "database": db,
            "tasks":    tasks,
            "user":     defaultUser.value,
            "token":    defaultToken.value
        }
        const request = JSON.stringify(sync);
        tag = tag ? tag : "";
        return await this.postRequest(request, `${database}/${tag}`);
    }

    static getRestPath(database: string, container: string, ids: string | string[], query: string) {
        let path = `./rest/${database}`;
        if (container)  path = `${path}/${container}`;
        if (ids) {
            if (Array.isArray(ids)) {
                path = `${path}?ids=${ids.join(',')}`;
            } else {
                path = `${path}/${ids}`;
            }
        }
        if (query)      path = `${path}?${query}`;
        return path;
    }

    static async restRequest (method: Method, body: string, database: string, container: string, ids: string | string[], query: string) {
        const path = App.getRestPath(database, container, ids, query);        
        const init = {        
            method:  method,
            headers: { 'Content-Type': 'application/json' },
            body:    body
        }
        try {
            // authenticate with cookies: "fliox-user" & "fliox-token"
            return await fetch(path, init);
        } catch (error) {
            return {
                ok:     false,
                status:     0,
                statusText: "exception",
                text:   () : string => error.message,
                json:   ()          => { throw error.message }
            }
        }
    }

    static getTaskError (content: ProtocolResponse_Union, taskIndex: number) {
        if (content.msg == "error") {
            return content.message;
        }
        const task = content.tasks[taskIndex];
        if (task.task == "error")
            return "task error:\n" + task.message;
        return undefined;
    }

    static bracketValue = /\[(.*?)\]/;

    static errorAsHtml (message: string, p: Resource | null) {
        // first line: error type, second line: error message
        const pos = message.indexOf(' > ');
        let error = message;
        if (pos > 0) {
            let reason = message.substring(pos + 3);
            if (reason.startsWith("at ")) {
                const id = reason.match(App.bracketValue)[1];
                if (p && id) {
                    const c: Resource   = { database: p.database, container: p.container, ids: [id] };
                    const coordinate    = JSON.stringify(c);
                    const link = `<a  href="#" onclick='app.loadEntities(${coordinate})'>${id}</a>`;
                    reason = reason.replace(id, link);
                }
                reason = reason.replace("] ", "]<br>");
            }
            error =  message.substring(0, pos) + " ><br>" + reason;
        }
        return `<code style="white-space: pre-line; color:red">${error}</code>`;
    }

    static setClass(element: Element, enable: boolean, className: string) {
        const classList = element.classList;
        if (enable) {
            classList.add(className);
            return;
        }
        classList.remove(className);        
    }

    toggleDescription() {
        this.changeConfig("showDescription", !this.config.showDescription);   
        this.openTab(this.config.activeTab);
    }

    openTab (tabName: string) {
        const config            = this.config;
        config.activeTab        = tabName;
        App.setClass(document.body, !config.showDescription, "miniHeader")
        const tabContents       = document.getElementsByClassName("tabContent");
        const tabs              = document.getElementsByClassName("tab");
        const gridTemplateRows  = document.body.style.gridTemplateRows.split(" ");
        const headerHeight      = getComputedStyle(document.body).getPropertyValue('--header-height');
        gridTemplateRows[0]     = config.showDescription ? headerHeight : "0";
        for (let i = 0; i < tabContents.length; i++) {
            const tabContent            = tabContents[i] as HTMLElement;
            const isActiveContent       = tabContent.id == tabName;
            tabContent.style.display    = isActiveContent ? "grid" : "none";
            gridTemplateRows[i + 2]     = isActiveContent ? "1fr" : "0"; // + 2  ->  "body-header" & "body-tabs"
            const isActiveTab           = tabs[i].getAttribute('value') == tabName;
            App.setClass(tabs[i], isActiveTab, "selected");
        }
        document.body.style.gridTemplateRows = gridTemplateRows.join(" ");
        this.layoutEditors();
        if (tabName != "settings") {
            this.setConfig("activeTab", tabName);
        }
    }

    selectedCatalog: HTMLElement;
    selectedCommand = undefined as HTMLElement;

    setSelectedCommand(element: HTMLElement) {
        this.selectedCommand?.classList.remove("selected");
        this.selectedCommand = element;
        element.classList.add("selected");        
    }

    hubInfo = { } as DbHubInfo;

    async loadCluster () {
        const tasks: SyncRequestTask_Union[] = [
            { "task": "query",  "container": "containers"},
            { "task": "query",  "container": "schemas"},
            { "task": "query",  "container": "commands"},
            { "task": "command","name": "DbHubInfo" }
        ];
        catalogExplorer.innerHTML = 'read databases <span class="spinner"></span>';
        const response  = await this.postRequestTasks("cluster", tasks, null);
        const content   = response.json as SyncResponse;
        const error     = App.getTaskError (content, 0);
        if (error) {
            catalogExplorer.innerHTML = App.errorAsHtml(error, null);
            return 
        }
        const dbContainers  = content.containers[0].entities    as DbContainers[];
        const dbSchemas     = content.containers[1].entities    as DbSchema[];
        const dbCommands    = content.containers[2].entities    as DbCommands[];
        const hubInfoResult = content.tasks[3]                  as SendCommandResult;
        this.hubInfo        = hubInfoResult.result              as DbHubInfo;
        //
        let   description   = this.hubInfo.description
        const website       = this.hubInfo.website
        if (description || website) {
            if (!description)
                description = "Website";
            hubInfoEl.innerHTML = website ? `<a href="${website}" target="_blank" rel="noopener noreferrer">${description}</a>` : description;
        }

        const ulCatalogs = createEl('ul');
        ulCatalogs.onclick = (ev) => {
            const path = ev.composedPath() as HTMLElement[];
            const selectedElement = path[0];
            if (selectedElement.classList.contains("caret")) {
                path[2].classList.toggle("active");
                return;
            }
            path[1].classList.add("active");
            if (this.selectedCatalog) this.selectedCatalog.classList.remove("selected");
            this.selectedCatalog =selectedElement;
            selectedElement.classList.add("selected");
            const databaseName      = selectedElement.childNodes[1].textContent;
            const commands          = dbCommands.find   (c => c.id == databaseName);
            const containers        = dbContainers.find (c => c.id == databaseName);
            this.listCommands(databaseName, commands, containers);
        }
        let firstDatabase = true;
        for (const dbContainer of dbContainers) {
            const liCatalog       = createEl('li');
            if (firstDatabase) {
                firstDatabase = false;
                liCatalog.classList.add("active");
            }
            const liDatabase            = createEl('div');
            const catalogCaret          = createEl('div');
            catalogCaret.classList.value= "caret";
            const catalogLabel          = createEl('span');
            catalogLabel.innerText      = dbContainer.id;
            liDatabase.title            = "database";
            catalogLabel.style.pointerEvents = "none"
            liDatabase.append(catalogCaret)
            liDatabase.append(catalogLabel)
            liCatalog.appendChild(liDatabase);
            ulCatalogs.append(liCatalog);

            const ulContainers = createEl('ul');
            ulContainers.onclick = (ev) => {
                ev.stopPropagation();
                const path = ev.composedPath() as HTMLElement[];
                const selectedElement = path[0];
                // in case of a multiline text selection selectedElement is the parent
                if (selectedElement.tagName.toLowerCase() != "div")
                    return;
                if (this.selectedCatalog) this.selectedCatalog.classList.remove("selected");
                this.selectedCatalog    = selectedElement;
                this.selectedCatalog.classList.add("selected");
                const containerName     = this.selectedCatalog.innerText.trim();
                const databaseName      = path[3].childNodes[0].childNodes[1].textContent;
                const params: Resource  = { database: databaseName, container: containerName, ids: [] };
                this.clearEntity(databaseName, containerName);
                this.loadContainer(params, null);
            }
            liCatalog.append(ulContainers);
            for (const containerName of dbContainer.containers) {
                const liContainer       = createEl('li');
                liContainer.title       = "container";
                const containerLabel    = createEl('div');
                containerLabel.innerHTML= "&nbsp;" + containerName;
                liContainer.append(containerLabel)
                ulContainers.append(liContainer);
            }
        }
        this.createEntitySchemas(dbSchemas)
        catalogExplorer.textContent = "";
        catalogExplorer.appendChild(ulCatalogs);

        this.listCommands(dbCommands[0].id, dbCommands[0], dbContainers[0]);
    }

    databaseSchemas: { [key: string]: DbSchema} = {};

    createEntitySchemas (dbSchemas: DbSchema[]) {
        const schemaMap: { [key: string]: MonacoSchema } = {};
        for (const dbSchema of dbSchemas) {
            const jsonSchemas       = dbSchema.jsonSchemas as { [key: string] : JsonSchema};
            const database          = dbSchema.id;
            const containerRefs     = {} as { [key: string] : string };
            const rootSchema        = jsonSchemas[dbSchema.schemaPath].definitions[dbSchema.schemaName];
            dbSchema._rootSchema    = rootSchema;
            const containers        = rootSchema.properties;
            for (const containerName in containers) {
                const container = containers[containerName];
                containerRefs[container.additionalProperties.$ref] = containerName;
            }
            this.databaseSchemas[database]  = dbSchema;
            dbSchema._containerSchemas      = {}

            // add all schemas and their definitions to schemaMap and map them to an uri like:
            //   http://main_db/Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json
            //   http://main_db/Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json#/definitions/PocStore
            for (const schemaPath in jsonSchemas) {
                const schema      = jsonSchemas[schemaPath];
                const uri         = "http://" + database + "/" + schemaPath;
                const schemaEntry: MonacoSchema = {
                    uri:            uri,
                    schema:         schema,
                    fileMatch:      [], // can have multiple in case schema is used by multiple editor models
                    _resolvedDef:   schema // not part of monaco > DiagnosticsOptions.schemas
                }
                const namespace     = schemaPath.substring(0, schemaPath.length - ".json".length);
                schemaMap[uri]      = schemaEntry;
                const definitions   = schema.definitions;
                const baseRefType   = schema.$ref ? schema.$ref.substring('#/definitions/'.length) : undefined;
                for (const definitionName in definitions) {
                    const definition        = definitions[definitionName];
                    definition._typeName    = definitionName;
                    definition._namespace   = namespace;
                    if (definitionName == baseRefType) {
                        definition._namespace = namespace.substring(0, namespace.length - definitionName.length - 1);
                    }
                    // console.log("---", definition._namespace, definitionName);
                    const path          = "/" + schemaPath + "#/definitions/" + definitionName;
                    const schemaId      = "." + path
                    const uri           = "http://" + database + path;
                    const containerName = containerRefs[schemaId];
                    let schemaRef : any = { $ref: schemaId };
                    if (containerName) {
                        dbSchema._containerSchemas[containerName] = definition;
                        // entityEditor type can either be its entity type or an array using this type
                        schemaRef = { "oneOf": [schemaRef, { type: "array", items: schemaRef } ] };
                    }
                    // add reference for definitionName pointing to definition in current schemaPath
                    const definitionEntry: MonacoSchema = {
                        uri:            uri,
                        schema:         schemaRef,
                        fileMatch:      [], // can have multiple in case schema is used by multiple editor models
                        _resolvedDef:   definition // not part of monaco > DiagnosticsOptions.schemas
                    }
                    schemaMap[uri] = definitionEntry;
                }
            }
            App.resolveRefs(jsonSchemas);
            this.addFileMatcher(database, dbSchema, schemaMap);
        }
        const monacoSchemas = Object.values(schemaMap);
        this.addSchemas(monacoSchemas);
    }

    static resolveRefs(jsonSchemas: { [key: string] : JsonSchema }) {
        for (const schemaPath in jsonSchemas) {
            // if (schemaPath == "Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Order.json") debugger;
            const schema      = jsonSchemas[schemaPath];
            App.resolveNodeRefs(jsonSchemas, schema, schema);
        }
    }

    static resolveNodeRefs(jsonSchemas: { [key: string] : JsonSchema }, schema: JsonSchema, node: JsonSchema) {
        const nodeType = typeof node;
        if (nodeType != "object")
            return;
        if (Array.isArray(node))
            return;
        const ref = node.$ref;
        if (ref) {
            if (ref[0] == "#") {
                const localName     = ref.substring("#/definitions/".length);
                node._resolvedDef   = schema.definitions[localName];
            } else {
                const localNamePos  = ref.indexOf ("#");
                const schemaPath    = ref.substring(2, localNamePos); // start after './'
                const localName     = ref.substring(localNamePos + "#/definitions/".length);
                const globalSchema  = jsonSchemas[schemaPath];
                node._resolvedDef   = globalSchema.definitions[localName];
            }
        }
        for (const propertyName in node) {
            if (propertyName == "_resolvedDef")
                continue;
            // if (propertyName == "dateTimeNull") debugger;
            const property  = (node as any)[propertyName] as FieldType;
            const oneOf     = property.oneOf;
            if (oneOf) {
                for (const oneOfType of oneOf) {
                    if (oneOfType.type == "null")
                        continue;
                    this.resolveNodeRefs(jsonSchemas, schema, oneOfType as JsonSchema); // todo fix cast
                }
                continue;                
            }
            this.resolveNodeRefs(jsonSchemas, schema, property as JsonSchema); // todo fix cast            
        }
    }

    // add a "fileMatch" property to all container entity type schemas used for editor validation
    addFileMatcher(database: string, dbSchema: DbSchema, schemaMap: { [key: string]: MonacoSchema }) {
        const jsonSchemas     = dbSchema.jsonSchemas as { [key: string] : JsonSchema};
        const schemaName      = dbSchema.schemaName;
        const schemaPath      = dbSchema.schemaPath;
        const jsonSchema      = jsonSchemas[schemaPath];
        const dbType          = jsonSchema.definitions[schemaName];
        const containers      = dbType.properties;
        for (const containerName in containers) {
            const container     = containers[containerName];
            const containerType = App.getResolvedType(container.additionalProperties, schemaPath);
            const uri       = "http://" + database + containerType.$ref.substring(1);
            const schema    = schemaMap[uri];
            const url       = `entity://${database}.${containerName.toLocaleLowerCase()}.json`;
            schema.fileMatch.push(url); // requires a lower case string
        }
        const commandType     = jsonSchema.definitions[schemaName];
        const commands        = commandType.commands;
        for (const commandName in commands) {
            const command   = commands[commandName];
            // assign file matcher for command param
            const paramType = App.getResolvedType(command.param, schemaPath);
            let url = `command-param://${database}.${commandName.toLocaleLowerCase()}.json`;
            if (paramType.$ref) {
                const uri       = "http://" + database + paramType.$ref.substring(1);
                const schema    = schemaMap[uri];
                schema.fileMatch.push(url); // requires a lower case string
            } else {
                // uri is never referenced - create an arbitrary unique uri
                const uri = "http://" + database + "/command/param" + commandName;
                const schema: MonacoSchema = {
                    uri:        uri,
                    schema:     paramType,
                    fileMatch:  [url]
                };
                schemaMap[uri] = schema;
            }
            // assign file matcher for command result
            const resultType   = App.getResolvedType(command.result, schemaPath);
            url = `command-result://${database}.${commandName.toLocaleLowerCase()}.json`;
            if (resultType.$ref) {
                const uri       = "http://" + database + resultType.$ref.substring(1);
                const schema    = schemaMap[uri];
                schema.fileMatch.push(url); // requires a lower case string
            } else {
                // uri is never referenced - create an arbitrary unique uri
                const uri    = "http://" + database + "/command/result" + commandName;
                const schema = {
                    uri:        uri,
                    schema:     resultType,
                    fileMatch: [url]
                };
                schemaMap[uri] = schema;
            }
        }
    }

    static getResolvedType (type: FieldType, schemaPath: string) {
        const $ref = type.$ref;
        if (!$ref)
            return type;
        if ($ref[0] != "#")
            return type;
        return { $ref: "./" + schemaPath + $ref };
    }

    
    getSchemaType(database: string) {
        const schema        = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;
        return `<a title="open database schema in new tab" href="./schema/${database}/html/schema.html" target="${database}">${schema.schemaName}</a>`;
    }

    getSchemaExports(database: string) {
        const schema        = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;
        return `<a title="open database schema in new tab" href="./schema/${database}/index.html" target="${database}">Typescript, C#, Kotlin, JSON Schema, HTML</a>`;
    }

    static getType(database: string, def: JsonType) {
        const ns          = def._namespace;
        const name        = def._typeName;
        return `<a title="open type definition in new tab" href="./schema/${database}/html/schema.html#${ns}.${name}" target="${database}">${name}</a>`;
    }

    getEntityType(database: string, container: string) {
        const def  = this.getContainerSchema(database, container);
        if (!def)
            return this.schemaLess;
        return App.getType(database, def);
    }

    getTypeLabel(database: string, type: FieldType) {
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

    schemaLess = '<span title="missing type definition - schema-less database" style="opacity:0.5">unknown</span>';

    static getDatabaseLink(database: string) {
        return `<a title="open database in new tab" href="./rest/${database}" target="_blank" rel="noopener noreferrer">${database}</a>`
    }

    setEditorHeader(show: "entity" | "command" | "none") {
        const displayEntity  = show == "entity"  ? "contents" : "none";
        const displayCommand = show == "command" ? "contents" : "none";
        el("entityTools")  .style.display = displayEntity;        
        el("entityHeader") .style.display = displayEntity;        
        el("commandTools") .style.display = displayCommand;
        el("commandHeader").style.display = displayCommand;
    }

    getCommandTags(database: string, command: string, signature: CommandType) {
        let label = this.schemaLess;
        if (signature) {
            const param   = this.getTypeLabel(database, signature.param);
            const result  = this.getTypeLabel(database, signature.result);
            label = `<span title="command parameter type"><span style="opacity: 0.5;">(param:</span> <span>${param}</span></span><span style="opacity: 0.5;">) : </span><span title="command result type">${result}</span>`
        }
        const link    = `command=${command}`;
        const url     = `./rest/${database}?command=${command}`;
        return {
            link:   `<a id="commandAnchor" title="command" onclick="app.sendCommand()" href="${url}" target="_blank" rel="noopener noreferrer">${link}</a>`,
            label:  label
        }
    }

    async sendCommand(method: Method) {
        const value     = this.commandValueEditor.getValue();
        const database  = this.entityIdentity.database;
        const command   = this.entityIdentity.command;
        if (!method) {
            const commandAnchor =  el("commandAnchor") as HTMLAnchorElement;
            let commandValue    = value == "null" ? "" : `&value=${value}`;
            const path          = App.getRestPath( database, null, null, `command=${command}${commandValue}`)
            commandAnchor.href  = path;
            // window.open(path, '_blank');
            return;
        }
        const response  = await App.restRequest(method, value, database, null, null, `command=${command}`);
        let content     = await response.text();
        content         = this.formatJson(this.config.formatResponses, content);
        this.entityEditor.setValue(content);
    }

    setDatabaseInfo(database: string, dbContainer: DbContainers) {
        el("databaseName").innerHTML      = App.getDatabaseLink(database);
        el("databaseSchema").innerHTML    = this.getSchemaType(database);
        el("databaseExports").innerHTML   = this.getSchemaExports(database);
        el("databaseType").innerHTML      = dbContainer.databaseType;        
    }

    listCommands (database: string, dbCommands: DbCommands, dbContainer: DbContainers) {
        this.setDatabaseInfo(database, dbContainer);
        this.setExplorerEditor("dbInfo");

        catalogSchema.innerHTML     = this.getSchemaType(database)
        this.setEditorHeader("none");
        filterRow.style.visibility  = "hidden";
        entityFilter.style.visibility  = "hidden";
        readEntitiesDB.innerHTML    = App.getDatabaseLink(database);
        readEntities.innerHTML      = "";

        const ulDatabase            = createEl('ul');
        ulDatabase.classList.value  = "database"
        const commandLabel  = createEl('li');
        const label         = '<small style="opacity:0.5; margin-left: 10px;" title="open database commands in new tab">&nbsp;commands</small>';
        commandLabel.innerHTML = `<a href="./rest/${database}?command=DbCommands" target="_blank" rel="noopener noreferrer">${label}</a>`;
        ulDatabase.append(commandLabel);

        const liCommands  = createEl('li');
        ulDatabase.appendChild(liCommands);

        const ulCommands    = createEl('ul');
        ulCommands.onclick  = (ev) => {
            this.setEditorHeader("command");
            const path      = ev.composedPath() as HTMLElement[];
            let selectedElement = path[0];
            // in case of a multiline text selection selectedElement is the parent

            const tagName = selectedElement.tagName;
            if (tagName == "SPAN" || tagName == "DIV") {
                selectedElement = path[1];
            }
            const commandName = selectedElement.children[0].textContent;
            this.setSelectedCommand(selectedElement);
            this.showCommand(database, commandName);

            if (path[0].classList.contains("command")) {
                this.sendCommand("POST");
            }
        }
        for (const command of dbCommands.commands) {
            const liCommand             = createEl('li');
            const commandLabel          = createEl('div');
            commandLabel.innerText      = command;
            liCommand.appendChild(commandLabel);
            const runCommand            = createEl('div');
            runCommand.classList.value  = "command";
            runCommand.title            = "POST command"
            liCommand.appendChild(runCommand);

            ulCommands.append(liCommand);
        }
        entityExplorer.innerText = ""
        liCommands.append(ulCommands);
        entityExplorer.appendChild(ulDatabase);
    }

    filter = {} as {
        database:   string,
        container:  string
    }

    filterOnKeyDown(event: KeyboardEvent) {
        if (event.code != 'Enter')
            return;
        this.applyFilter();
    }

    applyFilter() {
        const database  = this.filter.database;
        const container = this.filter.container;
        const filter    = entityFilter.value;
        const query     = filter.trim() == "" ? null : `filter=${encodeURIComponent(filter)}`;
        const params: Resource    = { database: database, container: container, ids: [] };
        this.saveFilter(database, container, filter)
        this.loadContainer(params, query);
    }

    removeFilter() {
        const params: Resource  = { database: this.filter.database, container: this.filter.container, ids: [] };
        this.loadContainer(params, null);
    }

    saveFilter(database: string, container: string, filter: string) {
        const filters = this.config.filters;
        if (filter.trim() == "") {
            const filterDatabase = filters[database];
            if (filterDatabase) {
                delete filterDatabase[container];
            }
        } else {
            if (!filters[database]) filters[database] = {}            
            filters[database][container] = [filter];
        }
        this.setConfig("filters", filters);
    }

    updateFilterLink() {
        const filter    = entityFilter.value;
        const query     = filter.trim() == "" ? "" : `?filter=${encodeURIComponent(filter)}`;
        const url       = `./rest/${this.filter.database}/${this.filter.container}${query}`;
        el<HTMLAnchorElement>("filterLink").href = url;
    }

    explorer: {
        database:           string;
        container:          string;
        entityType?:        JsonType;
        focusedCell?:       HTMLTableCellElement;
        cachedJsonValue?:   string;
        cachedJsonAst?:     jsonToAst.ValueNode;
    }
    explorerTable:  HTMLTableElement;

    async loadContainer (p: Resource, query: string) {
        const storedFilter  = this.config.filters[p.database]?.[p.container];
        const filter        = storedFilter && storedFilter[0] ? storedFilter[0] : "";        
        entityFilter.value  = filter;

        const removeFilterVisibility = query ? "" : "hidden";
        el("removeFilter").style.visibility = removeFilterVisibility;
        
        const entityType        = this.getContainerSchema(p.database, p.container);
        this.filter.database    = p.database;
        this.filter.container   = p.container;
        this.explorer = {
            database:   p.database,
            container:  p.container,
            entityType: entityType
        };        
        // const tasks =  [{ "task": "query", "container": p.container, "filterJson":{ "op": "true" }}];
        filterRow.style.visibility      = "";
        entityFilter.style.visibility   = "";
        catalogSchema.innerHTML  = this.getSchemaType(p.database) + ' · ' + this.getEntityType(p.database, p.container);
        readEntitiesDB.innerHTML = App.getDatabaseLink(p.database) + "/";
        const containerLink      = `<a title="open container in new tab" href="./rest/${p.database}/${p.container}" target="_blank" rel="noopener noreferrer">${p.container}/</a>`;
        readEntities.innerHTML   = `${containerLink}<span class="spinner"></span>`;

        const response           = await App.restRequest("GET", null, p.database, p.container, null, query);

        const reload = `<span class="reload" title='reload container' onclick='app.loadContainer(${JSON.stringify(p)})'></span>`
        writeResult.innerHTML   = "";        
        readEntities.innerHTML  = containerLink + reload;
        if (!response.ok) {
            const error = await response.text();
            entityExplorer.innerHTML = App.errorAsHtml(error, p);
            return;
        }

        let     entities    = await response.json() as Entity[];
        // const ids        = entities.map(entity => entity[keyName]) as string[];
        const   table       = this.explorerTable = createEl('table');

        this.entityFields   = {};
        const   head        = this.createExplorerHead(entityType, this.entityFields);


        table.append(head);
        table.classList.value   = "entities"
        table.onclick = async (ev) => {
            const path          = ev.composedPath() as HTMLElement[];
            const select        = ev.ctrlKey ? "toggle" : "id"
            const selectedIds   = this.getSelectionFromPath(path, select);
            if (selectedIds === null)
                return;
            this.setSelectedEntities(selectedIds);
            const params: Resource  = { database: p.database, container: p.container, ids: selectedIds };
            await this.loadEntities(params, false, null);

            const json  = this.entityEditor.getValue();
            const ast   = this.getAstFromJson(json);
            this.selectEditorValue(ast, this.explorer.focusedCell);
        }
        this.explorerEntities = {};
        this.selectedEntities = {};
        this.updateExplorerEntities(entities, entityType);
        this.setColumnWidths();
        entityExplorer.innerText = "";
        entityExplorer.appendChild(table);
    }

    getAstFromJson(json: string) : jsonToAst.ValueNode | null {
        if (json == "")
            return null;
        const explorer = this.explorer;
        if (json == explorer.cachedJsonValue)
            return explorer.cachedJsonAst;
        const ast = App.parseAst(json);
        explorer.cachedJsonAst      = ast;
        explorer.cachedJsonValue    = json;
        return ast;
    }

    selectEditorValue(ast: jsonToAst.ValueNode, focus: HTMLTableCellElement) {
        if (!ast || !focus)
            return;
        const row       = focus.parentNode as HTMLTableRowElement;
        const th        = this.explorerTable.rows[0].cells[focus.cellIndex];
        const thDiv     = th.children[0] as HTMLDivElement;
        const path      = thDiv.innerText;
        const keyName   = App.getEntityKeyName(this.explorer.entityType);
        const id        = row.cells[1].innerText;
        const range     = App.findPathRange(ast, path, keyName, id);
        if (range.entity) {
            this.entityEditor.revealRange(range.entity);
        }
        if (range.value) {
            this.entityEditor.setSelection(range.value);
            this.entityEditor.revealRange (range.value);            
        } else {
            // clear editor selection as focused cell not found in editor value
            const pos               = this.entityEditor.getPosition();
            const line              = pos.lineNumber    ?? 0;
            const column            = pos.column        ?? 0; 
            const clearedSelection  = new monaco.Selection(line, column, line, column);
            this.entityEditor.setSelection(clearedSelection);
            // console.log("path not found:", path)
        }        
    }

    getSelectionFromPath(path: HTMLElement[], select: "toggle" | "id") : string[] {
        // in case of a multiline text selection selectedElement is the parent
        const td = path[0];
        if (td.tagName != "TD")
            return null;
        const cell          = td as HTMLTableCellElement;
        const row           = td.parentElement as HTMLTableRowElement;
        const children      = path[1].children; // tr children
        const id            = (children[1] as HTMLElement).innerText;
        const isCheckbox    = td == children[0];
        if (isCheckbox || select == "toggle") {
            const selectedIds = Object.keys(this.selectedEntities);
            if (App.toggleIds(selectedIds, id) == "added") {
                const cellIndex = isCheckbox ? this.explorer.focusedCell?.cellIndex ?? 1 : cell.cellIndex;
                this.setFocusCell(row.rowIndex, cellIndex);
            }
            return selectedIds;
        }
        this.setFocusCell(row.rowIndex, cell.cellIndex);
        return [id];
    }

    static toggleIds(ids: string[], id: string) : "added" | "removed" {
        const index = ids.indexOf(id);
        if (index == -1) {
            ids.push(id);
            return "added";
        }
        ids.splice(index, 1);        
        return "removed";
    }

    setFocusCellSelectValue(rowIndex: number, cellIndex: number, scroll: "smooth" | null = null) {
        const td = this.setFocusCell(rowIndex, cellIndex, scroll);
        if (!td)
            return;
        const json  = this.entityEditor.getValue();
        const ast   = this.getAstFromJson(json);
        this.selectEditorValue(ast, td);
    }

    setFocusCell(rowIndex: number, cellIndex: number, scroll: "smooth" | null = null) : HTMLTableCellElement | null {
        const table = this.explorerTable;
        if (rowIndex < 1 || cellIndex < 1)
            return null;
        const rows = table.rows;
        if (rowIndex >= rows.length)
            return null;
        const row = rows[rowIndex];
        if (cellIndex >= row.cells.length)
            return null;

        const explorer = this.explorer;
        const td = row.cells[cellIndex];
        explorer.focusedCell?.classList.remove("focus");
        td.classList.add("focus");
        explorer.focusedCell = td;
        // td.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        App.ensureVisible(entityExplorer, td, 16, 22, scroll);
        return td;
    }

    // Chrome ignores { scroll-margin-top: 20px; scroll-margin-left: 16px; } for sticky header / first row 
    static ensureVisible(containerEl: HTMLElement, el: HTMLElement, offsetLeft: number, offsetTop: number, scroll: "smooth" | null) {
        const parentEl  = containerEl.parentElement;
        // const parent    = parentEl.getBoundingClientRect();
        // const container = containerEl.getBoundingClientRect();
        // const cell      = el.getBoundingClientRect();

        const width     = parentEl.clientWidth;
        const height    = parentEl.clientHeight;
        const x         = el.offsetLeft - offsetLeft;  // cell.x - offsetLeft - container.x;
        const y         = el.offsetTop  - offsetTop;   // cell.y - offsetTop  - container.y;

        const minLeft   = parentEl.scrollLeft;
        const minTop    = parentEl.scrollTop;
        const maxLeft   = minLeft + width  - el.clientWidth  - offsetLeft;
        const maxTop    = minTop  + height - el.clientHeight - offsetTop;

        if (x < minLeft ||
            y < minTop  ||
            x > maxLeft ||
            y > maxTop)
        {
            const left = x > maxLeft ? Math.min(x, el.offsetLeft + el.clientWidth  - width)  : Math.min (x, minLeft);
            const top  = y > maxTop  ? Math.min(y, el.offsetTop  + el.clientHeight - height) : Math.min (y, minTop);

            const smooth = scroll == "smooth" || top == parentEl.scrollTop;
            var opt: ScrollToOptions = { left, top, behavior: smooth ? "smooth" : undefined };
            parentEl.scrollTo(opt);
        }
    }

    explorerKeyDown(event: KeyboardEvent) {
        const td = this.explorer.focusedCell;
        if (!td)
            return;
        const explorer  = this.explorer;
        const table     = this.explorerTable;
        const row       = td.parentElement as HTMLTableRowElement;
        switch (event.code) {
            case 'Home':                
                if (event.ctrlKey) {
                    this.setFocusCellSelectValue (1, td.cellIndex, "smooth");
                } else {
                    this.setFocusCellSelectValue (row.rowIndex, 1, "smooth");
                }
                break;
            case 'End':
                if (event.ctrlKey) {
                    this.setFocusCellSelectValue (table.rows.length - 1, td.cellIndex, "smooth");
                } else {
                    this.setFocusCellSelectValue (row.rowIndex, row.cells.length - 1, "smooth");
                }                
                break;
            case 'PageUp':
                this.setFocusCellSelectValue(row.rowIndex - 3, td.cellIndex);
                break;                
            case 'PageDown':
                this.setFocusCellSelectValue(row.rowIndex + 3, td.cellIndex);
                break;                
            case 'ArrowUp':
                this.setFocusCellSelectValue(row.rowIndex - 1, td.cellIndex);
                break;
            case 'ArrowDown':
                this.setFocusCellSelectValue(row.rowIndex + 1, td.cellIndex);
                break;
            case 'ArrowLeft':
                this.setFocusCellSelectValue(row.rowIndex, td.cellIndex - 1);
                break;
            case 'ArrowRight':
                this.setFocusCellSelectValue(row.rowIndex, td.cellIndex + 1);
                break;
            case 'Space': {
                const id        = this.getRowId(row);
                const ids       = Object.keys(this.selectedEntities);
                App.toggleIds(ids, id);
                this.setSelectedEntities(ids);
                const params: Resource = { database: explorer.database, container: explorer.container, ids };
                this.loadEntities(params, false, null);
                break;
            }
            case 'Enter': {
                const ids               = [this.getRowId(row)];                
                this.setSelectedEntities(ids);
                const params: Resource  = { database: explorer.database, container: explorer.container, ids };
                this.loadEntities(params, false, null);
                break;
            }
            case 'KeyA': {
                if (!event.ctrlKey)
                    return;
                const ids               = Object.keys(this.explorerEntities);
                this.setSelectedEntities(ids);
                const params: Resource  = { database: explorer.database, container: explorer.container, ids };
                this.loadEntities(params, false, null);
                break;
            }
            case 'KeyC':
                if (!event.ctrlKey)
                    return;
                const editorValue = this.entityEditor.getValue();
                navigator.clipboard.writeText(editorValue);
                break;
            case 'Delete': {
                const ids   = Object.keys(this.selectedEntities);
                this.deleteEntities(explorer.database, explorer.container, ids);
                break;
            }
            default:
                return;
        }
        event.preventDefault();
    }

    getRowId(row: HTMLTableRowElement) : string {
        const keyName       = App.getEntityKeyName(this.explorer.entityType);
        const table         = this.explorerTable;
        const headerCells   = table.rows[0].cells;
        for (let i = 1; i < headerCells.length; i++) {
            if (headerCells[i].innerText != keyName)
                continue;
            return row.cells[i].innerText;
        }
        return null;
    }

    setSelectedEntities(ids: string[]) {
        for (const id in this.selectedEntities) {
            const entityEl = this.selectedEntities[id];
            entityEl.classList.remove("selected");
        }
        this.selectedEntities = this.findContainerEntities(ids);
        for (const id in this.selectedEntities) {
            this.selectedEntities[id].classList.add("selected");
        }
    }

    entityFields:       { [key: string] : Column }              = {}
    selectedEntities:   { [key: string] : HTMLTableRowElement } = {};
    explorerEntities:   { [key: string] : HTMLTableRowElement } = {};

    static getDataType(fieldType: FieldType) : DataType {
        const   ref = fieldType._resolvedDef;
        if (ref)
            return this.getDataType(ref as {} as FieldType);
        const oneOf = fieldType.oneOf;
        if (oneOf) {
            const jsonType = fieldType as { } as JsonType;
            if (jsonType.discriminator) {
                return { typeName: "object", jsonType: jsonType }
            }            
            for (const oneOfType of oneOf) {
                if (oneOfType.type == "null")
                    continue;
                return App.getDataType(oneOfType);
            }    
        }
        const type = fieldType.type;        
        if (type == "array") {
            const itemType = App.getDataType(fieldType.items);
            return { typeName: "array", jsonType: itemType.jsonType }
        }
        if (type == "object") {
            return { typeName: "object", jsonType: fieldType as {} as JsonType }
        }
        if (!Array.isArray(type))
            return { typeName: fieldType.type }
        for (const item of type) {
            if (item == "null")
                continue;
            return { typeName: item }
        }
        throw `missing type in type array`;      
    }

    static getColumnNames(columns: Column[], path: string[], fieldType: FieldType) {
        // if (path[0] == "jsonSchemas") debugger;
        const type:     DataType    = App.getDataType(fieldType);
        const typeName: TypeName    = type.typeName;
        switch (typeName) {
            case "string":
            case "integer":
            case "number":
            case "boolean":
            case "array":
                const name = path.join(".");
                columns.push({name: name, path: path, width: App.defaultColumnWidth });
                break;
            case "object":
                const addProps = type.jsonType.additionalProperties;
                //    isAny == true   <=>   additionalProperties == {}
                const isAny =   addProps !== null && typeof addProps == "object" && Object.keys(addProps).length == 0;
                if (isAny) {
                    const name = path.join(".");
                    columns.push({name: name, path: path, width: App.defaultColumnWidth });
                    break;
                }
                const properties = type.jsonType.properties;
                for (const name in properties) {
                    const property  = properties[name];
                    const fieldPath = [...path, name];
                    this.getColumnNames(columns, fieldPath, property);
                }
                break;
        }
    }

    createExplorerHead (entityType: JsonType, entityFields: { [key: string] : Column }) : HTMLTableRowElement {
        const keyName   = App.getEntityKeyName(entityType);
        if (entityType) {
            const properties    =  entityType.properties;
            for (const fieldName in properties) {
                const fieldType = properties[fieldName];
                const columns   = [] as Column[];
                App.getColumnNames(columns, [fieldName], fieldType);
                for (const column of columns) {
                    entityFields[column.name] = column;
                }
            }
        } else {
            entityFields[keyName] = { name: keyName, path: [keyName], width: App.defaultColumnWidth };
        }
        const   head            = createEl('tr');

        // cell: checkbox
        const   thCheckbox      = createEl('th');
        thCheckbox.style.width  = "16px";
        const   thCheckboxDiv   = createEl('div');        
        thCheckbox.append(thCheckboxDiv);
        head.append(thCheckbox);

        // cell: fields (id, ...)
        for (const fieldName in entityFields) {
            const column        = entityFields[fieldName];
            const th            = createEl('th');
            th.style.width      = `${App.defaultColumnWidth}px`;
            const thIdDiv       = createEl('div');
            const path          = column.path;
            thIdDiv.innerText   = path.length == 1 ? path[0] : `.${path[path.length-1]}`;
            thIdDiv.title       = fieldName;
            th.append(thIdDiv);
            const grip          = createEl('div');
            grip.classList.add("thGrip");
            grip.style.cursor   = "ew-resize";
            // grip.style.background   = 'red';
            // grip.style.userSelect = "none"; // disable text selection while dragging */
            grip.addEventListener('mousedown', (e) => this.thStartDrag(e, th) );
            th.appendChild(grip);
            head.append(th);
            column.th = th;
        }

        // cell: last
        const   thLast          = createEl('th');
        thLast.style.width      = "100%";
        head.append(thLast);
        return head;
    }

    static defaultColumnWidth   = 50;
    static maxColumnWidth       = 200;

    static calcWidth(text: string) : number {
        if (text === undefined)
            return 0;
        if (text.length > 40) {
            // avoid measuring long texts
            // 30 characters => 234px. Sample: "012345678901234567890123456789"
            return App.maxColumnWidth;
        }
        measureTextWidth.innerHTML = text;
        return Math.ceil(measureTextWidth.clientWidth);                
    }

    setColumnWidths() {
        for (const fieldName in this.entityFields) {
            const column = this.entityFields[fieldName];
            column.th.style.width = `${column.width + 10}px`;
        }
    }

    thDrag          : HTMLElement;
    thDragOffset    : number;    

    thStartDrag(event: MouseEvent, th: HTMLElement) {
        this.thDragOffset           = event.offsetX - (event.target as HTMLElement).clientWidth;
        this.thDrag                 = th;
        document.body.style.cursor  = "ew-resize";
        document.body.onmousemove   = (event)  => app.thOnDrag(event);
        document.body.onmouseup     = ()       => app.thEndDrag();
        event.preventDefault();
    }

    thOnDrag(event: MouseEvent) {
        const parent            = (this.thDrag.parentNode.parentNode.parentNode.parentNode as HTMLElement);
        const scrollOffset      = parent.scrollLeft;
        let width               = scrollOffset + event.clientX - this.thDragOffset - this.thDrag.offsetLeft;
        if (width < 25) width   = 25;
        this.thDrag.style.width = `${width}px`;
        event.preventDefault();
    }

    thEndDrag() {
        document.body.onmousemove   = undefined;
        document.body.onmouseup     = undefined;
        document.body.style.cursor  = "auto";
    }


    updateExplorerEntities(entities: Entity[], entityType: JsonType) {
        const table         = this.explorerTable;
        let entityCount     = 0;
        const keyName       = App.getEntityKeyName(entityType);
        const entityFields  = this.entityFields;
        const tds           = [] as HTMLTableCellElement[];
        // console.log("entities", entities);
        for (const entity of entities) {
            tds.length  = 0;
            const id    = entity[keyName];
            let   row   = this.explorerEntities[id];
            if (!row) {
                row = createEl('tr');
                this.explorerEntities[id] = row;

                // cell: add checkbox
                const tdCheckbox    = createEl('td');
                const checked       = createEl('input');
                checked.type        = "checkbox";
                checked.tabIndex    = -1;
                checked.checked     = true;
                tdCheckbox.append(checked);
                row.append(tdCheckbox);
                tds.push(tdCheckbox);

                // cell: add fields
                for (const _ in entityFields) {
                    const tdField = createEl('td');
                    row.append(tdField);
                    tds.push(tdField);
                }
                table.append(row);
            } else {
                for (const td of row.childNodes) {
                    tds.push(td as HTMLTableCellElement);
                }            
            }
            // cell: set fields
            const calcWidth = entityCount < 20;
            App.assignRowCells(tds, entity, entityFields, calcWidth);
            entityCount++;
        }
    }

    static assignRowCells (tds: HTMLTableCellElement[], entity: Entity, entityFields: { [key: string] : Column }, calcWidth: boolean) {
        let tdIndex = 1;
        for (const fieldName in entityFields) {
            // if (fieldName == "derivedClassNull.derivedVal") debugger;
            const column    = entityFields[fieldName];
            const path      = column.path;
            let   value     = entity;
            const pathLen   = path.length;
            let   i         = 0
            for (; i < pathLen; i++) {
                value = value[path[i]];
                if (value === null || value === undefined || typeof value != "object")
                    break;
            }
            if (i < pathLen - 1)
                value = undefined;
            const td        = tds[tdIndex++];
            // clear all children added previously
            while (td.firstChild) {
                td.removeChild(td.lastChild);
            }
            const content   = App.getCellContent(value);
            const count     = content.count;
            if (count === undefined) {
                td.textContent   = `${content.value} `
            } else {
                const isObjectArray     = content.isObjectArray;
                const countStr          = count == 0 ? '0' : `${count}: `
                const spanCount         = createEl("span");
                spanCount.textContent   = isObjectArray ? `${countStr} ${fieldName}` : countStr;
                spanCount.classList.add("cellCount");
                td.append(spanCount);
                if (!isObjectArray) {
                    const spanValue         = createEl("span");
                    spanValue.textContent   = content.value;
                    td.append(spanValue);
                } 
            }
            // measure text width is expensive => measure only the first 20 rows
            if (calcWidth) {
                let width           = App.calcWidth(content.value);
                if (count) width   += App.calcWidth(String(count));                
                if (column.width < width) {
                    column.width = width
                }
            }
        }
    }

    static getCellContent(value: any) : CellData {
        if (value === undefined)
            return { value: "" };                                       // 
        const type = typeof value;
        if (type != "object")
            return { value: value }                                     // abc
        if (Array.isArray(value)) {
            if (value.length > 0) {
                for (const item of value) {
                    if (typeof item == "object") {                      // 3: objects
                        return { count: value.length, isObjectArray: true};
                    }
                }
                const items = value.map(i => i);
                return { value: items.join(", "), count: value.length}; // 2: abc,xyz
            }
            return { value: "", count: 0 }                              // 0;
        }
        return { value: JSON.stringify(value) };                        // {"foo": "bar", ... }
    }

    removeExplorerIds(ids: string[]) {
        const selected = this.findContainerEntities(ids);
        for (const id in selected)
            selected[id].remove();
        for (const id of ids) {
            delete this.explorerEntities[id];
            delete this.selectedEntities[id];
        }
    }

    findContainerEntities (ids: string[]) : {[key: string] : HTMLTableRowElement} {
        const result : {[key: string] : HTMLTableRowElement} = {}
        for(const id of ids){
            const li = this.explorerEntities[id];
            if (!li)
                continue;
            result[id] = li;
        }
        return result;
    }

    entityIdentity = { } as {
        database:   string,
        container:  string,
        entityIds:  string[],
        command?:   string
    }

    entityHistoryPos    = -1;
    entityHistory: {
        selection?: monaco.Selection,
        route:      Resource
    }[] = [];

    storeCursor() {
        if (this.entityHistoryPos < 0)
            return;
        this.entityHistory[this.entityHistoryPos].selection    = this.entityEditor.getSelection();
    }

    navigateEntity(pos: number) {
        if (pos < 0 || pos >= this.entityHistory.length)
            return;
        this.storeCursor();
        this.entityHistoryPos = pos;
        const entry = this.entityHistory[pos]
        this.loadEntities(entry.route, true, entry.selection);
    }

    async loadEntities (p: Resource, preserveHistory: boolean, selection: monaco.IRange) : Promise<string> {
        this.setExplorerEditor("entity");
        this.setEditorHeader("entity");
        entityType.innerHTML    = this.getEntityType  (p.database, p.container);
        writeResult.innerHTML   = "";
        this.setEntitiesIds(p.database, p.container, p.ids);
        if (p.ids.length == 0) {
            this.setEntityValue(p.database, p.container, "");
            return null;
        }
        // entityIdsEl.innerHTML   = `${entityLink}<span class="spinner"></span>`;

        if (!preserveHistory) {
            this.storeCursor();
            this.entityHistory[++this.entityHistoryPos] = { route: {...p} };
            this.entityHistory.length = this.entityHistoryPos + 1;
        }
        this.entityIdentity = {
            database:   p.database,
            container:  p.container,
            entityIds:  [...p.ids]
        };
        // execute GET request
        const requestIds    = p.ids.length == 1 ? p.ids[0] : p.ids; // load as object if exact one id
        const response      = await App.restRequest("GET", null, p.database, p.container, requestIds, null);        
        let content         = await response.text();

        content             = this.formatJson(this.config.formatEntities, content);
        this.setEntitiesIds(p.database, p.container, p.ids);
        if (!response.ok) {
            this.setEntityValue(p.database, p.container, content);
            return null;
        }
        // console.log(entityJson);
        this.setEntityValue(p.database, p.container, content);
        if (selection)  this.entityEditor.setSelection(selection);        
        // this.entityEditor.focus(); // not useful - annoying: open soft keyboard on phone
        return content;
    }

    updateGetEntitiesAnchor(database: string, container: string) {
        // console.log("updateGetEntitiesAnchor");
        const idsStr = entityIdsInput.value;
        const ids    = idsStr.split(",");
        let   len    = ids.length;
        if (len == 1 && ids[0] == "") len = 0;
        entityIdsContainer.onclick      = _ => this.loadContainer({ database: database, container: container, ids: null }, null);
        entityIdsContainer.innerText    = `« ${container}`;
        entityIdsCount.innerText        = len > 0 ? `(${len})` : "";

        let getUrl: string;        
        if (len == 1) {
            getUrl = `./rest/${database}/${container}/${ids[0]}`;
        } else {
            getUrl = `./rest/${database}/${container}?ids=${idsStr}`;
        }
        entityIdsGET.href       = getUrl;
    }

    setEntitiesIds (database: string, container: string, ids: string[]) {
        entityIdsReload.onclick     = _ => this.loadInputEntityIds      (database, container);
        entityIdsInput.onchange     = _ => this.updateGetEntitiesAnchor (database, container);
        entityIdsInput.onkeydown    = e => this.onEntityIdsKeyDown   (e, database, container);
        entityIdsInput.value        = ids.join (",");
        this.updateGetEntitiesAnchor(database, container);
    }

    formatResult (action: string, statusCode: number, status: string, message: string) {
        const color = 200 <= statusCode && statusCode < 300 ? "green" : "red";
        return `<span>
            <span style="opacity:0.7">${action} status:</span>
            <span style="color: ${color};">${statusCode} ${status}</span>
            <span>${message}</span>
        </span>`;
    }

    async loadInputEntityIds (database: string, container: string) {
        const ids               = entityIdsInput.value == "" ? [] : entityIdsInput.value.split(",")
        const unchangedSelection= App.arraysEquals(this.entityIdentity.entityIds, ids)
        const p: Resource       = { database, container, ids };
        const response          = await this.loadEntities(p, true, null);
        if (unchangedSelection)
            return;
        let   json              = JSON.parse(response) as Entity[];
        if (json == null) {
            json = [];
        } else {
            if (!Array.isArray(json))
                json = [json];
        }
        const type          = this.getContainerSchema(database, container);
        this.updateExplorerEntities(json, type);
        this.selectEntities(database, container, ids);
    }

    onEntityIdsKeyDown(event: KeyboardEvent, database: string, container: string) {
        if (event.code != 'Enter')
            return;
        this.loadInputEntityIds(database, container);
    }

    clearEntity (database: string, container: string) {
        this.setExplorerEditor("entity");
        this.setEditorHeader("entity");

        this.entityIdentity = {
            database:   database,
            container:  container,
            entityIds:  []
        };
        entityType.innerHTML    = this.getEntityType (database, container);
        writeResult.innerHTML   = "";
        this.setEntitiesIds(database, container, []);
        this.setEntityValue(database, container, "");
    }

    getContainerSchema (database: string, container: string) : JsonType | null{
        const schema = this.databaseSchemas[database];
        if (schema) {
            return schema._containerSchemas[container];
        }
        return null;
    }

    static getEntityKeyName (entityType: JsonType) {
        if (entityType?.key)
            return entityType.key;
        return "id";
    }

    async saveEntitiesAction () {
        const ei        = this.entityIdentity;
        const jsonValue = this.entityModel.getValue();
        await this.saveEntities(ei.database, ei.container, jsonValue);
    }

    async saveEntities (database: string, container: string, jsonValue: string)
    {
        let value:      Entity | Entity[];
        try {
            value = JSON.parse(jsonValue) as Entity | Entity[];
        } catch (error) {
            writeResult.innerHTML = `<span style="color:red">Save failed: ${error}</code>`;
            return;
        }
        const entities          = Array.isArray(value) ? value : [value];
        const type              = this.getContainerSchema(database, container);
        const keyName           = App.getEntityKeyName(type);
        const ids               = entities.map(entity => entity[keyName]) as string[];
        writeResult.innerHTML   = 'save <span class="spinner"></span>';
        const requestIds        = Array.isArray(value) ? ids : ids[0];
        const response          = await App.restRequest("PUT", jsonValue, database, container, requestIds, null);
        if (!response.ok) {
            const error = await response.text();
            writeResult.innerHTML = this.formatResult("Save", response.status, response.statusText, error);
            return;
        }
        writeResult.innerHTML = this.formatResult("Save", response.status, response.statusText, "");
        // add or update explorer entities
        this.updateExplorerEntities(entities, type);
        if (App.arraysEquals(this.entityIdentity.entityIds, ids))
            return;
        this.selectEntities(database, container, ids);        
    }

    selectEntities(database: string, container: string, ids: string[]) {
        this.entityIdentity.entityIds = ids;
        this.setEntitiesIds(database, container, ids);
        let liIds = this.findContainerEntities(ids);

        this.setSelectedEntities(ids);
        const firstRow = liIds[ids[0]];
        if (firstRow) {
            const columnIndex = this.explorer.focusedCell?.cellIndex ?? 1;
            this.setFocusCell(firstRow.rowIndex, columnIndex, "smooth");
        }
        this.entityHistory[++this.entityHistoryPos] = { route: { database: database, container: container, ids:ids }};
        this.entityHistory.length = this.entityHistoryPos + 1;        
    }

    static arraysEquals(left: string[], right: string []) : boolean {
        if (left.length != right.length)
            return false;
        for (let i = 0; i < left.length; i++) {
            if (left[i] != right[i])
                return false;
        }
        return true;
    }

    async deleteEntitiesAction () {
        const ei = this.entityIdentity;
        await this.deleteEntities (ei.database, ei.container, ei.entityIds);
    }

    async deleteEntities (database: string, container: string, ids: string[]) {
        writeResult.innerHTML = 'delete <span class="spinner"></span>';
        const response = await App.restRequest("DELETE", null, database, container, ids, null);        
        if (!response.ok) {
            const error = await response.text();
            writeResult.innerHTML = this.formatResult("Delete", response.status, response.statusText, error);
        } else {
            this.entityIdentity.entityIds = [];
            writeResult.innerHTML = this.formatResult("Delete", response.status, response.statusText, "");
            this.setEntityValue(database, container, "");
            this.removeExplorerIds(ids);
        }
    }

    entityModel:    monaco.editor.ITextModel;
    entityModels:   {[key: string]: monaco.editor.ITextModel} = { };

    getModel (url: string) : monaco.editor.ITextModel {
        this.entityModel = this.entityModels[url];
        if (!this.entityModel) {
            const entityUri         = monaco.Uri.parse(url);
            this.entityModel        = monaco.editor.createModel(null, "json", entityUri);
            this.entityModels[url]  = this.entityModel;
        }
        return this.entityModel;
    }

    setEntityValue (database: string, container: string, value: string) : jsonToAst.ValueNode {
        const url   = `entity://${database}.${container}.json`;
        const model = this.getModel(url);
        model.setValue(value);
        this.entityEditor.setModel (model);
        if (value == "")
            return null;
        const ast = App.parseAst(value);
        if (!ast)
            return null;
        const containerSchema = this.getContainerSchema(database, container);
        if (containerSchema) {
            try {
                this.decorateJson(this.entityEditor, ast, containerSchema, database);
            } catch (error) {
                console.error("decorateJson", error);
            }
        }
        return ast;
    }

    static parseAst(value: string) : jsonToAst.ValueNode {
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
    }

    decorateJson(editor: monaco.editor.IStandaloneCodeEditor, ast: jsonToAst.ValueNode, containerSchema: JsonType, database: string) {        
        // --- deltaDecorations() -> [ITextModel | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.ITextModel.html
        const newDecorations: monaco.editor.IModelDeltaDecoration[] = [
            // { range: new monaco.Range(7, 13, 7, 22), options: { inlineClassName: 'refLinkDecoration' } }
        ];
        App.addRelationsFromAst(ast, containerSchema, (value, container) => {
            const range         = App.RangeFromNode(value);
            const markdownText  = `${database}/${container}  \nFollow: (ctrl + click)`;
            const hoverMessage  = [ { value: markdownText } ];
            newDecorations.push({ range: range, options: { inlineClassName: 'refLinkDecoration', hoverMessage: hoverMessage }});
        });
        editor.deltaDecorations([], newDecorations);
    }

    static addRelationsFromAst(ast: jsonToAst.ValueNode, schema: JsonType, addRelation: AddRelation) {
        if (ast.type == "Literal")
            return;
        for (const child of ast.children) {
            switch (child.type) {
            case "Object":
                App.addRelationsFromAst(child, schema, addRelation);
                break;
            case "Array":
                break;
            case "Property":
                // if (child.key.value == "employees") debugger;
                const property = schema.properties[child.key.value];
                if (!property)
                    continue;
                const value = child.value;

                switch (value.type) {
                case "Literal":
                    const relation = property.relation;
                    if (relation && value.value !== null) {
                        addRelation (value, relation);
                    }
                    break;
                case "Object":
                    const resolvedDef = property._resolvedDef;
                    if (resolvedDef) {
                        App.addRelationsFromAst(value, resolvedDef, addRelation);
                    }
                    break;
                case "Array":
                    const resolvedDef2 = property.items?._resolvedDef;
                    if (resolvedDef2) {
                        App.addRelationsFromAst(value, resolvedDef2, addRelation);
                    }
                    const relation2 = property.relation;
                    if (relation2) {
                        for (const item of value.children) {
                            if (item.type == "Literal") {
                                addRelation(item, relation2);
                            }
                        }
                    }
                    break;
                }
                break;
            }
        }
    }

    static hasProperty(objectNode: jsonToAst.ObjectNode, keyName: string, id: string) : boolean {
        for (const property of objectNode.children) {
            if (property.key.value != keyName)
                continue;
            const value = property.value;
            if (value.type == "Literal" && value.value == id)
                return true;
        }
        return false;
    }

    static findArrayItem(arrayNode: jsonToAst.ArrayNode, keyName: string, id: string) : jsonToAst.ObjectNode | null {
        for(const item of arrayNode.children) {
            if (item.type != "Object")
                continue;
            if (App.hasProperty(item, keyName, id))
                return item;
        }
        return null;
    }

    static findPathRange(ast: jsonToAst.ValueNode, pathString: string, keyName: string, id: string) : FindRange {
        const astRange  = App.RangeFromNode(ast);
        const path      = pathString.split('.');
        let   node      = ast;
        switch (ast.type) {
            case "Array":
                node = App.findArrayItem(ast, keyName, id);
                if (!node)
                    return { entity: null, value: null };
                break;
            case "Object":
                if (!App.hasProperty(ast, keyName, id))
                    return { entity: null, value: null };
                break;
            default:
                return { entity: null, value: null };
        }
        const entityRange = App.RangeFromNode(node);
        for (let i = 0; i < path.length; i++) {
            const name = path[i];
            if (node.type != "Object")
                return { entity: astRange, value: null };
            let foundChild: jsonToAst.PropertyNode = null;
            for (const child of node.children) {
                if (child.key.value == name) {
                    foundChild = child;
                    break;
                }
            }
            if (!foundChild)
                return { entity: entityRange, value: null };
            const valueRange = App.RangeFromNode(foundChild.value);
            return { entity: entityRange, value: valueRange };
        }
        return { entity: entityRange, value: null };
    }

    static RangeFromNode(node: jsonToAst.ValueNode) : monaco.Range{
        const trim  = node.type == "Literal" && typeof node.value == "string" ? 1 : 0;
        const start = node.loc.start;
        const end   = node.loc.end;
        return new monaco.Range(start.line, start.column + trim, end.line, end.column - trim);
    }

    setCommandParam (database: string, command: string, value: string) {
        const url           = `command-param://${database}.${command}.json`;
        const isNewModel    = this.entityModels[url] == undefined;
        const model         = this.getModel(url)
        if (isNewModel) {
            model.setValue(value);
        }
        this.commandValueEditor.setModel (model);
    }

    setCommandResult (database: string, command: string) {
        const url   = `command-result://${database}.${command}.json`;
        const model = this.getModel(url)
        this.entityEditor.setModel (model);
    }

    commandEditWidth = "60px";
    activeExplorerEditor: ExplorerEditor = undefined;

    setExplorerEditor(edit : ExplorerEditor) {
        this.activeExplorerEditor   = edit;
        // console.log("editor:", edit);
        const commandActive         = edit == "command";
        commandValueContainer.style.display = commandActive ? "" : "none";
        commandParamBar.style.display       = commandActive ? "" : "none";
        el("explorerEdit").style.gridTemplateRows = commandActive ? `${this.commandEditWidth} var(--vbar-width) 1fr` : "0 0 1fr";

        const editorActive              = edit == "command" || edit == "entity";
        entityContainer.style.display   = editorActive      ? "" : "none";
        el("dbInfo").style.display      = edit == "dbInfo"  ? "" : "none";
        //
        this.layoutEditors();
    }

    showCommand(database: string, commandName: string) {
        this.setExplorerEditor("command");

        const schema        = this.databaseSchemas[database]._rootSchema;
        const signature     = schema ? schema.commands[commandName] : null
        const def           = signature ? Object.keys(signature.param).length  == 0 ? "null" : "{}" : "null";
        const tags          = this.getCommandTags(database, commandName, signature);
        commandSignature.innerHTML      = tags.label;
        commandLink.innerHTML           = tags.link;

        this.entityIdentity.command     = commandName;
        this.entityIdentity.database    = database;
        this.setCommandParam (database, commandName, def);
        this.setCommandResult(database, commandName);
    }

    // --------------------------------------- monaco editor ---------------------------------------
    // [Monaco Editor Playground] https://microsoft.github.io/monaco-editor/playground.html#extending-language-services-configure-json-defaults

    async createProtocolSchemas () {

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
        const schemas: MonacoSchema[] = [];
        try {
            const jsonSchemaResponse  = await fetch("schema/protocol/json-schema.json");
            const jsonSchema          = await jsonSchemaResponse.json();

            for (const schemaName in jsonSchema) {
                const schema          = jsonSchema[schemaName];
                const url             = "protocol/json-schema/" + schemaName;
                const schemaEntry: MonacoSchema = {
                    uri:    "http://" + url,
                    schema: schema            
                }
                schemas.push(schemaEntry);
            }
        } catch (e) {
            console.error ("load json-schema.json failed");
        }
        return schemas;
    }

    requestModel:       monaco.editor.ITextModel;
    responseModel:      monaco.editor.ITextModel;

    requestEditor:      monaco.editor.IStandaloneCodeEditor;
    responseEditor:     monaco.editor.IStandaloneCodeEditor;
    entityEditor:       monaco.editor.IStandaloneCodeEditor;
    commandValueEditor: monaco.editor.IStandaloneCodeEditor;

    allMonacoSchemas: MonacoSchema[] = [];

    addSchemas (monacoSchemas: MonacoSchema[]) {
        this.allMonacoSchemas.push(...monacoSchemas);
        // [LanguageServiceDefaults | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.languages.json.LanguageServiceDefaults.html
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
            validate: true,
            schemas: this.allMonacoSchemas
        });
    }

    async setupEditors ()
    {
        // this.setExplorerEditor("none");
        
        // --- setup JSON Schema for monaco
        const requestUri      = monaco.Uri.parse("request://jsonRequest.json");   // a made up unique URI for our model
        const responseUri     = monaco.Uri.parse("request://jsonResponse.json");  // a made up unique URI for our model
        const monacoSchemas   = await this.createProtocolSchemas();

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
            this.requestEditor  = monaco.editor.create(requestContainer, { /* model: model */ });
            this.requestModel   = monaco.editor.createModel(null, "json", requestUri);
            this.requestEditor.setModel (this.requestModel);

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
            this.responseEditor = monaco.editor.create(responseContainer, { /* model: model */ });
            this.responseModel  = monaco.editor.createModel(null, "json", responseUri);
            this.responseEditor.setModel (this.responseModel);
        }

        // --- create entity editor
        {
            this.entityEditor   = monaco.editor.create(entityContainer, { });
            this.entityEditor.onMouseDown((e) => {
                if (!e.event.ctrlKey)
                    return;
                if (this.activeExplorerEditor != "entity")
                    return;
                // console.log('mousedown - ', e);
                const value     = this.entityEditor.getValue();
                const column    = e.target.position.column;
                const line      = e.target.position.lineNumber;
                window.setTimeout(() => { this.tryFollowLink(value, column, line) }, 1);
            });
        }
        // --- create command value editor
        {
            this.commandValueEditor     = monaco.editor.create(commandValue, { });
            // this.commandValueModel   = monaco.editor.createModel(null, "json");
            // this.commandValueEditor.setModel(this.commandValueModel);
            //this.commandValueEditor.setValue("{}");
        }
        // this.commandResponseModel = monaco.editor.createModel(null, "json");
        this.setEditorOptions();
        window.onresize = () => {
            this.layoutEditors();        
        };
    }

    setEditorOptions() {
        const editorSettings: monaco.editor.IEditorOptions & monaco.editor.IGlobalEditorOptions= {
            lineNumbers:    this.config.showLineNumbers ? "on" : "off",
            minimap:        { enabled: this.config.showMinimap ? true : false },
            theme:          window.appConfig.monacoTheme,
            mouseWheelZoom: true
        }
        this.requestEditor.     updateOptions ({ ...editorSettings });
        this.responseEditor.    updateOptions ({ ...editorSettings });
        this.entityEditor.      updateOptions ({ ...editorSettings });
        this.commandValueEditor.updateOptions ({ ...editorSettings });
    }

    tryFollowLink(value: string, column: number, line: number) {
        try {
            JSON.parse(value);  // early out invalid JSON
            const ast               = parse(value, { loc: true });
            const database          = this.entityIdentity.database;
            const containerSchema   = this.getContainerSchema(database, this.entityIdentity.container);

            let entity: Resource;
            App.addRelationsFromAst(ast, containerSchema, (value, container) => {
                if (entity || value.type != "Literal")
                    return;
                const start = value.loc.start;
                const end   = value.loc.end;
                if (start.line <= line && start.column <= column && line <= end.line && column <= end.column) {
                    // console.log(`${resolvedDef.databaseName}/${resolvedDef.containerName}/${value.value}`);
                    const literalValue = value.value as string;
                    entity = { database: database, container: container, ids: [literalValue] };
                }
            });
            if (entity) {
                this.loadEntities(entity, false, null);
            }
        } catch (error) {
            writeResult.innerHTML = `<span style="color:#FF8C00">Follow link failed: ${error}</code>`;
        }
    }

    setConfig<K extends ConfigKey>(key: K, value: Config[K]) {
        this.config[key]    = value;
        const elem          = el(key);
        if (elem instanceof HTMLInputElement) {
            elem.value   = value as string;
            elem.checked = value as boolean;
        }
        const valueStr = JSON.stringify(value, null, 2);
        window.localStorage.setItem(key, valueStr);
    }

    getConfig(key: keyof Config) {
        const valueStr = window.localStorage.getItem(key);
        try {
            return JSON.parse(valueStr);
        } catch(e) { }
        return undefined;
    }

    initConfigValue(key: ConfigKey) {
        const value = this.getConfig(key);
        if (value == undefined) {
            this.setConfig(key, this.config[key]);
            return;
        }
        this.setConfig(key, value);
    }

    config = defaultConfig;

    loadConfig() {
        this.initConfigValue("showLineNumbers");
        this.initConfigValue("showMinimap");
        this.initConfigValue("formatEntities");
        this.initConfigValue("formatResponses");
        this.initConfigValue("activeTab");
        this.initConfigValue("showDescription");
        this.initConfigValue("filters");
    }

    changeConfig (key: ConfigKey, value: boolean) {
        this.setConfig(key, value);
        switch (key) {
            case "showLineNumbers":
            case "showMinimap":
                this.setEditorOptions();
                break;
        }
    }

    formatJson(format: boolean, text: string) : string {
        if (format) {
            try {
                // const action = editor.getAction("editor.action.formatDocument");
                // action.run();
                const obj = JSON.parse(text);
                return JSON.stringify(obj, null, 4);
            }
            catch (error) {}            
        }
        return text;
    }

    layoutEditors () {
        // console.log("layoutEditors - activeTab: " + activeTab)
        switch (this.config.activeTab) {
        case "playground":
            const editors = [
                { editor: this.responseEditor,  elem: responseContainer },               
                { editor: this.requestEditor,   elem: requestContainer },
            ]
            this.layoutMonacoEditors(editors);
            break;
        case "explorer":
            // layout from right to left. Otherwise commandValueEditor.clientWidth is 0px;
            const editors2 = [
                { editor: this.entityEditor,        elem: entityContainer },               
                { editor: this.commandValueEditor,  elem: commandValue },
            ]
            this.layoutMonacoEditors(editors2);
            break;
        }
    }

    layoutMonacoEditors(pairs: { editor: monaco.editor.IStandaloneCodeEditor, elem: HTMLElement }[]) {
        for (let n = pairs.length - 1; n >= 0; n--) {
            const pair = pairs[n];
            if (!pair.editor || !pair.elem.children[0]) {
                pairs.splice(n, 1);
            }
        }
        for (const pair of pairs) {
            const child         = pair.elem.children[0] as HTMLElement;
            child.style.width   = "0px";  // required to shrink width.  Found no alternative solution right now.
            child.style.height  = "0px";  // required to shrink height. Found no alternative solution right now.
        }
        for (const pair of pairs) {
            pair.editor.layout();
        }
        // set editor width/height to their container width/height
        for (const pair of pairs) {
            const child         = pair.elem.children[0] as HTMLElement;
            child.style.width   = pair.elem.clientWidth  + "px";
            child.style.height  = pair.elem.clientHeight + "px";
        }
    }

    dragTemplate :  HTMLElement;
    dragBar:        HTMLElement;
    dragOffset:     number;
    dragHorizontal: boolean;

    startDrag(event: MouseEvent, template: string, bar: string, horizontal: boolean) {
        // console.log(`drag start: ${event.offsetX}, ${template}, ${bar}`)
        this.dragHorizontal = horizontal;
        this.dragOffset     = horizontal ? event.offsetX : event.offsetY
        this.dragTemplate   = el(template);
        this.dragBar        = el(bar);
        document.body.style.cursor = "ew-resize";
        document.body.onmousemove = (event)  => app.onDrag(event);
        document.body.onmouseup   = ()       => app.endDrag();
        event.preventDefault();
    }

    getGridColumns(xy: number) {
        const prev = this.dragBar.previousElementSibling as HTMLElement;
        xy = xy - (this.dragHorizontal ? prev.offsetLeft : prev.offsetTop);
        if (xy < 20) xy = 20;
        // console.log (`drag x: ${x}`);
        switch (this.dragTemplate.id) {
            case "playground":          return [xy + "px", "var(--bar-width)", "1fr"];
            case "explorer":
                const cols = this.dragTemplate.style.gridTemplateColumns.split(" ");
                switch (this.dragBar.id) { //  [150px var(--bar-width) 200px var(--bar-width) 1fr];
                    case "exBar1":      return [xy + "px", cols[1], cols[2], cols[3]];
                    case "exBar2":      return [cols[0], cols[1], xy + "px", cols[3]];
                }
                break;
            case "explorerEdit":
                this.commandEditWidth = xy + "px";
                return [this.commandEditWidth, "var(--vbar-width)", "1fr"];
        }
        throw `unhandled condition in getGridColumns() id: ${this.dragTemplate?.id}`
    }

    onDrag(event: MouseEvent) {
        if (!this.dragTemplate)
            return;
        // console.log(`  drag: ${event.clientX}`);
        const clientXY  = this.dragHorizontal ? event.clientX : event.clientY;
        const xy        = clientXY - this.dragOffset;
        const cols      = this.getGridColumns(xy);
        if (this.dragHorizontal) {
            this.dragTemplate.style.gridTemplateColumns = cols.join(" ");
        } else {
            this.dragTemplate.style.gridTemplateRows    = cols.join(" ");
        }
        this.layoutEditors();
        event.preventDefault();
    }

    endDrag() {
        if (!this.dragTemplate)
            return;
        document.body.onmousemove   = undefined;
        document.body.onmouseup     = undefined;
        this.dragTemplate           = undefined;
        document.body.style.cursor  = "auto";
    }

    toggleTheme() {
        let mode = document.documentElement.getAttribute('data-theme');
        mode = mode == 'dark' ? 'light' : 'dark'
        window.setTheme(mode)
        this.setEditorOptions();
    }

    initApp() {
        // --- methods without network requests
        this.loadConfig();
        this.initUserToken();
        this.openTab(app.getConfig("activeTab"));

        // --- methods performing network requests - note: methods are not awaited
        this.loadExampleRequestList();
        this.loadCluster();
    }
}

export const app = new App();
window.addEventListener("keydown", event => app.onKeyDown(event), true);
window.addEventListener("keyup",   event => app.onKeyUp(event), true);
