// --------------------------------------- WebSocket ---------------------------------------
var connection;
var websocketCount = 0;
var req = 1;
var clt = null;
var requestStart;
var subSeq   = 0;
var subCount = 0;

const responseState     = document.getElementById("response-state");
const subscriptionCount = document.getElementById("subscriptionCount");
const subscriptionSeq   = document.getElementById("subscriptionSeq");
const selectExample     = document.getElementById("example");
const socketStatus      = document.getElementById("socketStatus");
const reqIdElement      = document.getElementById("req");
const ackElement        = document.getElementById("ack");
const cltElement        = document.getElementById("clt");
const defaultUser       = document.getElementById("user");
const defaultToken      = document.getElementById("token");
const catalogExplorer   = document.getElementById("catalogExplorer");
const entityExplorer    = document.getElementById("entityExplorer");
const writeResult       = document.getElementById("writeResult");
const readEntitiesDB    = document.getElementById("readEntitiesDB");
const readEntities      = document.getElementById("readEntities");
const catalogSchema     = document.getElementById("catalogSchema");
const entityType        = document.getElementById("entityType");
const entityId          = document.getElementById("entityId");

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
        var loc     = window.location;
        var nr      = ("" + (++websocketCount)).padStart(3,0)
        var uri     = `ws://${loc.host}/ws-${nr}`;
        // var uri  = `ws://google.com:8080/`; // test connection timeout
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
            var duration = new Date().getTime() - requestStart;
            var data = JSON.parse(e.data);
            // console.log('server:', e.data);
            switch (data.msg) {
            case "resp":
            case "error":
                clt = data.clt;
                cltElement.innerText  = clt ?? " - ";
                const content = this.formatJson(this.formatResponses, e.data);
                this.responseModel.setValue(content)
                responseState.innerHTML = `· ${duration} ms`;
                break;
            case "ev":
                subscriptionCount.innerText = ++subCount;
                subSeq = data.seq;
                // multiple clients can use the same WebSocket. Use the latest
                if (clt == data.clt) {
                    subscriptionSeq.innerText   = subSeq ? subSeq : " - ";
                    ackElement.innerText        = subSeq ? subSeq : " - ";
                }
                break;
            }
        };
    }

    closeWebsocket  () {
        connection.close();
    }

    getCookie  (name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
    }

    initUserToken  () {
        var user    = this.getCookie("fliox-user")   ?? "admin";
        var token   = this.getCookie("fliox-token")  ?? "admin";
        this.setUser(user);
        this.setToken(token);
    }

    setUser (user) {
        defaultUser.value   = user;
        document.cookie = `fliox-user=${user};`;
    }

    setToken  (token) {
        defaultToken.value  = token;
        document.cookie = `fliox-token=${token};`;
    }

    selectUser (element) {
        let value = element.innerText;
        this.setUser(value);
        this.setToken(value);
    };

    addUserToken (jsonRequest) {
        var endBracket  = jsonRequest.lastIndexOf("}");
        if (endBracket == -1)
            return jsonRequest;
        var before      = jsonRequest.substring(0, endBracket);
        var after       = jsonRequest.substring(endBracket);
        var userToken   = JSON.stringify({ user: defaultUser.value, token: defaultToken.value});
        userToken       = userToken.substring(1, userToken.length - 1);
        return `${before},${userToken}${after}`;
    }

    sendSyncRequest () {
        if (!connection || connection.readyState != 1) { // 1 == OPEN {
            this.responseModel.setValue(`Request ${req} failed. WebSocket not connected`)
            responseState.innerHTML = "";
        } else {
            var jsonRequest = this.requestModel.getValue();
            jsonRequest = this.addUserToken(jsonRequest);
            try {
                var request     = JSON.parse(jsonRequest);
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
        reqIdElement.innerText  = req;
    }

    async postSyncRequest () {
        var jsonRequest         = this.requestModel.getValue();
        jsonRequest             = this.addUserToken(jsonRequest);
        responseState.innerHTML = '<span class="spinner"></span>';
        let start = new Date().getTime();
        var duration;
        try {
            const response = await this.postRequest(jsonRequest, "POST");
            let content = await response.text;
            content = this.formatJson(this.formatResponses, content);
            duration = new Date().getTime() - start;
            this.responseModel.setValue(content);
        } catch(error) {
            duration = new Date().getTime() - start;
            this.responseModel.setValue("POST error: " + error.message);
        }
        responseState.innerHTML = `· ${duration} ms`;
    }

    lastCtrlKey;
    refLinkDecoration;

    applyCtrlKey(event) {
        if (this.lastCtrlKey == event.ctrlKey)
            return;
        this.lastCtrlKey = event.ctrlKey;
        if (!this.refLinkDecoration) {
            const cssRules = document.styleSheets[0].cssRules;
            for (let n = 0; n < cssRules.length; n++) {
                if (cssRules[n].selectorText == ".refLinkDecoration:hover")
                    this.refLinkDecoration = cssRules[n];
            }
        }
        this.refLinkDecoration.style.cursor = this.lastCtrlKey ? "pointer" : "";
    }

    onKeyUp (event) {
        if (event.code == "ControlLeft")
            this.applyCtrlKey(event);
    }

    onKeyDown (event) {
        if (event.code == "ControlLeft")
            this.applyCtrlKey(event);

        switch (this.activeTab) {
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
                        this.execute(event, () => this.saveEntity());
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

    execute(event, lambda) {
        lambda();
        event.preventDefault();
    }

    // --------------------------------------- example requests ---------------------------------------
    async onExampleChange () {
        var exampleName = selectExample.value;
        if (exampleName == "") {
            this.requestModel.setValue("")
            return;
        }
        var response = await fetch(exampleName);
        var example = await response.text();
        this.requestModel.setValue(example)
    }

    async loadExampleRequestList () {
        // [html - How do I make a placeholder for a 'select' box? - Stack Overflow] https://stackoverflow.com/questions/5805059/how-do-i-make-a-placeholder-for-a-select-box
        var option = document.createElement("option");
        option.value    = "";
        option.disabled = true;
        option.selected = true;
        option.hidden   = true;
        option.text = "Select request ...";
        selectExample.add(option);

        var folder = './example-requests'
        var response = await fetch(folder);
        var exampleRequests = await response.json();
        var groupPrefix = "0";
        var groupCount  = 0;
        for (var example of exampleRequests) {
            if (!example.endsWith(".json"))
                continue;
            var name = example.substring(folder.length).replace(".sync.json", "");
            if (groupPrefix != name[0]) {
                groupPrefix = name[0];
                groupCount++;
            }
            option = document.createElement("option");
            option.value    = example;
            option.text     = (groupCount % 2 ? "\xA0\xA0" : "") + name;
            option.style    = groupCount % 2 ? "background-color: #ffffff;" : "background-color: #eeeeff;"
            selectExample.add(option);
        }
    }
    // --------------------------------------- Explorer ---------------------------------------
  

    async postRequest (request, tag) {
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

    async postRequestTasks (database, tasks, tag) {
        const db = database == "main_db" ? undefined : database;
        const request = JSON.stringify({
            "msg":      "sync",
            "database": db,
            "tasks":    tasks,
            "user":     defaultUser.value,
            "token":    defaultToken.value
        });
        tag = tag ? tag : "";
        return await this.postRequest(request, `${database}/${tag}`);
    }

    getRestPath(database, container, id, query) {
        let path = `./rest/${database}`;
        if (container)  path = `${path}/${container}`;
        if (id)         path = `${path}/${id}`;
        if (query)      path = `${path}?${query}`;
        return path;
    }

    async restRequest (method, body, database, container, id, query) {
        const path = this.getRestPath(database, container, id, query);        
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
                text:   () => error.message
            }
        }
    }

    getTaskError (content, taskIndex) {
        if (content.msg == "error") {
            return content.message;
        }
        var task = content.tasks[taskIndex];
        if (task.task == "error")
            return "task error:\n" + task.message;
        return undefined;
    }

    bracketValue = /\[(.*?)\]/;

    errorAsHtml (message, p) {
        // first line: error type, second line: error message
        const pos = message.indexOf(' > ');
        let error = message;
        if (pos > 0) {
            let reason = message.substring(pos + 3);
            if (reason.startsWith("at ")) {
                const id = reason.match(this.bracketValue)[1];
                if (p && id) {
                    const coordinate = JSON.stringify({ database: p.database, container: p.container, id: id });
                    const link = `<a  href="#" onclick='app.loadEntity(${coordinate})'>${id}</a>`;
                    reason = reason.replace(id, link);
                }
                reason = reason.replace("] ", "]<br>");
            }
            error =  message.substring(0, pos) + " ><br>" + reason;
        }
        return `<code style="white-space: pre-line; color:red">${error}</code>`;
    }

    setClass(element, enable, className) {
        const classList = element.classList;
        if (enable) {
            classList.add(className);
            return;
        }
        classList.remove(className);        
    }

    toggleDescription() {
        this.changeConfig("showDescription", !this.showDescription);   
        this.openTab(this.activeTab);
    }

    openTab (tabName) {
        this.activeTab = tabName;
        this.setClass(document.body, !this.showDescription, "miniHeader")
        var tabContents = document.getElementsByClassName("tabContent");
        var tabs = document.getElementsByClassName("tab");
        const gridTemplateRows = document.body.style.gridTemplateRows.split(" ");
        const headerHeight = getComputedStyle(document.body).getPropertyValue('--header-height');
        gridTemplateRows[0] = this.showDescription ? headerHeight : "0";
        for (var i = 0; i < tabContents.length; i++) {
            const tabContent = tabContents[i]
            tabContent.style.display = tabContent.id == tabName ? "grid" : "none";
            const isActiveTab = tabContent.id == tabName;
            this.setClass(tabs[i], isActiveTab, "selected");
            gridTemplateRows[i + 2] = isActiveTab ? "1fr" : "0"; // + 2  ->  "body-header" & "body-tabs"
        }
        document.body.style.gridTemplateRows = gridTemplateRows.join(" ");
        this.layoutEditors();
        if (tabName != "settings") {
            this.setConfig("activeTab", tabName);
        }
    }

    selectedCatalog;
    selectedEntity = {
        elem: null
    }

    setSelectedEntity(elem) {
        if (this.selectedEntity.elem) {
            this.selectedEntity.elem.classList.remove("selected");
        }
        this.selectedEntity.elem = elem;
        this.selectedEntity.elem.classList.add("selected");
    }

    async loadCluster () {
        const tasks = [
            { "task": "query", "container": "containers", "filterJson":{ "op": "true" }},
            { "task": "query", "container": "schemas",    "filterJson":{ "op": "true" }},
            { "task": "query", "container": "commands",   "filterJson":{ "op": "true" }}
        ];
        catalogExplorer.innerHTML = 'read databases <span class="spinner"></span>';
        const response = await this.postRequestTasks("cluster", tasks);
        const content = response.json;
        var error = this.getTaskError (content, 0);
        if (error) {
            catalogExplorer.innerHTML = this.errorAsHtml(error);
            return 
        }
        const dbContainers  = content.containers[0].entities;
        const dbSchemas       = content.containers[1].entities;
        const commands      = content.containers[2].entities;
        var ulCatalogs = document.createElement('ul');
        ulCatalogs.onclick = (ev) => {
            var path = ev.composedPath();
            const selectedElement = path[0];
            if (selectedElement.classList.contains("caret")) {
                path[2].classList.toggle("active");
                return;
            }
            path[1].classList.add("active");
            if (this.selectedCatalog) this.selectedCatalog.classList.remove("selected");
            this.selectedCatalog =selectedElement;
            selectedElement.classList.add("selected");
            const databaseName = selectedElement.childNodes[1].innerText;
            var dbCommands  = commands.find  (c => c.id == databaseName);
            var dbContainer = dbContainers.find  (c => c.id == databaseName);
            catalogSchema.innerHTML  = this.getSchemaType(databaseName)
            this.listCommands(databaseName, dbCommands, dbContainer);
            // var style = path[1].childNodes[1].style;
            // style.display = style.display == "none" ? "" : "none";
        }
        let firstDatabase = true;
        for (var dbContainer of dbContainers) {
            var liCatalog       = document.createElement('li');
            if (firstDatabase) {
                firstDatabase = false;
                liCatalog.classList.add("active");
            }
            var liDatabase          = document.createElement('div');
            var catalogCaret        = document.createElement('div');
            catalogCaret.classList  = "caret";
            var catalogLabel        = document.createElement('span');
            catalogLabel.innerText  = dbContainer.id;
            liDatabase.title        = "database";
            catalogLabel.style = "pointer-events: none;"
            liDatabase.append(catalogCaret)
            liDatabase.append(catalogLabel)
            liCatalog.appendChild(liDatabase);
            ulCatalogs.append(liCatalog);

            var ulContainers = document.createElement('ul');
            ulContainers.onclick = (ev) => {
                ev.stopPropagation();
                var path = ev.composedPath();
                const selectedElement = path[0];
                // in case of a multiline text selection selectedElement is the parent
                if (selectedElement.tagName.toLowerCase() != "div")
                    return;
                if (this.selectedCatalog) this.selectedCatalog.classList.remove("selected");
                this.selectedCatalog = selectedElement;
                this.selectedCatalog.classList.add("selected");
                const containerName = this.selectedCatalog.innerText.trim();
                const databaseName  = path[3].childNodes[0].childNodes[1].innerText;
                var params = { database: databaseName, container: containerName };
                this.clearEntity(databaseName, containerName);
                this.loadEntities(params);
            }
            liCatalog.append(ulContainers);
            for (const containerName of dbContainer.containers) {
                var liContainer     = document.createElement('li');
                liContainer.title   = "container";
                var containerLabel  = document.createElement('div');
                containerLabel.innerHTML = "&nbsp;" + containerName;
                liContainer.append(containerLabel)
                ulContainers.append(liContainer);
            }
        }
        this.createEntitySchemas(dbSchemas)
        catalogExplorer.textContent = "";
        catalogExplorer.appendChild(ulCatalogs);
    }

    databaseSchemas = {};

    createEntitySchemas (dbSchemas) {
        var schemaMap = {};
        for (var dbSchema of dbSchemas) {
            var jsonSchemas     = dbSchema.jsonSchemas;
            var database        = dbSchema.id;
            const containerRefs = {};
            const rootSchema    = jsonSchemas[dbSchema.schemaPath].definitions[dbSchema.schemaName];
            const containers    = rootSchema.properties;
            for (const containerName in containers) {
                const container = containers[containerName];
                containerRefs[container.additionalProperties.$ref] = containerName;
            }
            this.databaseSchemas[database] = dbSchema;
            dbSchema.containerSchemas = {}

            // add all schemas and their definitions to schemaMap and map them to an uri like:
            //   http://main_db/Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json
            //   http://main_db/Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json#/definitions/PocStore
            for (var schemaPath in jsonSchemas) {
                var schema      = jsonSchemas[schemaPath];
                var uri         = "http://" + database + "/" + schemaPath;
                var schemaEntry = {
                    uri:            uri,
                    schema:         schema,
                    fileMatch:      [], // can have multiple in case schema is used by multiple editor models
                    _resolvedDef:   schema // not part of monaco > DiagnosticsOptions.schemas
                }
                const namespace     = schemaPath.substring(0, schemaPath.length - ".json".length);
                schemaMap[uri]      = schemaEntry;
                const definitions   = schema.definitions;
                const baseRefType   = schema.$ref ? schema.$ref.substring('#/definitions/'.length) : undefined;
                for (var definitionName in definitions) {
                    const definition        = definitions[definitionName];
                    definition._typeName    = definitionName;
                    definition._namespace   = namespace;
                    if (definitionName == baseRefType) {
                        definition._namespace = namespace.substring(0, namespace.length - definitionName.length - 1);
                    }
                    // console.log("---", definition._namespace, definitionName);
                    var path                = "/" + schemaPath + "#/definitions/" + definitionName;
                    var schemaId            = "." + path
                    var uri                 = "http://" + database + path;
                    var containerName       = containerRefs[schemaId]
                    if (containerName) {
                        dbSchema.containerSchemas[containerName] = definition;
                    }
                    // add reference for definitionName pointing to definition in current schemaPath
                    var definitionEntry = {
                        uri:            uri,
                        schema:         { $ref: schemaId },
                        fileMatch:      [], // can have multiple in case schema is used by multiple editor models
                        _resolvedDef:   definition // not part of monaco > DiagnosticsOptions.schemas
                    }
                    schemaMap[uri] = definitionEntry;
                }
            }
            this.resolveRefs(jsonSchemas);
            this.addFileMatcher(database, dbSchema, schemaMap);
        }
        var monacoSchemas = Object.values(schemaMap);
        this.addSchemas(monacoSchemas);
    }

    resolveRefs(jsonSchemas) {
        for (const schemaPath in jsonSchemas) {
            // if (schemaPath == "Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Order.json") debugger;
            const schema      = jsonSchemas[schemaPath];
            this.resolveNodeRefs(jsonSchemas, schema, schema);
        }
    }

    resolveNodeRefs(jsonSchemas, schema, node) {
        const nodeType = typeof node;
        switch (nodeType) {
        case "array":
            console.log("array"); // todo remove
            return;
        case "object":
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
                // if (propertyName == "employees") debugger;
                const property = node[propertyName];
                this.resolveNodeRefs(jsonSchemas, schema, property);
            }
            return;
        }
    }

    // add a "fileMatch" property to all container entity type schemas used for editor validation
    addFileMatcher(database, dbSchema, schemaMap) {
        var jsonSchemas     = dbSchema.jsonSchemas;
        var schemaName      = dbSchema.schemaName;
        var schemaPath      = dbSchema.schemaPath;
        var dbSchema        = jsonSchemas[schemaPath];
        var dbType          = dbSchema.definitions[schemaName];
        var containers      = dbType.properties;
        for (var containerName in containers) {
            const container     = containers[containerName];
            var containerType   = this.getResolvedType(container.additionalProperties, schemaPath);
            var uri = "http://" + database + containerType.$ref.substring(1);
            const schema = schemaMap[uri];
            var url = `entity://${database}.${containerName.toLocaleLowerCase()}.json`;
            schema.fileMatch.push(url); // requires a lower case string
        }
        var commandType     = dbSchema.definitions[schemaName];
        var commands        = commandType.commands;
        for (var commandName in commands) {
            const command   = commands[commandName];
            // assign file matcher for command param
            var paramType   = this.getResolvedType(command.param, schemaPath);
            var url = `command-param://${database}.${commandName.toLocaleLowerCase()}.json`;
            if (paramType.$ref) {
                var uri = "http://" + database + paramType.$ref.substring(1);
                const schema = schemaMap[uri];
                schema.fileMatch.push(url); // requires a lower case string
            } else {
                // uri is never referenced - create an arbitrary unique uri
                var uri = "http://" + database + "/command/param" + commandName;
                const schema = {
                    schema:     paramType,
                    fileMatch:  [url]
                };
                schemaMap[uri] = schema;
            }
            // assign file matcher for command result
            var resultType   = this.getResolvedType(command.result, schemaPath);
            var url = `command-result://${database}.${commandName.toLocaleLowerCase()}.json`;
            if (resultType.$ref) {
                var uri = "http://" + database + resultType.$ref.substring(1);
                const schema = schemaMap[uri];
                schema.fileMatch.push(url); // requires a lower case string
            } else {
                // uri is never referenced - create an arbitrary unique uri
                var uri = "http://" + database + "/command/result" + commandName;
                const schema = {
                    schema:     resultType,
                    fileMatch: [url]
                };
                schemaMap[uri] = schema;
            }
        }
    }

    getResolvedType (type, schemaPath) {
        var $ref = type.$ref;
        if (!$ref)
            return type;
        if ($ref[0] != "#")
            return type;
        return { $ref: "./" + schemaPath + $ref };
    }

    
    getSchemaType(database) {
        const schema        = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;
        return `<a title="open database schema in new tab" href="./schema/${database}/html/schema.html" target="${database}">${schema.schemaName}</a>`;
    }

    getType(database, def) {
        var ns          = def._namespace;
        var name        = def._typeName;
        return `<a title="open type definition in new tab" href="./schema/${database}/html/schema.html#${ns}.${name}" target="${database}">${name}</a>`;
    }

    getEntityType(database, container) {
        const schema    = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;        
        var dbSchema    = schema.jsonSchemas[schema.schemaPath].definitions[schema.schemaName];
        var def         = dbSchema.properties[container].additionalProperties._resolvedDef;
        return this.getType(database, def);
    }

    getTypeLabel(database, type) {
        if (type.type) {
            return type.type;
        }
        const def = type._resolvedDef;
        if (def) {
            return this.getType(database, def);
        }        
        var result = JSON.stringify(type);
        return result = result == "{}" ? "any" : result;
    }

    schemaLess = '<span title="missing type definition - schema-less database" style="opacity:0.5">unknown</span>';

    setEditorHeader(show) {
        var displayEntity  = show == "entity" ? "" : "none";
        var displayCommand = show == "command" ? "" : "none";
        document.getElementById("entityTools")  .style.display = displayEntity;        
        document.getElementById("entityHeader") .style.display = displayEntity;        
        document.getElementById("commandTools") .style.display = displayCommand;
        document.getElementById("commandHeader").style.display = displayCommand;
    }

    getCommandTags(database, command, signature) {
        let label = this.schemaLess;
        if (signature) {
            const param   = this.getTypeLabel(database, signature.param);
            const result  = this.getTypeLabel(database, signature.result);
            label = `<span title="command parameter type"><span style="opacity: 0.5;">(param:</span> <span>${param}</span></span><span style="opacity: 0.5;">) : </span><span title="command result type">${result}</span>`
        }
        var link    = `command=${command}`;
        var url     = `./rest/${database}?command=${command}`;
        return {
            link:   `<a id="commandAnchor" title="command" onclick="app.sendCommand()" href="${url}" target="_blank" rel="noopener noreferrer">${link}</a>`,
            label:  label
        }
    }

    async sendCommand(method) {
        const value     = this.commandValueEditor.getValue();
        const database  = this.entityIdentity.database;
        const command   = this.entityIdentity.command;
        if (!method) {
            const commandAnchor =  document.getElementById("commandAnchor");
            let commandValue = value == "null" ? "" : `&value=${value}`;
            const path = this.getRestPath( database, null, null, `command=${command}${commandValue}`)
            commandAnchor.href = path;
            // window.open(path, '_blank');
            return;
        }
        const response = await this.restRequest(method, value, database, null, null, `command=${command}`);
        let content = await response.text();
        content = this.formatJson(this.formatResponses, content);
        this.entityEditor.setValue(content);
    }

    listCommands (database, dbCommands, dbContainer) {
        filterRow.style.visibility  = "hidden";
        entityFilter.style.visibility  = "hidden";
        readEntitiesDB.innerHTML    = `<a title="database" href="./rest/${database}" target="_blank" rel="noopener noreferrer">${database}</a>`;
        readEntities.innerHTML      = "";

        var ulDatabase  = document.createElement('ul');
        ulDatabase.classList = "database"
        var typeLabel = document.createElement('div');
        typeLabel.innerHTML = `<small style="opacity:0.5">type: ${dbContainer.databaseType}</small>`;
        ulDatabase.append(typeLabel)
        var commandLabel = document.createElement('div');
        const label = '<small style="opacity:0.5" title="open database commands in new tab">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;commands</small>';
        commandLabel.innerHTML = `<a href="./rest/${database}?command=DbCommands" target="_blank" rel="noopener noreferrer">${label}</a>`;
        ulDatabase.append(commandLabel)

        var liCommands  = document.createElement('li');
        ulDatabase.appendChild(liCommands);

        var ulCommands = document.createElement('ul');
        ulCommands.onclick = (ev) => {
            this.setEditorHeader("command");
            var path = ev.composedPath();
            let selectedElement = path[0];
            // in case of a multiline text selection selectedElement is the parent

            const tagName = selectedElement.tagName;
            if (tagName == "SPAN" || tagName == "DIV") {
                selectedElement = path[1];
            }
            const commandName = selectedElement.children[0].innerText;
            this.setSelectedEntity(selectedElement);

            this.showCommand(database, commandName);

            if (path[0].classList.contains("command")) {
                this.sendCommand("POST");
            }
        }
        for (const command of dbCommands.commands) {
            var liCommand = document.createElement('li');
            var commandLabel = document.createElement('div');
            commandLabel.innerText = command;
            liCommand.appendChild(commandLabel);
            var runCommand = document.createElement('div');
            runCommand.classList    = "command";
            runCommand.title        = "POST command"
            liCommand.appendChild(runCommand);

            ulCommands.append(liCommand);
        }
        entityExplorer.innerText = ""
        liCommands.append(ulCommands);
        entityExplorer.appendChild(ulDatabase);
    }

    filter = {
        database:   undefined,
        container:  undefined
    }

    filterOnKeyUp(event) {
        if (event.code != 'Enter')
            return;
        this.applyFilter(this.filter.database, this.filter.container, entityFilter.value);
    }

    applyFilter() {
        const database  = this.filter.database;
        const container = this.filter.container;
        const filter    = entityFilter.value;
        const query   = filter.trim() == "" ? null : `filter=${encodeURIComponent(filter)}`;
        const params  = { database: database, container: container };
        this.saveFilter(database, container, filter)
        this.loadEntities(params, query);
    }

    removeFilter() {
        const params  = { database: this.filter.database, container: this.filter.container };
        this.loadEntities(params);
    }

    saveFilter(database, container, filter) {
        if (filter.trim() == "") {
            const filterDatabase = this.filters[database];
            if (filterDatabase) {
                delete filterDatabase[container];
            }
        } else {
            if (!this.filters[database]) this.filters[database] = {}            
            this.filters[database][container] = [filter];
        }
        this.setConfig("filters", this.filters);
    }

    updateFilterLink() {
        var filter  = entityFilter.value;
        var query   = filter.trim() == "" ? "" : `?filter=${encodeURIComponent(filter)}`;
        const url = `./rest/${this.filter.database}/${this.filter.container}${query}`;
        filterLink.href = url;
    }

    async loadEntities (p, query) {
        // if (p.clearSelection) this.setEditorHeader();
        const storedFilter = this.filters[p.database]?.[p.container];
        const filter = storedFilter && storedFilter[0] ? storedFilter[0] : "";        
        entityFilter.value = filter;

        const removeFilterVisibility = query ? "" : "hidden";
        removeFilter.style.visibility = removeFilterVisibility;
        
        this.filter.database     = p.database;
        this.filter.container    = p.container;
        
        // const tasks =  [{ "task": "query", "container": p.container, "filterJson":{ "op": "true" }}];
        filterRow.style.visibility   = "";
        entityFilter.style.visibility  = "";
        catalogSchema.innerHTML  = this.getSchemaType(p.database) + ' · ' + this.getEntityType(p.database, p.container);
        readEntitiesDB.innerHTML = `<a title="open database in new tab" href="./rest/${p.database}" target="_blank" rel="noopener noreferrer">${p.database}/</a>`;
        const containerLink      = `<a title="open container in new tab" href="./rest/${p.database}/${p.container}" target="_blank" rel="noopener noreferrer">${p.container}/</a>`;
        readEntities.innerHTML   = `${containerLink}<span class="spinner"></span>`;

        const response           = await this.restRequest("GET", null, p.database, p.container, null, query);

        const reload = `<span class="reload" title='reload container' onclick='app.loadEntities(${JSON.stringify(p)})'></span>`
        writeResult.innerHTML   = "";        
        readEntities.innerHTML  = containerLink + reload;
        if (!response.ok) {
            const error = await response.text();
            entityExplorer.innerHTML = this.errorAsHtml(error, p);
            return;
        }
        let     content = await response.json();
        const   ids     = content.map(entity => entity.id);
        const   ulIds   = document.createElement('ul');
        ulIds.classList = "entities"
        ulIds.onclick = (ev) => {
            const path = ev.composedPath();
            const selectedElement = path[0];
            // in case of a multiline text selection selectedElement is the parent
            if (selectedElement.tagName.toLowerCase() != "li")
                return;
            this.setSelectedEntity(selectedElement);
            const entityId = selectedElement.innerText;
            const params = { database: p.database, container: p.container, id: entityId };
            this.loadEntity(params);
        }
        for (var id of ids) {
            var liId = document.createElement('li');
            liId.innerText = id;
            ulIds.append(liId);
        }
        entityExplorer.innerText = ""
        entityExplorer.appendChild(ulIds);
    }

    entityIdentity = {
        database:   undefined,
        container:  undefined,
        entityId:   undefined,
        command:    undefined
    }

    entityHistoryPos    = -1;
    entityHistory       = [];

    storeCursor() {
        if (this.entityHistoryPos < 0)
            return;
        this.entityHistory[this.entityHistoryPos].selection    = this.entityEditor.getSelection();
    }

    navigateEntity(pos) {
        if (pos < 0 || pos >= this.entityHistory.length)
            return;
        this.storeCursor();
        this.entityHistoryPos = pos;
        const entry = this.entityHistory[pos]
        this.loadEntity(entry.route, true, entry.selection);
    }

    async loadEntity (p, preserveHistory, selection) {
        this.explorerEditCommandVisible(false);
        this.layoutEditors();

        if (!preserveHistory) {
            this.storeCursor();
            this.entityHistory[++this.entityHistoryPos] = { route: {...p} };
            this.entityHistory.length = this.entityHistoryPos + 1;
        }
        this.entityIdentity = {
            database:   p.database,
            container:  p.container,
            entityId:   p.id
        };
        this.setEditorHeader("entity");
        entityType.innerHTML    = this.getEntityType (p.database, p.container);
        const entityLink        = this.getEntityLink(p.database, p.container, p.id);
        entityId.innerHTML      = `${entityLink}<span class="spinner"></span>`;
        writeResult.innerHTML   = "";
        const response  = await this.restRequest("GET", null, p.database, p.container, p.id);        
        let content   = await response.text();
        content = this.formatJson(this.formatEntities, content);
        entityId.innerHTML = entityLink + this.getEntityReload(p.database, p.container, p.id);
        if (!response.ok) {
            this.setEntityValue(p.database, p.container, content);
            return;
        }
        // console.log(entityJson);
        this.setEntityValue(p.database, p.container, content);
        if (selection)  this.entityEditor.setSelection(selection);        
        // this.entityEditor.focus(); // not useful - annoying: open soft keyboard on phone
    }

    getEntityLink (database, container, id) {
        const containerRoute    = { database: database, container: container }
        let link = `<a href="#" style="opacity:0.7; margin-right:20px;" onclick='app.loadEntities(${JSON.stringify(containerRoute)})'>« ${container}</a>`;
        if (id) {
            link += `<a title="open entity in new tab" href="./rest/${database}/${container}/${id}" target="_blank" rel="noopener noreferrer">${id}</a>`;
        }
        return link;
    }

    getEntityReload (database, container, id) {
        const p = { database, container, id };
        return `<span class="reload" title='reload entity' onclick='app.loadEntity(${JSON.stringify(p)}, true)'></span>`
    }

    clearEntity (database, container) {
        this.explorerEditCommandVisible(false);
        this.layoutEditors();

        this.entityIdentity = {
            database:   database,
            container:  container,
            entityId:   undefined
        };
        this.setEditorHeader("entity");
        entityType.innerHTML    = this.getEntityType (database, container);
        writeResult.innerHTML   = "";
        entityId.innerHTML      = this.getEntityLink(database, container);
        this.setEntityValue(database, container, "");
    }

    async saveEntity () {
        const database  = this.entityIdentity.database;
        const container = this.entityIdentity.container;
        const jsonValue = this.entityModel.getValue();
        let id;
        try {
            var keyName = "id"; // could be different. keyName can be retrieved from schema
            id = JSON.parse(jsonValue)[keyName];
        } catch (error) {
            writeResult.innerHTML = `<span style="color:red">Save failed: ${error}</code>`;
            return;
        }
        writeResult.innerHTML = 'save <span class="spinner"></span>';

        const response = await this.restRequest("PUT", jsonValue, database, container, id);
        if (!response.ok) {
            const error = await response.text();
            writeResult.innerHTML = `<span style="color:red">Save failed: ${error}</code>`;
            return;
        }
        writeResult.innerHTML = "Save successful";
        // add as HTML element to entityExplorer if new
        if (this.entityIdentity.entityId != id) {
            this.entityIdentity.entityId = id;
            const entityLink    = this.getEntityLink(database, container, id);
            entityId.innerHTML  = entityLink + this.getEntityReload(database, container, id);
            var liId = document.createElement('li');
            liId.innerText = id;
            liId.classList = "selected";
            const ulIds= entityExplorer.querySelector("ul");
            ulIds.append(liId);
            this.setSelectedEntity(liId);
            liId.scrollIntoView();
            this.entityHistory[++this.entityHistoryPos] = { route: { database: database, container: container, id:id }};
            this.entityHistory.length = this.entityHistoryPos + 1;
        }
    }

    async deleteEntity () {
        const id        = this.entityIdentity.entityId;
        var container   = this.entityIdentity.container;
        var database    = this.entityIdentity.database;
        writeResult.innerHTML = 'delete <span class="spinner"></span>';
        const response = await this.restRequest("DELETE", null, database, container, id);
        if (!response.ok) {
            var error = await response.text();
            writeResult.innerHTML = `<span style="color:red">Delete failed: ${error}</code>`;
        } else {
            writeResult.innerHTML = "Delete successful";
            entityId.innerHTML = "";
            this.setEntityValue(database, container, "");
            var selected = entityExplorer.querySelector(`li.selected`);
            selected.remove();
        }
    }

    entityModel;
    entityModels = {};

    getModel (url) {
        this.entityModel = this.entityModels[url];
        if (!this.entityModel) {
            var entityUri   = monaco.Uri.parse(url);
            this.entityModel = monaco.editor.createModel(null, "json", entityUri);
            this.entityModels[url] = this.entityModel;
        }
        return this.entityModel;
    }

    setEntityValue (database, container, value) {
        var url = `entity://${database}.${container}.json`;
        var model = this.getModel(url);
        model.setValue(value);
        this.entityEditor.setModel (model);
        if (value == "")
            return;
        var databaseSchema = this.databaseSchemas[database];
        if (!databaseSchema)
            return;
        try {
            const containerSchema   = databaseSchema.containerSchemas[container];
            this.decorateJson(this.entityEditor, value, containerSchema, database);
        } catch (error) {
            console.error("decorateJson", error);
        }
    }

    decorateJson(editor, value, containerSchema, database) {
        JSON.parse(value);  // early out on invalid JSON
        // 1.) [json-to-ast - npm] https://www.npmjs.com/package/json-to-ast
        // 2.) bundle.js created fom npm module 'json-to-ast' via:
        //     [node.js - How to use npm modules in browser? is possible to use them even in local (PC) ? - javascript - Stack Overflow] https://stackoverflow.com/questions/49562978/how-to-use-npm-modules-in-browser-is-possible-to-use-them-even-in-local-pc
        // 3.) browserify main.js | uglifyjs > bundle.js
        //     [javascript - How to get minified output with browserify? - Stack Overflow] https://stackoverflow.com/questions/15590702/how-to-get-minified-output-with-browserify
        const ast = parse(value, { loc: true });
        // console.log ("AST", ast);
        
        // --- deltaDecorations() -> [ITextModel | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.ITextModel.html
        const oldDecorations = [];
        const newDecorations = [
            // { range: new monaco.Range(7, 13, 7, 22), options: { inlineClassName: 'refLinkDecoration' } }
        ];
        this.addRelationsFromAst(ast, containerSchema, (value, container) => {
            const start         = value.loc.start;
            const end           = value.loc.end;
            const range         = new monaco.Range(start.line, start.column, end.line, end.column);
            const markdownText  = `${database}/${container}  \nFollow: (ctrl + click)`;
            const hoverMessage  = [ { value: markdownText } ];
            newDecorations.push({ range: range, options: { inlineClassName: 'refLinkDecoration', hoverMessage: hoverMessage }});
        });
        var decorations = editor.deltaDecorations(oldDecorations, newDecorations);
    }

    addRelationsFromAst(ast, schema, addRelation) {
        if (!ast.children) // ast is a 'Literal'
            return;
        for (const child of ast.children) {
            switch (child.type) {
            case "Object":
                this.addRelationsFromAst(child, schema, addRelation);
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
                    var relation = property.relation;
                    if (relation && value.value !== null) {
                        addRelation (value, relation);
                    }
                    break;
                case "Object":
                    var resolvedDef = property._resolvedDef;
                    if (resolvedDef) {
                        this.addRelationsFromAst(value, resolvedDef, addRelation);
                    }
                    break;
                case "Array":
                    var resolvedDef = property.items?._resolvedDef;
                    if (resolvedDef) {
                        this.addRelationsFromAst(value, resolvedDef, addRelation);
                    }
                    var relation = property.relation;
                    if (relation) {
                        for (const item of value.children) {
                            if (item.type == "Literal") {
                                addRelation(item, relation);
                            }
                        }
                    }
                    break;
                }
                break;
            }
        }
    }

    setCommandParam (database, command, value) {
        var url = `command-param://${database}.${command}.json`;
        var isNewModel = this.entityModels[url] == undefined;
        var model = this.getModel(url)
        if (isNewModel) {
            model.setValue(value);
        }
        this.commandValueEditor.setModel (model);
    }

    setCommandResult (database, command) {
        var url = `command-result://${database}.${command}.json`;
        var model = this.getModel(url)
        this.entityEditor.setModel (model);
    }

    commandEditWidth = "60px";

    explorerEditCommandVisible(visible) {
        commandValueContainer.style.display = visible ? "" : "none";
        commandParamBar.style.display       = visible ? "" : "none";
        explorerEdit.style.gridTemplateRows = visible ? `${this.commandEditWidth} var(--vbar-width) 1fr` : "0 0 1fr";
    }

    showCommand(database, commandName) {
        this.explorerEditCommandVisible(true);

        this.layoutEditors();

        const schema        = this.databaseSchemas[database];
        const service       = schema ? schema.jsonSchemas[schema.schemaPath].definitions[schema.schemaName] : null;
        const signature     = service ? service.commands[commandName] : null
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
        var schemas = [];
        try {
            var jsonSchemaResponse  = await fetch("schema/protocol/json-schema.json");
            var jsonSchema          = await jsonSchemaResponse.json();

            for (let schemaName in jsonSchema) {
                var schema          = jsonSchema[schemaName];
                var url             = "protocol/json-schema/" + schemaName;
                var schemaEntry = {
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

    requestModel;
    responseModel;

    requestEditor;
    responseEditor;
    entityEditor;
    commandValueEditor;

    requestContainer        = document.getElementById("requestContainer");
    responseContainer       = document.getElementById("responseContainer")
    commandValueContainer   = document.getElementById("commandValueContainer");
    commandParamBar         = document.getElementById("commandParamBar");
    commandValue            = document.getElementById("commandValue");
    entityContainer         = document.getElementById("entityContainer");

    allMonacoSchemas = [];

    addSchemas (monacoSchemas) {
        this.allMonacoSchemas.push(...monacoSchemas);
        // [LanguageServiceDefaults | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.languages.json.LanguageServiceDefaults.html
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
            validate: true,
            schemas: this.allMonacoSchemas
        });
    }

    async setupEditors ()
    {
        this.explorerEditCommandVisible(false);
        
        // --- setup JSON Schema for monaco
        var requestUri      = monaco.Uri.parse("request://jsonRequest.json");   // a made up unique URI for our model
        var responseUri     = monaco.Uri.parse("request://jsonResponse.json");  // a made up unique URI for our model
        var monacoSchemas   = await this.createProtocolSchemas();

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
            this.requestEditor = monaco.editor.create(requestContainer, { /* model: model */ });
            this.requestModel = monaco.editor.createModel(null, "json", requestUri);
            this.requestEditor.setModel (this.requestModel);

            var defaultRequest = `{
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
            this.responseModel = monaco.editor.createModel(null, "json", responseUri);
            this.responseEditor.setModel (this.responseModel);
        }

        // --- create entity editor
        {
            this.entityEditor = monaco.editor.create(entityContainer, { });
            this.entityEditor.onMouseDown((e) => {
                if (!e.event.ctrlKey)
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
            this.commandValueEditor = monaco.editor.create(commandValue, { });
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
        const editorSettings = {
            lineNumbers:    this.showLineNumbers ? "on" : "off",
            minimap:        { enabled: this.showMinimap ? true : false },
            theme:          window.appConfig.monacoTheme,
            mouseWheelZoom: true
        }
        this.requestEditor.     updateOptions ({ ...editorSettings });
        this.responseEditor.    updateOptions ({ ...editorSettings });
        this.entityEditor.      updateOptions ({ ...editorSettings });
        this.commandValueEditor.updateOptions ({ ...editorSettings });
    }

    tryFollowLink(value, column, line) {
        try {
            JSON.parse(value);  // early out invalid JSON
            const ast               = parse(value, { loc: true });
            const database          = this.entityIdentity.database;
            const databaseSchema    = this.databaseSchemas[database];
            const containerSchema   = databaseSchema.containerSchemas[this.entityIdentity.container];

            let entity;
            this.addRelationsFromAst(ast, containerSchema, (value, container) => {
                if (entity)
                    return;
                const start = value.loc.start;
                const end   = value.loc.end;
                if (start.line <= line && start.column <= column && line <= end.line && column <= end.column) {
                    // console.log(`${resolvedDef.databaseName}/${resolvedDef.containerName}/${value.value}`);
                    entity = { database: database, container: container, id: value.value };
                }
            });
            if (entity) {
                this.loadEntity(entity);
            }
        } catch (error) {
            writeResult.innerHTML = `<span style="color:#FF8C00">Follow link failed: ${error}</code>`;
        }
    }

    setConfig(key, value) {
        this[key] = value;
        const elem = document.getElementById(key);
        if (elem) {
            elem.value   = value;
            elem.checked = value;
        }
        const valueStr = JSON.stringify(value, null, 2);
        window.localStorage.setItem(key, valueStr);
    }

    getConfig(key) {
        const valueStr = window.localStorage.getItem(key);
        try {
            return JSON.parse(valueStr);
        } catch(e) { }
        return undefined;
    }

    initConfigValue(key) {
        var value = this.getConfig(key);
        if (value == undefined) {
            this.setConfig(key, this[key]);
            return;
        }
        this.setConfig(key, value);
    }

    showLineNumbers = false;
    showMinimap     = false;
    formatEntities  = false;
    formatResponses = true;
    activeTab       = "playground";
    showDescription = true;
    filters         = {};

    loadConfig() {
        this.initConfigValue("showLineNumbers");
        this.initConfigValue("showMinimap");
        this.initConfigValue("formatEntities");
        this.initConfigValue("formatResponses");
        this.initConfigValue("activeTab");
        this.initConfigValue("showDescription");
        this.initConfigValue("filters");
    }

    changeConfig (key, value) {
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
                return JSON.stringify(obj, null, 4);
            }
            catch (error) {}            
        }
        return text;
    }

    layoutEditors () {
        // console.log("layoutEditors - activeTab: " + activeTab)
        switch (this.activeTab) {
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

    layoutMonacoEditors(pairs) {
        for (let n = pairs.length - 1; n >= 0; n--) {
            const pair = pairs[n];
            if (!pair.editor || !pair.elem.children[0]) {
                pairs.splice(n, 1);
            }
        }
        for (var pair of pairs) {
            const style = pair.elem.children[0].style;
            style.width  = "0px";  // required to shrink width.  Found no alternative solution right now.
            style.height = "0px";  // required to shrink height. Found no alternative solution right now.
        }
        for (var pair of pairs) {
            pair.editor.layout();
        }
        // set editor width/height to their container width/height
        for (var pair of pairs) {
            const style  = pair.elem.children[0].style;
            style.width  = pair.elem.clientWidth  + "px";
            style.height = pair.elem.clientHeight + "px";
        }
    }

    dragTemplate;
    dragBar;
    dragOffset;
    dragHorizontal;

    startDrag(event, template, bar, horizontal) {
        // console.log(`drag start: ${event.offsetX}, ${template}, ${bar}`)
        this.dragHorizontal = horizontal;
        this.dragOffset     = horizontal ? event.offsetX : event.offsetY
        this.dragTemplate   = document.getElementById(template);
        this.dragBar        = document.getElementById(bar);
        document.body.style.cursor = "ew-resize";
        event.preventDefault();
    }

    getGridColumns(xy) {
        const prev = this.dragBar.previousElementSibling;
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
    }

    onDrag(event) {
        if (!this.dragTemplate)
            return;
        // console.log(`  drag: ${event.clientX}`);
        const clientXY  = this.dragHorizontal ? event.clientX : event.clientY;
        const xy         = clientXY - this.dragOffset;
        const cols      = this.getGridColumns(xy);
        if (this.dragHorizontal) {
            this.dragTemplate.style.gridTemplateColumns = cols.join(" ");
        } else {
            this.dragTemplate.style.gridTemplateRows = cols.join(" ");
        }
        this.layoutEditors();
        event.preventDefault();
    }

    endDrag() {
        if (!this.dragTemplate)
            return;
        this.dragTemplate = undefined;
        document.body.style.cursor = "auto";
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
window.addEventListener("keyup", event => app.onKeyUp(event), true);
