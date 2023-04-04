import { el, createEl, parseAst } from "./types.js";
import { App, app } from "./index.js";
import { Schema } from "./schema.js";
const entityExplorer = el("entityExplorer");
const writeResult = el("writeResult");
const readEntitiesDB = el("readEntitiesDB");
const readEntities = el("readEntities");
const readEntitiesCount = el("readEntitiesCount");
const catalogSchema = el("catalogSchema");
const explorerTools = el("explorerTools");
const entityType = el("entityType");
const entityIdsContainer = el("entityIdsContainer");
const entityIdsCount = el("entityIdsCount");
const entityIdsGET = el("entityIdsGET");
const entityIdsInput = el("entityIdsInput");
const entityIdsReload = el("entityIdsReload");
const entityDelete = el("entityDelete");
const entitySave = el("entitySave");
const entityPatch = el("entityPatch");
const filterArea = el("filterArea");
const filterRow = el("filterRow");
const commandSignature = el("commandSignature");
const commandAnchor = el("commandAnchor");
const commandDocs = el("commandDocs");
// entity/command editor
const commandValueContainer = el("commandValueContainer");
const commandParamBar = el("commandParamBar");
const entityContainer = el("entityContainer");
// ----------------------------------------------- EntityEditor -----------------------------------------------
export class EntityEditor {
    constructor() {
        this.entityEditor = null;
        this.commandValueEditor = null;
        this.selectedCommandEl = null;
        this.selectedCommands = {}; // store selected command per database
        this.entityIdentity = {};
        this.entityHistoryPos = -1;
        this.entityHistory = [];
        this.entityModels = {};
        this.commandEditWidth = "100px";
        this.activeExplorerEditor = undefined;
    }
    initEditor(entityEditor, commandValueEditor) {
        this.entityEditor = entityEditor;
        this.commandValueEditor = commandValueEditor;
        entityEditor.onDidChangeModelContent(() => {
            const length = entityEditor.getModel().getValueLength();
            this.enableSaveButton(length != 0);
        });
    }
    enableSaveButton(enable) {
        entitySave.disabled = !enable;
        entityPatch.disabled = !enable;
    }
    setEditorHeader(show) {
        const displayEntity = show == "entity" ? "contents" : "none";
        const displayCommand = show == "command" ? "contents" : "none";
        const displayDB = show == "database" ? "contents" : "none";
        el("entityTools").style.display = displayEntity;
        el("entityHeader").style.display = displayEntity;
        el("commandTools").style.display = displayCommand;
        el("commandHeader").style.display = displayCommand;
        el("databaseTools").style.display = displayDB;
    }
    async sendCommand() {
        const param = this.commandValueEditor.getValue();
        const e = this.entityIdentity;
        const database = e.database;
        const command = e.command;
        const type = e.msgType;
        const response = await App.restRequest("POST", param, database, null, `${type}=${command}`);
        let content = await response.text();
        content = app.formatJson(app.config.formatResponses, content);
        this.entityEditor.setValue(content);
    }
    setDatabaseInfo(database, dbContainer) {
        const schemaType = app.getSchemaType(database);
        const diagramLink = App.getDiagramLink(database);
        const apiLinks = App.getApiLinks(database, "open database API", "");
        el("databaseName").innerHTML = App.getDatabaseLink(database);
        el("databaseSchema").innerHTML = `${schemaType} ${diagramLink} ${apiLinks}`;
        el("databaseTypes").innerHTML = app.getSchemaTypes(database);
        el("databaseStorage").innerHTML = dbContainer.storage;
        el("schemaDescription").innerHTML = app.getSchemaDescription(database);
    }
    setExplorerSelection(database, command, element) {
        var _a;
        (_a = this.selectedCommandEl) === null || _a === void 0 ? void 0 : _a.classList.remove("selected");
        this.selectedCommandEl = element;
        element.classList.add("selected");
        this.selectedCommands[database] = command;
    }
    selectDatabaseInfo(database) {
        this.setExplorerEditor("dbInfo");
        const infoEl = el("databaseInfo");
        this.setExplorerSelection(database, null, infoEl);
        this.setEditorHeader("database");
    }
    selectCommand(database, command, message) {
        this.setEditorHeader("command");
        this.setExplorerSelection(database, command, message.li);
        this.showCommand(database, command, message.type);
    }
    listCommands(database, dbMessages, dbContainer) {
        app.explorer.initExplorer(null, null, null, null);
        this.setDatabaseInfo(database, dbContainer);
        const schemaType = app.getSchemaType(database);
        const diagramLink = App.getDiagramLink(database);
        catalogSchema.innerHTML = `${schemaType}&nbsp;${diagramLink}`;
        explorerTools.innerHTML = "";
        el("databaseLabel").innerHTML = schemaType;
        filterRow.style.visibility = "hidden";
        filterArea.style.visibility = "hidden";
        const messagesLink = App.getMessagesLink(database);
        readEntitiesDB.innerHTML = messagesLink;
        readEntities.innerHTML = "";
        readEntitiesCount.innerHTML = "";
        const ulDatabase = createEl('ul');
        ulDatabase.classList.value = "database";
        // database link
        const databaseLink = createEl('li');
        databaseLink.id = "databaseInfo";
        databaseLink.style.display = "flex";
        databaseLink.style.marginBottom = "5px";
        const databaseAnchor = createEl("a");
        databaseAnchor.href = "#";
        databaseAnchor.style.width = "100%";
        databaseAnchor.target = "blank";
        databaseAnchor.rel = "noopener noreferrer";
        databaseAnchor.onclick = (ev) => { this.selectDatabaseInfo(database); ev.preventDefault(); };
        databaseAnchor.innerHTML = '<span style="" title="show general database information">database info</span>';
        databaseLink.append(databaseAnchor);
        ulDatabase.append(databaseLink);
        const messageMap = {};
        // commands link
        const commandLink = this.createMessagesDocLink(database, "commands");
        ulDatabase.append(commandLink);
        // commands list
        const messagesLi = this.createMessagesLi(database, "cmd", dbMessages.commands, messageMap);
        ulDatabase.appendChild(messagesLi);
        const messages = dbMessages.messages;
        if (messages && messages.length > 0) {
            // messages link
            const commandLink = this.createMessagesDocLink(database, "messages");
            ulDatabase.append(commandLink);
            // messages list
            const messagesLi = this.createMessagesLi(database, "msg", dbMessages.messages, messageMap);
            ulDatabase.appendChild(messagesLi);
        }
        entityExplorer.innerText = "";
        entityExplorer.appendChild(ulDatabase);
        const selectedCommand = this.selectedCommands[database];
        const message = messageMap[selectedCommand];
        if (message) {
            this.selectCommand(database, selectedCommand, message);
            message.li.scrollIntoView();
        }
        else {
            this.selectDatabaseInfo(database);
        }
    }
    createMessagesDocLink(database, category) {
        const categoryEl = createEl('li');
        const apiLinks = category == "commands" ? " " + App.getApiLinks(database, "open commands API", "#/commands") : "";
        categoryEl.innerHTML = app.getSchemaCommand(database, category, category) + apiLinks;
        if (!app.databaseSchemas[database]) {
            categoryEl.innerHTML = `<span style="opacity: 0.5;">${category}</span>`;
        }
        const child = categoryEl.firstElementChild;
        child.style.marginLeft = "10px";
        return categoryEl;
    }
    createMessagesLi(database, type, messages, messageMap) {
        const ulCommands = createEl('ul');
        ulCommands.onclick = (ev) => {
            const path = ev.composedPath();
            let selectedElement = path[0];
            // in case of a multiline text selection selectedElement is the parent
            const tagName = selectedElement.tagName;
            if (tagName == "SPAN" || tagName == "DIV") {
                selectedElement = path[1];
            }
            const commandName = selectedElement.children[0].textContent;
            const message = messageMap[commandName];
            this.selectCommand(database, commandName, message);
            if (path[0].classList.contains("command")) {
                this.sendCommand();
            }
        };
        for (const message of messages) {
            const liCommand = createEl('li');
            const commandLabel = createEl('div');
            commandLabel.innerText = message;
            liCommand.appendChild(commandLabel);
            const runCommand = createEl('div');
            runCommand.classList.value = "command";
            runCommand.title = `Send ${type} using POST`;
            liCommand.appendChild(runCommand);
            ulCommands.append(liCommand);
            messageMap[message] = { li: liCommand, type: type };
        }
        const liCommands = createEl('li');
        liCommands.append(ulCommands);
        return liCommands;
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
        const ei = this.entityIdentity = {
            database: p.database,
            container: p.container,
            entityIds: [...p.ids]
        };
        // execute GET request
        const response = await EntityEditor.requestIds(p.database, p.container, p.ids);
        let content = await response.text();
        content = app.formatJson(app.config.formatEntities, content);
        // this.setEntitiesIds(p.database, p.container, p.ids);
        if (!response.ok) {
            this.setEntityValue(p.database, p.container, content);
            return null;
        }
        // console.log(entityJson);
        app.explorer.setSelectedEntities(ei.database, ei.container, ei.entityIds);
        this.setEntityValue(p.database, p.container, content);
        if (selection)
            this.entityEditor.setSelection(selection);
        // this.entityEditor.focus(); // not useful - annoying: open soft keyboard on phone
        return content;
    }
    static async requestIds(database, container, requestIds) {
        if (requestIds.length == 1) {
            return await App.restRequest("GET", null, database, `${container}/${requestIds[0]}`, null);
        }
        if (this.testBulkGet) {
            const query = `ids=${requestIds.join(',')}`;
            return await App.restRequest("GET", null, database, container, query);
        }
        const idsStr = JSON.stringify(requestIds);
        return await App.restRequest("POST", idsStr, database, `${container}/bulk-get`, null);
    }
    // updateCount = 0;
    updateGetEntitiesAnchor(database, container) {
        // console.trace(`updateGetEntitiesAnchor ${++this.updateCount}`);
        const idsStr = entityIdsInput.value;
        entityDelete.disabled = idsStr == "";
        const ids = idsStr.split(",");
        let len = ids.length;
        if (len == 1 && ids[0] == "")
            len = 0;
        entityIdsContainer.onclick = async () => {
            await app.explorer.loadContainer({ database: database, container: container, ids: null }, null);
            const ei = this.entityIdentity;
            app.explorer.setSelectedEntities(ei.database, ei.container, ei.entityIds);
        };
        entityIdsContainer.innerText = `« ${container}`;
        entityIdsCount.innerText = len > 0 ? `· ${len}` : "";
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
        entityIdsReload.onclick = () => this.loadEntitiesById(database, container);
        // entityIdsInput.onchange     = () => this.updateGetEntitiesAnchor (database, container);
        entityIdsInput.onkeydown = e => this.onEntityIdsKeyDown(e, database, container);
        entityIdsInput.oninput = () => { entityDelete.disabled = true; };
        entityIdsInput.value = ids.join(",");
        this.updateGetEntitiesAnchor(database, container);
    }
    static formatResult(action, statusCode, status, message) {
        const color = 200 <= statusCode && statusCode < 300 ? "green" : "red";
        return `<span>
            <span style="opacity:0.7">${action} status:</span>
            <span style="color: ${color};">${statusCode} ${status}</span>
            <span>${message}</span>
        </span>`;
    }
    async loadEntitiesById(database, container) {
        const text = entityIdsInput.value;
        const ids = text == "" ? [] : text.split(",");
        const unchangedSelection = EntityEditor.arraysEquals(this.entityIdentity.entityIds, ids);
        const p = { database, container, ids };
        const response = await this.loadEntities(p, true, null);
        if (unchangedSelection)
            return;
        let entities = JSON.parse(response);
        if (entities == null) {
            entities = [];
        }
        else {
            if (!Array.isArray(entities))
                entities = [entities];
        }
        const type = app.getContainerSchema(database, container);
        app.explorer.updateEntities(database, container, entities, type, "All");
        this.selectEntities(database, container, ids);
        if (entities.length != ids.length) {
            entityIdsCount.innerText = `· ${entities.length}/${ids.length}`;
        }
    }
    onEntityIdsKeyDown(event, database, container) {
        if (event.code != 'Enter')
            return;
        this.loadEntitiesById(database, container);
    }
    clearEntity(database, container) {
        this.setExplorerEditor("entity");
        this.setEditorHeader("entity");
        this.entityIdentity = {
            database: database,
            container: container,
            entityIds: [],
            command: null,
            msgType: null
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
    /**
     * Store - aka upsert - the entities in the database using a PUT request
     */
    async saveEntitiesAction() {
        const ei = this.entityIdentity;
        const jsonValue = this.entityModel.getValue();
        await this.changeEntities(ei.database, ei.container, jsonValue, "PUT");
    }
    /**
     * Merge the given members to the stored entity using a PATCH request.
     * See RFC 7386 - JSON Merge Patch https://www.rfc-editor.org/rfc/rfc7386
     **/
    async patchEntitiesAction() {
        const ei = this.entityIdentity;
        const jsonValue = this.entityModel.getValue();
        await this.changeEntities(ei.database, ei.container, jsonValue, "PATCH");
    }
    async changeEntities(database, container, jsonValue, method) {
        const action = method == "PUT" ? "Save" : "Patch";
        let value;
        try {
            value = JSON.parse(jsonValue);
        }
        catch (error) {
            writeResult.innerHTML = `<span style="color:red">${action} failed: ${error}</code>`;
            return;
        }
        const entities = Array.isArray(value) ? value : [value];
        const type = app.getContainerSchema(database, container);
        const keyName = EntityEditor.getEntityKeyName(type);
        const ids = entities.map(entity => String(entity[keyName]));
        writeResult.innerHTML = `${action} <span class="spinner"></span>`;
        const containerPath = Array.isArray(value) ? container : `${container}/${ids[0]}`;
        const response = await App.restRequest(method, jsonValue, database, containerPath, null);
        if (!response.ok) {
            const error = await response.text();
            writeResult.innerHTML = EntityEditor.formatResult(action, response.status, response.statusText, error);
            return;
        }
        writeResult.innerHTML = EntityEditor.formatResult(action, response.status, response.statusText, "");
        // add or update explorer entities
        const updateCell = method == "PUT" ? "All" : "NotNull";
        app.explorer.updateEntities(database, container, entities, type, updateCell);
        if (EntityEditor.arraysEquals(this.entityIdentity.entityIds, ids))
            return;
        this.setEntitiesIds(database, container, ids);
        this.selectEntities(database, container, ids);
    }
    selectEntities(database, container, ids) {
        var _a;
        const ei = this.entityIdentity;
        ei.entityIds = ids;
        // this.setEntitiesIds(database, container, ids);
        const rowIndices = app.explorer.findRowIndices(ids);
        app.explorer.setSelectedEntities(ei.database, ei.container, ei.entityIds);
        const firstRow = rowIndices[ids[0]];
        if (firstRow) {
            const focusedCell = app.explorer.getFocusedCell();
            const column = (_a = focusedCell === null || focusedCell === void 0 ? void 0 : focusedCell.column) !== null && _a !== void 0 ? _a : 1;
            app.explorer.setFocusCellSelectValue(firstRow, column, "smooth");
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
        const response = await EntityEditor.deleteIds(database, container, ids);
        if (!response.ok) {
            const error = await response.text();
            writeResult.innerHTML = EntityEditor.formatResult("Delete", response.status, response.statusText, error);
            return;
        }
        this.entityIdentity.entityIds = [];
        this.setEntitiesIds(database, container, []);
        writeResult.innerHTML = EntityEditor.formatResult("Delete", response.status, response.statusText, "");
        this.setEntityValue(database, container, "");
        app.explorer.removeExplorerIds(ids);
    }
    static async deleteIds(database, container, ids) {
        if (ids.length == 1) {
            return await App.restRequest("DELETE", null, database, `${container}/${ids[0]}`, null);
        }
        const idsStr = JSON.stringify(ids);
        return await App.restRequest("POST", idsStr, database, `${container}/bulk-delete`, null);
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
        this.enableSaveButton(value != "");
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
                                addRelation(value, relation);
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
                    }
                    break;
                }
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
                    return { entity: null, value: null, lastProperty: null, lastPath: null };
                break;
            case "Object":
                if (!EntityEditor.hasProperty(node, keyName, id))
                    return { entity: null, value: null, lastProperty: null, lastPath: null };
                break;
            default:
                return { entity: null, value: null, lastProperty: null, lastPath: null };
        }
        const entityRange = EntityEditor.RangeFromLoc(node.loc, 0);
        let lastRange = null;
        const lastPath = [];
        // walk path in object
        let objectNode = node;
        let foundChild;
        let i = 0;
        for (; i < path.length; i++) {
            foundChild = null;
            const name = path[i];
            if (objectNode.type != "Object")
                return { entity: astRange, value: null, lastProperty: null, lastPath: null };
            const children = objectNode.children;
            for (const child of children) {
                if (child.key.value == name) {
                    foundChild = child;
                    lastPath.push(name);
                    if (child.value.type == "Object") {
                        objectNode = child.value;
                    }
                    break;
                }
            }
            const lastChild = children[children.length - 1];
            lastRange = EntityEditor.RangeFromLoc(lastChild.loc, 0);
            if (!foundChild)
                break;
        }
        if (foundChild) {
            const valueRange = EntityEditor.RangeFromLoc(foundChild.value.loc, 0);
            return { entity: entityRange, value: valueRange, lastProperty: lastRange, lastPath };
        }
        return { entity: entityRange, value: null, lastProperty: lastRange, lastPath };
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
        const url = `message-param://${database}.${command}.json`;
        const isNewModel = this.entityModels[url] == undefined;
        const model = this.getModel(url);
        if (isNewModel) {
            model.setValue(value);
        }
        this.commandValueEditor.setModel(model);
    }
    setCommandResult(database, command) {
        const url = `message-result://${database}.${command}.json`;
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
        app.layoutEditors();
    }
    showCommand(database, command, type) {
        var _a;
        this.setExplorerEditor("command");
        const schema = (_a = app.databaseSchemas[database]) === null || _a === void 0 ? void 0 : _a._rootSchema;
        const messages = type == "cmd" ? schema === null || schema === void 0 ? void 0 : schema.commands : schema === null || schema === void 0 ? void 0 : schema.messages;
        const signature = messages ? messages[command] : null;
        const defaultParam = EntityEditor.getDefaultValue(signature === null || signature === void 0 ? void 0 : signature.param);
        this.entityIdentity = {
            database: database,
            container: null,
            entityIds: null,
            command: command,
            msgType: type
        };
        this.setCommandParam(database, command, defaultParam); // sets command param => must be called before getCommandUrl()
        this.setCommandResult(database, command);
        commandSignature.innerHTML = this.getCommandDocsEl(database, command, signature);
        //  commandAnchor.innerHTML     = `GET <span style="opacity:0.5">${database}?cmd=</span>${command}`;
        commandAnchor.innerHTML = command; // `GET &nbsp;&nbsp;${command}`;
        commandAnchor.href = this.getCommandUrl(database, command, type);
        commandAnchor.onfocus = () => {
            commandAnchor.href = this.getCommandUrl(database, command, type);
        };
        const docs = signature === null || signature === void 0 ? void 0 : signature.description;
        commandDocs.innerHTML = docs ? docs : "";
    }
    static getDefaultValue(fieldType) {
        if (!fieldType)
            return 'null';
        const type = Schema.getFieldType(fieldType);
        if (type.isNullable)
            return 'null';
        fieldType = type.type;
        const resolvedDef = fieldType._resolvedDef;
        const paramType = resolvedDef ? resolvedDef.type : fieldType.type;
        switch (paramType) {
            case "object": return '{}';
            case "array": return '[]';
            case "string": return 'null';
            case "number": return '0';
            case "boolean": return 'false';
            default: return 'null';
        }
    }
    getCommandUrl(database, command, type) {
        let param = this.commandValueEditor.getValue();
        try {
            const valueStr = JSON.parse(param);
            param = JSON.stringify(valueStr); // format to one line / remove white spaces
        }
        catch (_a) {
            // use unformatted invalid value instead
        }
        const commandParam = param == "null" ? "" : `&param=${param}`;
        return `./rest/${database}?${type}=${command}${commandParam}`;
    }
    getCommandDocsEl(database, command, signature) {
        if (!signature)
            return app.schemaLess;
        const category = signature.result ? "commands" : "messages";
        const param = EntityEditor.getMessageArg("param", database, signature.param);
        const returnHtml = EntityEditor.getReturnType(database, signature.result);
        const commandEl = app.getSchemaCommand(database, category, command);
        const apiLinks = App.getApiLinks(database, "open commands API", `#/commands/post__cmd_${command}`);
        const el = `<span title="command parameter type">
            ${commandEl}
            (${param})
        </span>
        ${returnHtml}&nbsp;&nbsp;${apiLinks}`;
        return el;
    }
    static getReturnType(database, returnType) {
        if (!returnType)
            return "&nbsp;: void";
        const resultType = app.getTypeLabel(database, returnType);
        return `<span style="opacity: 0.5;">&nbsp;:&nbsp;</span><span title="command result type">${resultType}</span>`;
    }
    static getMessageArg(name, database, fieldType) {
        if (!fieldType)
            return "";
        const type = app.getTypeLabel(database, fieldType);
        return `<span style="opacity: 0.5;">${name}: </span><span>${type}</span>`;
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
EntityEditor.testBulkGet = false; // alternative to read entities by id. Will not work properly if entity ids contain commas
//# sourceMappingURL=entity-editor.js.map