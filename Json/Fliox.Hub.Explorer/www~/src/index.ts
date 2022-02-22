/// <reference types="../../../../node_modules/monaco-editor/monaco" />

import { el, createEl, Resource, Method, ConfigKey, Config, defaultConfig, getColorBasedOnBackground } from "./types.js";
import { Schema, MonacoSchema }                                 from "./schema.js";
import { Explorer }                                             from "./explorer.js";
import { EntityEditor }                                         from "./entity-editor.js";
import { Playground }                                           from "./playground.js";

import { FieldType, JsonType }                                  from "../../../../Json.Tests/assets~/Schema/Typescript/JsonSchema/Friflo.Json.Fliox.Schema.JSON";
import { DbSchema, DbContainers, DbCommands, HostDetails }          from "../../../../Json.Tests/assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster";
import { SyncRequest, SyncResponse, ProtocolResponse_Union }    from "../../../../Json.Tests/assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol";
import { SyncRequestTask_Union, SendCommandResult }             from "../../../../Json.Tests/assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol.Tasks";

declare global {
    interface Window {
        appConfig: { monacoTheme: string };
        setTheme(mode: string) : void;
        app: App;
    }
}

const projectName           = el("projectName");
const projectUrl            = el("projectUrl")      as HTMLAnchorElement;
const envEl                 = el("envEl");
const defaultUser           = el("user")            as HTMLInputElement;
const defaultToken          = el("token")           as HTMLInputElement;
const catalogExplorer       = el("catalogExplorer");
const entityExplorer        = el("entityExplorer");

const entityFilter          = el("entityFilter")    as HTMLInputElement;

// request response editor
const requestContainer      = el("requestContainer");
const responseContainer     = el("responseContainer");

// entity/command editor
const commandValue          = el("commandValue");
const entityContainer       = el("entityContainer");

/* if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register("./sw.js").then(registration => {
        console.log("SW registered");
    }).catch(error => {
        console.error(`SW failed: ${error}`);
    });
} */


export class App {
    readonly explorer:      Explorer;
    readonly editor:        EntityEditor;
    readonly playground:    Playground;

    constructor() {
        this.explorer   = new Explorer(this.config);
        this.editor     = new EntityEditor();
        this.playground = new Playground();

        window.addEventListener("keydown", event => this.onKeyDown(event), true);
        window.addEventListener("keyup",   event => this.onKeyUp(event),   true);
    }

    private getCookie  (name: string) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2)
            return parts.pop().split(';').shift();
        return null;
    }

    private initUserToken  () {
        const user    = this.getCookie("fliox-user")   ?? "admin";
        const token   = this.getCookie("fliox-token")  ?? "admin";
        this.setUser(user);
        this.setToken(token);
    }

    public setUser (user: string) : void {
        defaultUser.value   = user;
        document.cookie = `fliox-user=${user};`;
    }

    public setToken  (token: string) : void {
        defaultToken.value  = token;
        document.cookie = `fliox-token=${token};`;
    }

    public selectUser (element: HTMLElement) : void {
        const value = element.innerText;
        this.setUser(value);
        this.setToken(value);
    }


    private lastCtrlKey:        boolean;
    public  refLinkDecoration:  CSSStyleRule;

    private applyCtrlKey(event: KeyboardEvent) {
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

    public onKeyUp (event: KeyboardEvent) : void {
        if (event.code == "ControlLeft")
            this.applyCtrlKey(event);
    }

    public onKeyDown (event: KeyboardEvent) : void {
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
                        this.execute(event, () => editor.sendCommand());
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

    private switchTab () {
        if (document.activeElement == entityExplorer)
            this.entityEditor.focus();
        else
            entityExplorer.focus();
    }

    private execute(event: KeyboardEvent, lambda: () => void) {
        lambda();
        event.preventDefault();
    }


    // --------------------------------------- Fliox HTTP --------------------------------------- 
    public static async postRequest (request: string, tag: string) : Promise<{ text: string; json: any; }> {
        const init = {        
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    request
        };
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

    private static async postRequestTasks (database: string, tasks: SyncRequestTask_Union[], tag: string) {
        const db = database == "main_db" ? undefined : database;
        const sync: SyncRequest = {
            "msg":      "sync",
            "database": db,
            "tasks":    tasks,
            "user":     defaultUser.value,
            "token":    defaultToken.value
        };
        const request = JSON.stringify(sync);
        tag = tag ? tag : "";
        return await App.postRequest(request, `${database}/${tag}`);
    }

    public static getRestPath(database: string, container: string, ids: string | string[], query: string) : string {
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

    static async restRequest (method: Method, body: string, database: string, container: string, ids: string | string[], query: string) : Promise<Response> {
        const path = App.getRestPath(database, container, ids, query);        
        const init = {        
            method:  method,
            headers: { 'Content-Type': 'application/json' },
            body:    body
        };
        try {
            // authenticate with cookies: "fliox-user" & "fliox-token"
            return await fetch(path, init);
        } catch (error) {
            const text = () : Promise<string> => error.message;
            const ret: Partial<Response> = {
                ok:         false,
                status:     0,
                statusText: "exception",
                text:       text,
                json:       () => { throw error.message; }
            };
            return ret as Response;
        }
    }

    private static getTaskError (content: ProtocolResponse_Union, taskIndex: number) {
        if (content.msg == "error") {
            return content.message;
        }
        const task = content.tasks[taskIndex];
        if (task.task == "error")
            return "task error:\n" + task.message;
        return undefined;
    }

    private static bracketValue = /\[(.*?)\]/;

    public static errorAsHtml (message: string, p: Resource | null) : string {
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


    // --------------------------------------- general App UI --------------------------------------- 
    public toggleDescription() : void {
        this.changeConfig("showDescription", !this.config.showDescription);   
        this.openTab(this.config.activeTab);
    }

    public openTab (tabName: string) : void {
        const config            = this.config;
        config.activeTab        = tabName;
        App.setClass(document.body, !config.showDescription, "miniHeader");
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
    
    private static setClass(element: Element, enable: boolean, className: string) {
        const classList = element.classList;
        if (enable) {
            classList.add(className);
            return;
        }
        classList.remove(className);        
    }

    private selectedCatalog:    HTMLElement;
    private hostDetails:        HostDetails;

    private async loadCluster () {
        const tasks: SyncRequestTask_Union[] = [
            { "task": "query",  "container": "containers"},
            { "task": "query",  "container": "schemas"},
            { "task": "query",  "container": "commands"},
            { "task": "command","name": "std.Details" }
        ];
        catalogExplorer.innerHTML = 'read databases <span class="spinner"></span>';
        const response  = await App.postRequestTasks("cluster", tasks, null);
        const content   = response.json as SyncResponse;
        const error     = App.getTaskError (content, 0);
        if (error) {
            catalogExplorer.innerHTML = App.errorAsHtml(error, null);
            return;
        }
        const dbContainers  = content.containers[0].entities    as DbContainers[];
        const dbSchemas     = content.containers[1].entities    as DbSchema[];
        const dbCommands    = content.containers[2].entities    as DbCommands[];
        const hubInfoResult = content.tasks[3]                  as SendCommandResult;
        this.hostDetails    = hubInfoResult.result              as HostDetails;
        //
        const name      = this.hostDetails.projectName;
        const website   = this.hostDetails.projectWebsite;
        const envName   = this.hostDetails.envName;
        const envColor  = this.hostDetails.envColor;
        if (name) {
            projectName.innerText   = name;
            document.title          = envName ? `${name} · ${envName}` : name;
        }
        if (website)    projectUrl.href     = website;
        if (envName)    envEl.innerText    = envName;
        if (envColor && CSS.supports('color', envColor)) {
            envEl.style.backgroundColor = envColor;
            envEl.style.color = getColorBasedOnBackground(envEl.style.backgroundColor);
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
            this.editor.listCommands(databaseName, commands, containers);
        };
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
            catalogLabel.style.pointerEvents = "none";
            liDatabase.append(catalogCaret);
            liDatabase.append(catalogLabel);
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
                this.editor.clearEntity(databaseName, containerName);
                this.explorer.loadContainer(params, null);
            };
            liCatalog.append(ulContainers);
            for (const containerName of dbContainer.containers) {
                const liContainer       = createEl('li');
                liContainer.title       = "container";
                const containerLabel    = createEl('div');
                containerLabel.innerHTML= "&nbsp;" + containerName;
                liContainer.append(containerLabel);
                ulContainers.append(liContainer);
            }
        }
        const schemaMap     = Schema.createEntitySchemas(this.databaseSchemas, dbSchemas);
        const monacoSchemas = Object.values(schemaMap);
        this.addSchemas(monacoSchemas);

        catalogExplorer.textContent = "";
        catalogExplorer.appendChild(ulCatalogs);

        this.editor.listCommands(dbCommands[0].id, dbCommands[0], dbContainers[0]);
    }


    // --------------------------------------- schema ---------------------------------------
    public readonly databaseSchemas: { [key: string]: DbSchema} = {};
    
    public getSchemaType(database: string) : string {
        const schema        = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;
        return `<a title="open database schema in new tab" href="./schema/${database}/html/schema.html" target="${database}">${schema.schemaName}</a>`;
    }

    public getSchemaCommand(database: string, command: string) : string {
        const schema        = this.databaseSchemas[database];
        if (!schema)
            return command;
        return `<a title="open database schema in new tab" href="./schema/${database}/html/schema.html" target="${database}">${command}</a>`;
    }

    public getSchemaTypes(database: string) : string {
        const schema        = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;
        return `<a title="open database schema types in new tab" href="./schema/${database}/index.html" target="${database}">Typescript, C#, Kotlin, JSON Schema</a>`;
    }

    public getSchemaDescription(database: string) : string {
        const schema        = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;            
        return schema._rootSchema.description ?? "";
    }

    private static getType(database: string, def: JsonType) {
        const ns          = def._namespace;
        const name        = def._typeName;
        return `<a title="open type definition in new tab" href="./schema/${database}/html/schema.html#${ns}.${name}" target="${database}">${name}</a>`;
    }

    public getEntityType(database: string, container: string) : string {
        const def  = this.getContainerSchema(database, container);
        if (!def)
            return this.schemaLess;
        return App.getType(database, def);
    }

    public getTypeLabel(database: string, type: FieldType) : string {
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

    public readonly schemaLess = '<span title="missing type definition - schema-less database" style="opacity:0.5">unknown</span>';

    public static getDatabaseLink(database: string) : string {
        return `<a title="open database in new tab" href="./rest/${database}" target="_blank" rel="noopener noreferrer">${database}</a>`;
    }

    public getContainerSchema (database: string, container: string) : JsonType | null{
        const schema = app.databaseSchemas[database];
        if (schema) {
            return schema._containerSchemas[container];
        }
        return null;
    }

    // --------------------------------------- filter --------------------------------------- 
    public filter = {} as {
        database:   string,
        container:  string
    }

    public filterOnKeyDown(event: KeyboardEvent) : void {
        if (event.code != 'Enter')
            return;
        this.applyFilter();
    }

    public applyFilter() : void {
        const database  = this.filter.database;
        const container = this.filter.container;
        const filter    = entityFilter.value;
        const query     = filter.trim() == "" ? null : `filter=${encodeURIComponent(filter)}`;
        const params: Resource    = { database: database, container: container, ids: [] };
        this.saveFilter(database, container, filter);
        this.explorer.loadContainer(params, query);
    }

    public removeFilter(): void {
        const params: Resource  = { database: this.filter.database, container: this.filter.container, ids: [] };
        this.explorer.loadContainer(params, null);
    }

    private saveFilter(database: string, container: string, filter: string) {
        const filters = this.config.filters;
        if (filter.trim() == "") {
            const filterDatabase = filters[database];
            if (filterDatabase) {
                delete filterDatabase[container];
            }
        } else {
            if (!filters[database]) filters[database] = {};     
            filters[database][container] = [filter];
        }
        this.setConfig("filters", filters);
    }

    public updateFilterLink(): void {
        const filter    = entityFilter.value;
        const query     = filter.trim() == "" ? "" : `?filter=${encodeURIComponent(filter)}`;
        const url       = `./rest/${this.filter.database}/${this.filter.container}${query}`;
        el<HTMLAnchorElement>("filterLink").href = url;
    }


    // --------------------------------------- monaco editor ---------------------------------------
    // [Monaco Editor Playground] https://microsoft.github.io/monaco-editor/playground.html#extending-language-services-configure-json-defaults

    private static addSchema(prefix: string, jsonSchema: any, schemas: MonacoSchema[]) {
        for (const schemaName in jsonSchema) {
            const schema          = jsonSchema[schemaName];
            const url             = prefix + schemaName;
            const schemaEntry: MonacoSchema = {
                uri:    "http://" + url,
                schema: schema            
            };
            schemas.push(schemaEntry);
        }
    }

    private async createProtocolSchemas () {

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
            const protocolSchemaResponse    = await fetch("schema/protocol/json-schema.json");
            const protocolSchema            = await protocolSchemaResponse.json();
            App.addSchema("protocol/json-schema/", protocolSchema, schemas);

            const filterSchemaResponse      = await fetch("schema/filter/json-schema.json");
            const filterSchema              = await filterSchemaResponse.json();
            App.addSchema("filter/json-schema/", filterSchema, schemas);

        } catch (e) {
            console.error ("load json-schema.json failed");
        }
        return schemas;
    }

    public              requestModel:       monaco.editor.ITextModel;
    public              responseModel:      monaco.editor.ITextModel;

    public              requestEditor:      monaco.editor.IStandaloneCodeEditor;
    public              responseEditor:     monaco.editor.IStandaloneCodeEditor;
    public              entityEditor:       monaco.editor.IStandaloneCodeEditor;
    public              commandValueEditor: monaco.editor.IStandaloneCodeEditor;

    private readonly    allMonacoSchemas:   MonacoSchema[] = [];

    addSchemas (monacoSchemas: MonacoSchema[]): void {
        this.allMonacoSchemas.push(...monacoSchemas);
        // [LanguageServiceDefaults | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.languages.json.LanguageServiceDefaults.html
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
            validate: true,
            schemas: this.allMonacoSchemas
        });
    }

    async setupEditors () : Promise<void>
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
        "name":  "std.Echo",
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
                if (this.editor.activeExplorerEditor != "entity")
                    return;
                // console.log('mousedown - ', e);
                const value     = this.entityEditor.getValue();
                const column    = e.target.position.column;
                const line      = e.target.position.lineNumber;
                window.setTimeout(() => { this.editor.tryFollowLink(value, column, line); }, 1);
            });
        }
        // --- create command value editor
        {
            this.commandValueEditor     = monaco.editor.create(commandValue, { });
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

    private setEditorOptions() {
        const editorSettings: monaco.editor.IEditorOptions & monaco.editor.IGlobalEditorOptions= {
            lineNumbers:    this.config.showLineNumbers ? "on" : "off",
            minimap:        { enabled: this.config.showMinimap ? true : false },
            theme:          window.appConfig.monacoTheme,
            mouseWheelZoom: true
        };
        this.requestEditor.     updateOptions ({ ...editorSettings });
        this.responseEditor.    updateOptions ({ ...editorSettings });
        this.entityEditor.      updateOptions ({ ...editorSettings });
        this.commandValueEditor.updateOptions ({ ...editorSettings });
    }


    // -------------------------------------- config --------------------------------------------
    private setConfig<K extends ConfigKey>(key: K, value: Config[K]) {
        this.config[key]    = value;
        const elem          = el(key);
        if (elem instanceof HTMLInputElement) {
            elem.value   = value as string;
            elem.checked = value as boolean;
        }
        const valueStr = JSON.stringify(value, null, 2);
        window.localStorage.setItem(key, valueStr);
    }

    private getConfig(key: keyof Config) {
        const valueStr = window.localStorage.getItem(key);
        try {
            return JSON.parse(valueStr);
        } catch(e) { }
        return undefined;
    }

    private initConfigValue(key: ConfigKey) {
        const value = this.getConfig(key);
        if (value == undefined) {
            this.setConfig(key, this.config[key]);
            return;
        }
        this.setConfig(key, value);
    }

    public config = defaultConfig;

    private loadConfig() {
        this.initConfigValue("showLineNumbers");
        this.initConfigValue("showMinimap");
        this.initConfigValue("formatEntities");
        this.initConfigValue("formatResponses");
        this.initConfigValue("activeTab");
        this.initConfigValue("showDescription");
        this.initConfigValue("filters");
    }

    public changeConfig (key: ConfigKey, value: boolean): void {
        this.setConfig(key, value);
        switch (key) {
            case "showLineNumbers":
            case "showMinimap":
                this.setEditorOptions();
                break;
        }
    }

    public formatJson(format: boolean, text: string) : string {
        if (format) {
            try {
                // const action = editor.getAction("editor.action.formatDocument");
                // action.run();
                const obj       = JSON.parse(text);
                const formatted = JSON.stringify(obj, null, 4);
                if (!Array.isArray(obj))
                    return formatted;
                let lines   = formatted.split('\n');
                lines       = lines.slice(1, lines.length - 1);
                lines       = lines.map(l => l.substring(4)); // remove 4 leading spaces
                return `[${lines.join('\n')}]`;
            }
            catch (error) {}            
        }
        return text;
    }

    public layoutEditors (): void {
        // console.log("layoutEditors - activeTab: " + activeTab)
        switch (this.config.activeTab) {
            case "playground": {
                const editors = [
                    { editor: this.responseEditor,  elem: responseContainer },               
                    { editor: this.requestEditor,   elem: requestContainer },
                ];
                this.layoutMonacoEditors(editors);
                break;
            }
            case "explorer": {
                // layout from right to left. Otherwise commandValueEditor.clientWidth is 0px;
                const editors2 = [
                    { editor: this.entityEditor,        elem: entityContainer },               
                    { editor: this.commandValueEditor,  elem: commandValue },
                ];
                this.layoutMonacoEditors(editors2);
                break;
            }
        }
    }

    private layoutMonacoEditors(pairs: { editor: monaco.editor.IStandaloneCodeEditor, elem: HTMLElement }[]) {
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


    // --------------------------------------- grid-area drag bars --------------------------------------- 
    private dragTemplate :  HTMLElement;
    private dragBar:        HTMLElement;
    private dragOffset:     number;
    private dragHorizontal: boolean;

    public startDrag(event: MouseEvent, template: string, bar: string, horizontal: boolean): void {
        // console.log(`drag start: ${event.offsetX}, ${template}, ${bar}`)
        this.dragHorizontal = horizontal;
        this.dragOffset     = horizontal ? event.offsetX : event.offsetY;
        this.dragTemplate   = el(template);
        this.dragBar        = el(bar);
        document.body.style.cursor = "ew-resize";
        document.body.onmousemove = (event)  => app.onDrag(event);
        document.body.onmouseup   = ()       => app.endDrag();
        event.preventDefault();
    }

    private getGridColumns(xy: number) {
        const prev = this.dragBar.previousElementSibling as HTMLElement;
        xy = xy - (this.dragHorizontal ? prev.offsetLeft : prev.offsetTop);
        if (xy < 20) xy = 20;
        // console.log (`drag x: ${x}`);
        switch (this.dragTemplate.id) {
            case "playground":          return [xy + "px", "var(--bar-width)", "1fr"];
            case "explorer": {
                const cols = this.dragTemplate.style.gridTemplateColumns.split(" ");
                switch (this.dragBar.id) { //  [150px var(--bar-width) 200px var(--bar-width) 1fr];
                    case "exBar1":      return [xy + "px", cols[1], cols[2], cols[3]];
                    case "exBar2":      return [cols[0], cols[1], xy + "px", cols[3]];
                }
                break;
            }
            case "explorerEdit":
                this.editor.commandEditWidth = xy + "px";
                return [this.editor.commandEditWidth, "var(--vbar-width)", "1fr"];
        }
        throw `unhandled condition in getGridColumns() id: ${this.dragTemplate?.id}`;
    }

    private onDrag(event: MouseEvent) {
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

    private endDrag() {
        if (!this.dragTemplate)
            return;
        document.body.onmousemove   = undefined;
        document.body.onmouseup     = undefined;
        this.dragTemplate           = undefined;
        document.body.style.cursor  = "auto";
    }

    public toggleTheme(): void {
        let mode = document.documentElement.getAttribute('data-theme');
        mode = mode == 'dark' ? 'light' : 'dark';
        window.setTheme(mode);
        this.setEditorOptions();
    }

    public initApp(): void {
        // --- methods without network requests
        this.loadConfig();
        this.initUserToken();
        this.openTab(app.getConfig("activeTab"));

        // --- methods performing network requests - note: methods are not awaited
        this.playground.loadExampleRequestList();
        this.loadCluster();
    }
}

export const app = new App();

