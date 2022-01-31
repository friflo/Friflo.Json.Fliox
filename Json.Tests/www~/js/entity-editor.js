import { el, createEl, parseAst } from "./types.js";
import { App, app } from "./index.js";
const entityExplorer = el("entityExplorer");
const writeResult = el("writeResult");
const readEntitiesDB = el("readEntitiesDB");
const readEntities = el("readEntities");
const catalogSchema = el("catalogSchema");
const entityType = el("entityType");
const entityIdsContainer = el("entityIdsContainer");
const entityIdsCount = el("entityIdsCount");
const entityIdsGET = el("entityIdsGET");
const entityIdsInput = el("entityIdsInput");
const entityIdsReload = el("entityIdsReload");
const entityFilter = el("entityFilter");
const filterRow = el("filterRow");
const commandSignature = el("commandSignature");
const commandLink = el("commandLink");
// entity/command editor
const commandValueContainer = el("commandValueContainer");
const commandParamBar = el("commandParamBar");
const entityContainer = el("entityContainer");
// ----------------------------------------------- EntityEditor -----------------------------------------------
export class EntityEditor {
    constructor() {
        this.selectedCommand = undefined;
        this.entityIdentity = {};
        this.entityHistoryPos = -1;
        this.entityHistory = [];
        this.entityModels = {};
        this.commandEditWidth = "60px";
        this.activeExplorerEditor = undefined;
    }
    initEditor(entityEditor, commandValueEditor) {
        this.entityEditor = entityEditor;
        this.commandValueEditor = commandValueEditor;
    }
    setSelectedCommand(element) {
        var _a;
        (_a = this.selectedCommand) === null || _a === void 0 ? void 0 : _a.classList.remove("selected");
        this.selectedCommand = element;
        element.classList.add("selected");
    }
    setEditorHeader(show) {
        const displayEntity = show == "entity" ? "contents" : "none";
        const displayCommand = show == "command" ? "contents" : "none";
        el("entityTools").style.display = displayEntity;
        el("entityHeader").style.display = displayEntity;
        el("commandTools").style.display = displayCommand;
        el("commandHeader").style.display = displayCommand;
    }
    getCommandTags(database, command, signature) {
        let label = app.schemaLess;
        if (signature) {
            const param = app.getTypeLabel(database, signature.param);
            const result = app.getTypeLabel(database, signature.result);
            label = `<span title="command parameter type"><span style="opacity: 0.5;">(param:</span> <span>${param}</span></span><span style="opacity: 0.5;">) : </span><span title="command result type">${result}</span>`;
        }
        const link = `command=${command}`;
        const url = `./rest/${database}?command=${command}`;
        return {
            link: `<a id="commandAnchor" title="command" href="${url}" target="_blank" rel="noopener noreferrer">${link}</a>`,
            label: label
        };
    }
    async sendCommand(method) {
        const value = this.commandValueEditor.getValue();
        const database = this.entityIdentity.database;
        const command = this.entityIdentity.command;
        if (!method) {
            const commandAnchor = el("commandAnchor");
            const commandValue = value == "null" ? "" : `&value=${value}`;
            const path = App.getRestPath(database, null, null, `command=${command}${commandValue}`);
            commandAnchor.href = path;
            // window.open(path, '_blank');
            return;
        }
        const response = await App.restRequest(method, value, database, null, null, `command=${command}`);
        let content = await response.text();
        content = app.formatJson(app.config.formatResponses, content);
        this.entityEditor.setValue(content);
    }
    setDatabaseInfo(database, dbContainer) {
        el("databaseName").innerHTML = App.getDatabaseLink(database);
        el("databaseSchema").innerHTML = app.getSchemaType(database);
        el("databaseExports").innerHTML = app.getSchemaExports(database);
        el("databaseType").innerHTML = dbContainer.databaseType;
    }
    listCommands(database, dbCommands, dbContainer) {
        this.setDatabaseInfo(database, dbContainer);
        this.setExplorerEditor("dbInfo");
        catalogSchema.innerHTML = app.getSchemaType(database);
        this.setEditorHeader("none");
        filterRow.style.visibility = "hidden";
        entityFilter.style.visibility = "hidden";
        readEntitiesDB.innerHTML = App.getDatabaseLink(database);
        readEntities.innerHTML = "";
        const ulDatabase = createEl('ul');
        ulDatabase.classList.value = "database";
        const commandLabel = createEl('li');
        const label = '<small style="opacity:0.5; margin-left: 10px;" title="open database commands in new tab">&nbsp;commands</small>';
        commandLabel.innerHTML = `<a href="./rest/${database}?command=DbCommands" target="_blank" rel="noopener noreferrer">${label}</a>`;
        ulDatabase.append(commandLabel);
        const liCommands = createEl('li');
        ulDatabase.appendChild(liCommands);
        const ulCommands = createEl('ul');
        ulCommands.onclick = (ev) => {
            this.setEditorHeader("command");
            const path = ev.composedPath();
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
            const liCommand = createEl('li');
            const commandLabel = createEl('div');
            commandLabel.innerText = command;
            liCommand.appendChild(commandLabel);
            const runCommand = createEl('div');
            runCommand.classList.value = "command";
            runCommand.title = "POST command";
            liCommand.appendChild(runCommand);
            ulCommands.append(liCommand);
        }
        entityExplorer.innerText = "";
        liCommands.append(ulCommands);
        entityExplorer.appendChild(ulDatabase);
    }
    storeCursor() {
        if (this.entityHistoryPos < 0)
            return;
        this.entityHistory[this.entityHistoryPos].selection = this.entityEditor.getSelection();
    }
    navigateEntity(pos) {
        if (pos < 0 || pos >= this.entityHistory.length)
            return;
        this.storeCursor();
        this.entityHistoryPos = pos;
        const entry = this.entityHistory[pos];
        this.loadEntities(entry.route, true, entry.selection);
    }
    async loadEntities(p, preserveHistory, selection) {
        this.setExplorerEditor("entity");
        this.setEditorHeader("entity");
        entityType.innerHTML = app.getEntityType(p.database, p.container);
        writeResult.innerHTML = "";
        this.setEntitiesIds(p.database, p.container, p.ids);
        if (p.ids.length == 0) {
            this.setEntityValue(p.database, p.container, "");
            return null;
        }
        // entityIdsEl.innerHTML   = `${entityLink}<span class="spinner"></span>`;
        if (!preserveHistory) {
            this.storeCursor();
            this.entityHistory[++this.entityHistoryPos] = { route: Object.assign({}, p) };
            this.entityHistory.length = this.entityHistoryPos + 1;
        }
        this.entityIdentity = {
            database: p.database,
            container: p.container,
            entityIds: [...p.ids]
        };
        // execute GET request
        const requestIds = p.ids.length == 1 ? p.ids[0] : p.ids; // load as object if exact one id
        const response = await App.restRequest("GET", null, p.database, p.container, requestIds, null);
        let content = await response.text();
        content = app.formatJson(app.config.formatEntities, content);
        this.setEntitiesIds(p.database, p.container, p.ids);
        if (!response.ok) {
            this.setEntityValue(p.database, p.container, content);
            return null;
        }
        // console.log(entityJson);
        this.setEntityValue(p.database, p.container, content);
        if (selection)
            this.entityEditor.setSelection(selection);
        // this.entityEditor.focus(); // not useful - annoying: open soft keyboard on phone
        return content;
    }
    updateGetEntitiesAnchor(database, container) {
        // console.log("updateGetEntitiesAnchor");
        const idsStr = entityIdsInput.value;
        const ids = idsStr.split(",");
        let len = ids.length;
        if (len == 1 && ids[0] == "")
            len = 0;
        entityIdsContainer.onclick = _ => app.explorer.loadContainer({ database: database, container: container, ids: null }, null);
        entityIdsContainer.innerText = `« ${container}`;
        entityIdsCount.innerText = len > 0 ? `(${len})` : "";
        let getUrl;
        if (len == 1) {
            getUrl = `./rest/${database}/${container}/${ids[0]}`;
        }
        else {
            getUrl = `./rest/${database}/${container}?ids=${idsStr}`;
        }
        entityIdsGET.href = getUrl;
    }
    setEntitiesIds(database, container, ids) {
        entityIdsReload.onclick = _ => this.loadInputEntityIds(database, container);
        entityIdsInput.onchange = _ => this.updateGetEntitiesAnchor(database, container);
        entityIdsInput.onkeydown = e => this.onEntityIdsKeyDown(e, database, container);
        entityIdsInput.value = ids.join(",");
        this.updateGetEntitiesAnchor(database, container);
    }
    formatResult(action, statusCode, status, message) {
        const color = 200 <= statusCode && statusCode < 300 ? "green" : "red";
        return `<span>
            <span style="opacity:0.7">${action} status:</span>
            <span style="color: ${color};">${statusCode} ${status}</span>
            <span>${message}</span>
        </span>`;
    }
    async loadInputEntityIds(database, container) {
        const ids = entityIdsInput.value == "" ? [] : entityIdsInput.value.split(",");
        const unchangedSelection = EntityEditor.arraysEquals(this.entityIdentity.entityIds, ids);
        const p = { database, container, ids };
        const response = await this.loadEntities(p, true, null);
        if (unchangedSelection)
            return;
        let json = JSON.parse(response);
        if (json == null) {
            json = [];
        }
        else {
            if (!Array.isArray(json))
                json = [json];
        }
        const type = app.getContainerSchema(database, container);
        app.explorer.updateExplorerEntities(json, type);
        this.selectEntities(database, container, ids);
    }
    onEntityIdsKeyDown(event, database, container) {
        if (event.code != 'Enter')
            return;
        this.loadInputEntityIds(database, container);
    }
    clearEntity(database, container) {
        this.setExplorerEditor("entity");
        this.setEditorHeader("entity");
        this.entityIdentity = {
            database: database,
            container: container,
            entityIds: []
        };
        entityType.innerHTML = app.getEntityType(database, container);
        writeResult.innerHTML = "";
        this.setEntitiesIds(database, container, []);
        this.setEntityValue(database, container, "");
    }
    static getEntityKeyName(entityType) {
        if (entityType === null || entityType === void 0 ? void 0 : entityType.key)
            return entityType.key;
        return "id";
    }
    async saveEntitiesAction() {
        const ei = this.entityIdentity;
        const jsonValue = this.entityModel.getValue();
        await this.saveEntities(ei.database, ei.container, jsonValue);
    }
    async saveEntities(database, container, jsonValue) {
        let value;
        try {
            value = JSON.parse(jsonValue);
        }
        catch (error) {
            writeResult.innerHTML = `<span style="color:red">Save failed: ${error}</code>`;
            return;
        }
        const entities = Array.isArray(value) ? value : [value];
        const type = app.getContainerSchema(database, container);
        const keyName = EntityEditor.getEntityKeyName(type);
        const ids = entities.map(entity => entity[keyName]);
        writeResult.innerHTML = 'save <span class="spinner"></span>';
        const requestIds = Array.isArray(value) ? ids : ids[0];
        const response = await App.restRequest("PUT", jsonValue, database, container, requestIds, null);
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
    selectEntities(database, container, ids) {
        var _a;
        this.entityIdentity.entityIds = ids;
        this.setEntitiesIds(database, container, ids);
        const liIds = app.explorer.findContainerEntities(ids);
        app.explorer.setSelectedEntities(ids);
        const firstRow = liIds[ids[0]];
        if (firstRow) {
            const focusedCell = app.explorer.getFocusedCell();
            const columnIndex = (_a = focusedCell === null || focusedCell === void 0 ? void 0 : focusedCell.cellIndex) !== null && _a !== void 0 ? _a : 1;
            app.explorer.setFocusCellSelectValue(firstRow.rowIndex, columnIndex, "smooth");
        }
        this.entityHistory[++this.entityHistoryPos] = { route: { database: database, container: container, ids: ids } };
        this.entityHistory.length = this.entityHistoryPos + 1;
    }
    static arraysEquals(left, right) {
        if (left.length != right.length)
            return false;
        for (let i = 0; i < left.length; i++) {
            if (left[i] != right[i])
                return false;
        }
        return true;
    }
    async deleteEntitiesAction() {
        const ei = this.entityIdentity;
        await this.deleteEntities(ei.database, ei.container, ei.entityIds);
    }
    async deleteEntities(database, container, ids) {
        writeResult.innerHTML = 'delete <span class="spinner"></span>';
        const response = await App.restRequest("DELETE", null, database, container, ids, null);
        if (!response.ok) {
            const error = await response.text();
            writeResult.innerHTML = this.formatResult("Delete", response.status, response.statusText, error);
        }
        else {
            this.entityIdentity.entityIds = [];
            writeResult.innerHTML = this.formatResult("Delete", response.status, response.statusText, "");
            this.setEntityValue(database, container, "");
            app.explorer.removeExplorerIds(ids);
        }
    }
    getModel(url) {
        this.entityModel = this.entityModels[url];
        if (!this.entityModel) {
            const entityUri = monaco.Uri.parse(url);
            this.entityModel = monaco.editor.createModel(null, "json", entityUri);
            this.entityModels[url] = this.entityModel;
        }
        return this.entityModel;
    }
    setEntityValue(database, container, value) {
        const url = `entity://${database}.${container}.json`;
        const model = this.getModel(url);
        model.setValue(value);
        this.entityEditor.setModel(model);
        if (value == "")
            return null;
        const ast = parseAst(value);
        if (!ast)
            return null;
        const containerSchema = app.getContainerSchema(database, container);
        if (containerSchema) {
            try {
                this.decorateJson(this.entityEditor, ast, containerSchema, database);
            }
            catch (error) {
                console.error("decorateJson", error);
            }
        }
        return ast;
    }
    decorateJson(editor, ast, containerSchema, database) {
        // --- deltaDecorations() -> [ITextModel | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.ITextModel.html
        const newDecorations = [
        // { range: new monaco.Range(7, 13, 7, 22), options: { inlineClassName: 'refLinkDecoration' } }
        ];
        EntityEditor.addRelationsFromAst(ast, containerSchema, (value, container) => {
            const range = EntityEditor.RangeFromNode(value);
            const markdownText = `${database}/${container}  \nFollow: (ctrl + click)`;
            const hoverMessage = [{ value: markdownText }];
            newDecorations.push({ range: range, options: { inlineClassName: 'refLinkDecoration', hoverMessage: hoverMessage } });
        });
        editor.deltaDecorations([], newDecorations);
    }
    static addRelationsFromAst(ast, schema, addRelation) {
        var _a;
        if (ast.type == "Literal")
            return;
        for (const child of ast.children) {
            switch (child.type) {
                case "Object":
                    EntityEditor.addRelationsFromAst(child, schema, addRelation);
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
                                addRelation(value, relation);
                            }
                            break;
                        case "Object":
                            const resolvedDef = property._resolvedDef;
                            if (resolvedDef) {
                                EntityEditor.addRelationsFromAst(value, resolvedDef, addRelation);
                            }
                            break;
                        case "Array":
                            const resolvedDef2 = (_a = property.items) === null || _a === void 0 ? void 0 : _a._resolvedDef;
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
                    break;
            }
        }
    }
    static hasProperty(objectNode, keyName, id) {
        for (const property of objectNode.children) {
            if (property.key.value != keyName)
                continue;
            const value = property.value;
            if (value.type == "Literal" && value.value == id)
                return true;
        }
        return false;
    }
    static findArrayItem(arrayNode, keyName, id) {
        for (const item of arrayNode.children) {
            if (item.type != "Object")
                continue;
            if (EntityEditor.hasProperty(item, keyName, id))
                return item;
        }
        return null;
    }
    static findPathRange(ast, pathString, keyName, id) {
        const astRange = EntityEditor.RangeFromNode(ast);
        const path = pathString.split('.');
        let node = ast;
        switch (node.type) {
            case "Array":
                node = EntityEditor.findArrayItem(node, keyName, id);
                if (!node)
                    return { entity: null, value: null, lastProperty: null };
                break;
            case "Object":
                if (!EntityEditor.hasProperty(node, keyName, id))
                    return { entity: null, value: null, lastProperty: null };
                break;
            default:
                return { entity: null, value: null, lastProperty: null };
        }
        const entityRange = EntityEditor.RangeFromNode(node);
        const lastProperty = node.children[node.children.length - 1];
        const lastRange = EntityEditor.RangeFromLoc(lastProperty.loc, 0);
        for (let i = 0; i < path.length; i++) {
            const name = path[i];
            if (node.type != "Object")
                return { entity: astRange, value: null, lastProperty: lastRange };
            let foundChild = null;
            for (const child of node.children) {
                if (child.key.value == name) {
                    foundChild = child;
                    break;
                }
            }
            if (!foundChild)
                return { entity: entityRange, value: null, lastProperty: lastRange };
            const valueRange = EntityEditor.RangeFromNode(foundChild.value);
            return { entity: entityRange, value: valueRange, lastProperty: lastRange };
        }
        return { entity: entityRange, value: null, lastProperty: lastRange };
    }
    static RangeFromNode(node) {
        const trim = node.type == "Literal" && typeof node.value == "string" ? 1 : 0;
        return EntityEditor.RangeFromLoc(node.loc, trim);
    }
    static RangeFromLoc(loc, trim) {
        const start = loc.start;
        const end = loc.end;
        return new monaco.Range(start.line, start.column + trim, end.line, end.column - trim);
    }
    setCommandParam(database, command, value) {
        const url = `command-param://${database}.${command}.json`;
        const isNewModel = this.entityModels[url] == undefined;
        const model = this.getModel(url);
        if (isNewModel) {
            model.setValue(value);
        }
        this.commandValueEditor.setModel(model);
    }
    setCommandResult(database, command) {
        const url = `command-result://${database}.${command}.json`;
        const model = this.getModel(url);
        this.entityEditor.setModel(model);
    }
    setExplorerEditor(edit) {
        this.activeExplorerEditor = edit;
        // console.log("editor:", edit);
        const commandActive = edit == "command";
        commandValueContainer.style.display = commandActive ? "" : "none";
        commandParamBar.style.display = commandActive ? "" : "none";
        el("explorerEdit").style.gridTemplateRows = commandActive ? `${this.commandEditWidth} var(--vbar-width) 1fr` : "0 0 1fr";
        const editorActive = edit == "command" || edit == "entity";
        entityContainer.style.display = editorActive ? "" : "none";
        el("dbInfo").style.display = edit == "dbInfo" ? "" : "none";
        //
        app.layoutEditors();
    }
    showCommand(database, commandName) {
        this.setExplorerEditor("command");
        const schema = app.databaseSchemas[database]._rootSchema;
        const signature = schema ? schema.commands[commandName] : null;
        const def = signature ? Object.keys(signature.param).length == 0 ? "null" : "{}" : "null";
        const tags = this.getCommandTags(database, commandName, signature);
        commandSignature.innerHTML = tags.label;
        commandLink.innerHTML = tags.link;
        this.entityIdentity.command = commandName;
        this.entityIdentity.database = database;
        this.setCommandParam(database, commandName, def);
        this.setCommandResult(database, commandName);
    }
    tryFollowLink(value, column, line) {
        try {
            JSON.parse(value); // early out invalid JSON
            const ast = parseAst(value);
            const database = this.entityIdentity.database;
            const containerSchema = app.getContainerSchema(database, this.entityIdentity.container);
            let entity;
            EntityEditor.addRelationsFromAst(ast, containerSchema, (value, container) => {
                if (entity || value.type != "Literal")
                    return;
                const start = value.loc.start;
                const end = value.loc.end;
                if (start.line <= line && start.column <= column && line <= end.line && column <= end.column) {
                    // console.log(`${resolvedDef.databaseName}/${resolvedDef.containerName}/${value.value}`);
                    const literalValue = value.value;
                    entity = { database: database, container: container, ids: [literalValue] };
                }
            });
            if (entity) {
                this.loadEntities(entity, false, null);
            }
        }
        catch (error) {
            writeResult.innerHTML = `<span style="color:#FF8C00">Follow link failed: ${error}</code>`;
        }
    }
}
//# sourceMappingURL=entity-editor.js.map