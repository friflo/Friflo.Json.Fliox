import { el, createEl, Resource, Entity, Method, parseAst }     from "./types.js";
import { App, app }                                             from "./index.js";

import { CommandType, JsonType }        from "../../../../Json.Tests/assets~/Schema/Typescript/JsonSchema/Friflo.Json.Fliox.Schema.JSON";
import { DbContainers, DbCommands }     from "../../../../Json.Tests/assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster";

type AddRelation = (value: jsonToAst.ValueNode, container: string) => void;

type FindRange = {
    readonly entity:        monaco.Range | null;
    readonly value:         monaco.Range | null;
    readonly lastProperty:  monaco.Range | null;
    readonly lastPath:      string[];
}

type ExplorerEditor = "command" | "entity" | "dbInfo"
type CodeEditor     = monaco.editor.IStandaloneCodeEditor;


const entityExplorer    = el("entityExplorer");
const writeResult       = el("writeResult");
const readEntitiesDB    = el("readEntitiesDB");
const readEntities      = el("readEntities");
const catalogSchema     = el("catalogSchema");
const explorerTools     = el("explorerTools");
const entityType        = el("entityType");
const entityIdsContainer= el("entityIdsContainer");
const entityIdsCount    = el("entityIdsCount");
const entityIdsGET      = el("entityIdsGET")    as HTMLAnchorElement;
const entityIdsInput    = el("entityIdsInput")  as HTMLInputElement;
const entityIdsReload   = el("entityIdsReload");

const entityFilter      = el("entityFilter")    as HTMLInputElement;
const filterRow         = el("filterRow");
const commandSignature  = el("commandSignature");
const commandAnchor     = el("commandAnchor")   as HTMLAnchorElement;

// entity/command editor
const commandValueContainer  = el("commandValueContainer");
const commandParamBar        = el("commandParamBar");
const entityContainer        = el("entityContainer");


// ----------------------------------------------- EntityEditor -----------------------------------------------
export class EntityEditor
{
    private entityEditor:       CodeEditor  = null;
    private commandValueEditor: CodeEditor  = null;
    private selectedCommand:    HTMLElement = null;

    public initEditor(entityEditor: CodeEditor, commandValueEditor: CodeEditor) : void {
        this.entityEditor       = entityEditor;
        this.commandValueEditor = commandValueEditor;
    }

    private setSelectedCommand(element: HTMLElement) {
        this.selectedCommand?.classList.remove("selected");
        this.selectedCommand = element;
        element.classList.add("selected");        
    }

    private setEditorHeader(show: "entity" | "command" | "database" | "none") {
        const displayEntity  = show == "entity"     ? "contents" : "none";
        const displayCommand = show == "command"    ? "contents" : "none";
        const displayDB      = show == "database"   ? "contents" : "none";
        el("entityTools")  .style.display = displayEntity;        
        el("entityHeader") .style.display = displayEntity;        
        el("commandTools") .style.display = displayCommand;
        el("commandHeader").style.display = displayCommand;
        el("databaseTools").style.display = displayDB;
    }

    private getCommandDocsEl(database: string, command: string, signature: CommandType) {
        if (!signature)
            return app.schemaLess;
        const param         = app.getTypeLabel(database, signature.param);
        const result        = app.getTypeLabel(database, signature.result);
        const commandDocs   = app.getSchemaCommand(database, command);        
        const el =
        `<span title="command parameter type">
            ${commandDocs}
            <span style="opacity: 0.5;">(param:</span>
            <span>${param}</span>
        </span>
        <span style="opacity: 0.5;">) :&nbsp;</span>
        <span title="command result type">${result}</span>`;
        return el;
    }

    private getCommandUrl(database: string, command: string) {
        // const value     = this.commandValueEditor.getValue();
        return `./rest/${database}?command=${command}`;
    }

    public async sendCommand(method: Method) : Promise<void> {
        const value     = this.commandValueEditor.getValue();
        const database  = this.entityIdentity.database;
        const command   = this.entityIdentity.command;
        if (!method) {
            const commandAnchor =  el("commandAnchor") as HTMLAnchorElement;
            const commandValue    = value == "null" ? "" : `&value=${value}`;
            const path          = App.getRestPath( database, null, null, `command=${command}${commandValue}`);
            commandAnchor.href  = path;
            // window.open(path, '_blank');
            return;
        }
        const response  = await App.restRequest(method, value, database, null, null, `command=${command}`);
        let content     = await response.text();
        content         = app.formatJson(app.config.formatResponses, content);
        this.entityEditor.setValue(content);
    }

    private setDatabaseInfo(database: string, dbContainer: DbContainers) {
        el("databaseName").innerHTML      = App.getDatabaseLink(database);
        el("databaseSchema").innerHTML    = app.getSchemaType(database);
        el("databaseExports").innerHTML   = app.getSchemaExports(database);
        el("databaseType").innerHTML      = dbContainer.databaseType;        
    }

    public listCommands (database: string, dbCommands: DbCommands, dbContainer: DbContainers) : void {
        this.setDatabaseInfo(database, dbContainer);
        this.setExplorerEditor("dbInfo");

        const schemaType                = app.getSchemaType(database);
        catalogSchema.innerHTML         = schemaType;
        explorerTools.innerHTML         = "";
        this.setEditorHeader("database");
        el("databaseLabel").innerHTML   = `${schemaType}&nbsp;<span style="opacity:0.5;">schema</span>`;
        filterRow.style.visibility      = "hidden";
        entityFilter.style.visibility   = "hidden";
        readEntitiesDB.innerHTML        = App.getDatabaseLink(database);
        readEntities.innerHTML          = "";

        const ulDatabase            = createEl('ul');
        ulDatabase.classList.value  = "database";
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
        };
        for (const command of dbCommands.commands) {
            const liCommand             = createEl('li');
            const commandLabel          = createEl('div');
            commandLabel.innerText      = command;
            liCommand.appendChild(commandLabel);
            const runCommand            = createEl('div');
            runCommand.classList.value  = "command";
            runCommand.title            = "POST command";
            liCommand.appendChild(runCommand);

            ulCommands.append(liCommand);
        }
        entityExplorer.innerText = "";
        liCommands.append(ulCommands);
        entityExplorer.appendChild(ulDatabase);
    }

    private entityIdentity = { } as {
        readonly    database:   string,
        readonly    container:  string,
                    entityIds:  string[],
        readonly    command?:   string
    }

    public  entityHistoryPos    = -1;
    private entityHistory: {
        selection?: monaco.Selection,
        route:      Resource
    }[] = [];

    private storeCursor() {
        if (this.entityHistoryPos < 0)
            return;
        this.entityHistory[this.entityHistoryPos].selection    = this.entityEditor.getSelection();
    }

    public navigateEntity(pos: number) : void {
        if (pos < 0 || pos >= this.entityHistory.length)
            return;
        this.storeCursor();
        this.entityHistoryPos   = pos;
        const entry             = this.entityHistory[pos];
        this.loadEntities(entry.route, true, entry.selection);
    }

    public async loadEntities (p: Resource, preserveHistory: boolean, selection: monaco.IRange) : Promise<string> {
        this.setExplorerEditor("entity");
        this.setEditorHeader("entity");
        entityType.innerHTML    = app.getEntityType  (p.database, p.container);
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

        content             = app.formatJson(app.config.formatEntities, content);
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

    private updateGetEntitiesAnchor(database: string, container: string) {
        // console.log("updateGetEntitiesAnchor");
        const idsStr = entityIdsInput.value;
        const ids    = idsStr.split(",");
        let   len    = ids.length;
        if (len == 1 && ids[0] == "") len = 0;
        entityIdsContainer.onclick      = () => app.explorer.loadContainer({ database: database, container: container, ids: null }, null);
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

    private setEntitiesIds (database: string, container: string, ids: string[]) {
        entityIdsReload.onclick     = () => this.loadInputEntityIds      (database, container);
        entityIdsInput.onchange     = () => this.updateGetEntitiesAnchor (database, container);
        entityIdsInput.onkeydown    = e => this.onEntityIdsKeyDown   (e, database, container);
        entityIdsInput.value        = ids.join (",");
        this.updateGetEntitiesAnchor(database, container);
    }

    private formatResult (action: string, statusCode: number, status: string, message: string) {
        const color = 200 <= statusCode && statusCode < 300 ? "green" : "red";
        return `<span>
            <span style="opacity:0.7">${action} status:</span>
            <span style="color: ${color};">${statusCode} ${status}</span>
            <span>${message}</span>
        </span>`;
    }

    private async loadInputEntityIds (database: string, container: string) {
        const ids               = entityIdsInput.value == "" ? [] : entityIdsInput.value.split(",");
        const unchangedSelection= EntityEditor.arraysEquals(this.entityIdentity.entityIds, ids);
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
        const type          = app.getContainerSchema(database, container);
        app.explorer.updateExplorerEntities(json, type);
        this.selectEntities(database, container, ids);
    }

    private onEntityIdsKeyDown(event: KeyboardEvent, database: string, container: string) {
        if (event.code != 'Enter')
            return;
        this.loadInputEntityIds(database, container);
    }

    public clearEntity (database: string, container: string) : void {
        this.setExplorerEditor("entity");
        this.setEditorHeader("entity");

        this.entityIdentity = {
            database:   database,
            container:  container,
            entityIds:  [],
            command:    null
        };
        entityType.innerHTML    = app.getEntityType (database, container);
        writeResult.innerHTML   = "";
        this.setEntitiesIds(database, container, []);
        this.setEntityValue(database, container, "");
    }

    public  static getEntityKeyName (entityType: JsonType) : string {
        if (entityType?.key)
            return entityType.key;
        return "id";
    }

    public async saveEntitiesAction () : Promise<void> {
        const ei        = this.entityIdentity;
        const jsonValue = this.entityModel.getValue();
        await this.saveEntities(ei.database, ei.container, jsonValue);
    }

    private async saveEntities (database: string, container: string, jsonValue: string)
    {
        let value:      Entity | Entity[];
        try {
            value = JSON.parse(jsonValue) as Entity | Entity[];
        } catch (error) {
            writeResult.innerHTML = `<span style="color:red">Save failed: ${error}</code>`;
            return;
        }
        const entities          = Array.isArray(value) ? value : [value];
        const type              = app.getContainerSchema(database, container);
        const keyName           = EntityEditor.getEntityKeyName(type as JsonType);
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
        app.explorer.updateExplorerEntities(entities, type);
        if (EntityEditor.arraysEquals(this.entityIdentity.entityIds, ids))
            return;
        this.selectEntities(database, container, ids);        
    }

    private selectEntities(database: string, container: string, ids: string[]) {
        this.entityIdentity.entityIds = ids;
        this.setEntitiesIds(database, container, ids);
        const rowIndices = app.explorer.findRowIndices(ids);

        app.explorer.setSelectedEntities(ids);
        const firstRow = rowIndices[ids[0]];
        if (firstRow) {
            const focusedCell   = app.explorer.getFocusedCell();
            const column        = focusedCell?.column ?? 1;
            app.explorer.setFocusCellSelectValue(firstRow, column, "smooth");
        }
        this.entityHistory[++this.entityHistoryPos] = { route: { database: database, container: container, ids:ids }};
        this.entityHistory.length = this.entityHistoryPos + 1;        
    }

    private static arraysEquals(left: string[], right: string []) : boolean {
        if (left.length != right.length)
            return false;
        for (let i = 0; i < left.length; i++) {
            if (left[i] != right[i])
                return false;
        }
        return true;
    }

    public async deleteEntitiesAction () : Promise<void> {
        const ei = this.entityIdentity;
        await this.deleteEntities (ei.database, ei.container, ei.entityIds);
    }

    public  async deleteEntities (database: string, container: string, ids: string[]) : Promise<void> {
        writeResult.innerHTML = 'delete <span class="spinner"></span>';
        const response = await App.restRequest("DELETE", null, database, container, ids, null);        
        if (!response.ok) {
            const error = await response.text();
            writeResult.innerHTML = this.formatResult("Delete", response.status, response.statusText, error);
        } else {
            this.entityIdentity.entityIds = [];
            writeResult.innerHTML = this.formatResult("Delete", response.status, response.statusText, "");
            this.setEntityValue(database, container, "");
            app.explorer.removeExplorerIds(ids);
        }
    }

    private entityModel:    monaco.editor.ITextModel;
    private entityModels:   {[key: string]: monaco.editor.ITextModel} = { };

    private getModel (url: string) : monaco.editor.ITextModel {
        this.entityModel = this.entityModels[url];
        if (!this.entityModel) {
            const entityUri         = monaco.Uri.parse(url);
            this.entityModel        = monaco.editor.createModel(null, "json", entityUri);
            this.entityModels[url]  = this.entityModel;
        }
        return this.entityModel;
    }

    private setEntityValue (database: string, container: string, value: string) : jsonToAst.ValueNode {
        const url   = `entity://${database}.${container}.json`;
        const model = this.getModel(url);
        model.setValue(value);
        this.entityEditor.setModel (model);
        if (value == "")
            return null;
        const ast = parseAst(value);
        if (!ast)
            return null;
        const containerSchema = app.getContainerSchema(database, container);
        if (containerSchema) {
            try {
                this.decorateJson(this.entityEditor, ast, containerSchema, database);
            } catch (error) {
                console.error("decorateJson", error);
            }
        }
        return ast;
    }

    private decorateJson(editor: CodeEditor, ast: jsonToAst.ValueNode, containerSchema: JsonType, database: string) {        
        // --- deltaDecorations() -> [ITextModel | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.ITextModel.html
        const newDecorations: monaco.editor.IModelDeltaDecoration[] = [
            // { range: new monaco.Range(7, 13, 7, 22), options: { inlineClassName: 'refLinkDecoration' } }
        ];
        EntityEditor.addRelationsFromAst(ast, containerSchema, (value, container) => {
            const range         = EntityEditor.RangeFromNode(value);
            const markdownText  = `${database}/${container}  \nFollow: (ctrl + click)`;
            const hoverMessage  = [ { value: markdownText } ];
            newDecorations.push({ range: range, options: { inlineClassName: 'refLinkDecoration', hoverMessage: hoverMessage }});
        });
        editor.deltaDecorations([], newDecorations);
    }

    private static addRelationsFromAst(ast: jsonToAst.ValueNode, schema: JsonType, addRelation: AddRelation) {
        if (ast.type == "Literal")
            return;
        for (const child of ast.children) {
            switch (child.type) {
                case "Object":
                    EntityEditor.addRelationsFromAst(child, schema, addRelation);
                    break;
                case "Array":
                    break;
                case "Property": {
                    // if (child.key.value == "employees") debugger;
                    const property = schema.properties[child.key.value];
                    if (!property)
                        continue;
                    const value = child.value;

                    switch (value.type) {
                        case "Literal": {
                            const relation = property.relation;
                            if (relation && value.value !== null) {
                                addRelation (value, relation);
                            }
                            break;
                        }
                        case "Object": {
                            const resolvedDef = property._resolvedDef;
                            if (resolvedDef) {
                                EntityEditor.addRelationsFromAst(value, resolvedDef, addRelation);
                            }
                            break;
                        }
                        case "Array": {
                            const resolvedDef2 = property.items?._resolvedDef;
                            if (resolvedDef2) {
                                EntityEditor.addRelationsFromAst(value, resolvedDef2, addRelation);
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
                    }
                    break;
                }
            }
        }
    }

    private static hasProperty(objectNode: jsonToAst.ObjectNode, keyName: string, id: string) : boolean {
        for (const property of objectNode.children) {
            if (property.key.value != keyName)
                continue;
            const value = property.value;
            if (value.type == "Literal" && value.value == id)
                return true;
        }
        return false;
    }

    private static findArrayItem(arrayNode: jsonToAst.ArrayNode, keyName: string, id: string) : jsonToAst.ObjectNode | null {
        for(const item of arrayNode.children) {
            if (item.type != "Object")
                continue;
            if (EntityEditor.hasProperty(item, keyName, id))
                return item;
        }
        return null;
    }

    public static findPathRange(ast: jsonToAst.ValueNode, pathString: string, keyName: string, id: string) : FindRange {
        const astRange  = EntityEditor.RangeFromNode(ast);
        const path      = pathString.split('.');
        let   node      = ast;
        switch (node.type) {
            case "Array":
                node = EntityEditor.findArrayItem(node, keyName, id);
                if (!node)
                    return { entity: null, value: null, lastProperty: null, lastPath: null };
                break;
            case "Object":
                if (!EntityEditor.hasProperty(node, keyName, id))
                    return { entity: null, value: null, lastProperty: null, lastPath: null };
                break;
            default:
                return { entity: null, value: null, lastProperty: null, lastPath: null };
        }
        const entityRange                   = EntityEditor.RangeFromLoc(node.loc, 0);
        let   lastRange:    monaco.Range    = null;
        const lastPath:     string[]        = [];

        // walk path in object
        let objectNode: jsonToAst.ObjectNode    = node;
        let foundChild: jsonToAst.PropertyNode;
        let i = 0;
        for (; i < path.length; i++) {
            foundChild = null;
            const name = path[i];
            if (objectNode.type != "Object")
                return { entity: astRange, value: null, lastProperty: null, lastPath: null };
            const children = objectNode.children;
            for (const child of children) {
                if (child.key.value == name) {
                    foundChild  = child;
                    lastPath.push(name);
                    if (child.value.type == "Object") {
                        objectNode  = child.value;
                    }
                    break;
                }
            }
            const lastChild = children[children.length - 1];
            lastRange       = EntityEditor.RangeFromLoc(lastChild.loc, 0);
            if (!foundChild)
                break;
        }

        if (foundChild) {
            const valueRange = EntityEditor.RangeFromLoc(foundChild.value.loc, 0);
            return { entity: entityRange, value: valueRange, lastProperty: lastRange, lastPath };
        }
        return { entity: entityRange, value: null, lastProperty: lastRange, lastPath };
    }

    private static RangeFromNode(node: jsonToAst.ValueNode) : monaco.Range{
        const trim  = node.type == "Literal" && typeof node.value == "string" ? 1 : 0;
        return EntityEditor.RangeFromLoc(node.loc, trim);
    }

    private static RangeFromLoc(loc: jsonToAst.Location, trim: number) : monaco.Range{
        const start = loc.start;
        const end   = loc.end;
        return new monaco.Range(start.line, start.column + trim, end.line, end.column - trim);
    }

    private setCommandParam (database: string, command: string, value: string) {
        const url           = `command-param://${database}.${command}.json`;
        const isNewModel    = this.entityModels[url] == undefined;
        const model         = this.getModel(url);
        if (isNewModel) {
            model.setValue(value);
        }
        this.commandValueEditor.setModel (model);
    }

    private setCommandResult (database: string, command: string) {
        const url   = `command-result://${database}.${command}.json`;
        const model = this.getModel(url);
        this.entityEditor.setModel (model);
    }

    public commandEditWidth = "60px";
    public activeExplorerEditor: ExplorerEditor = undefined;

    public setExplorerEditor(edit : ExplorerEditor) : void {
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
        app.layoutEditors();
    }

    private showCommand(database: string, commandName: string) {
        this.setExplorerEditor("command");

        const schema        = app.databaseSchemas[database]._rootSchema;
        const signature     = schema ? schema.commands[commandName] : null;
        const def           = signature ? Object.keys(signature.param).length  == 0 ? "null" : "{}" : "null";
        const docsEl        = this.getCommandDocsEl(database, commandName, signature);
        commandSignature.innerHTML  = docsEl;
        commandAnchor.innerText     = `command=${commandName}`;
        commandAnchor.href          = this.getCommandUrl(database, commandName);

        this.entityIdentity = {
            database:   database,
            container:  null,
            entityIds:  null,
            command:    commandName,
        };
        this.setCommandParam (database, commandName, def);
        this.setCommandResult(database, commandName);
    }

    public tryFollowLink(value: string, column: number, line: number) : void {
        try {
            JSON.parse(value);  // early out invalid JSON
            const ast               = parseAst(value);
            const database          = this.entityIdentity.database;
            const containerSchema   = app.getContainerSchema(database, this.entityIdentity.container);

            let entity: Resource;
            EntityEditor.addRelationsFromAst(ast, containerSchema, (value, container) => {
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
}
