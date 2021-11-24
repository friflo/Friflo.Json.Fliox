// --------------------------------------- WebSocket ---------------------------------------
var connection;
var websocketCount = 0;
var req = 1;
var clt = null;
var requestStart;
var subSeq   = 0;
var subCount = 0;
var activeTab;

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
                    this.responseModel.setValue(e.data)
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
            const content = await response.text;
            duration = new Date().getTime() - start;
            this.responseModel.setValue(content);
        } catch(error) {
            duration = new Date().getTime() - start;
            this.responseModel.setValue("POST error: " + error.message);
        }
        responseState.innerHTML = `· ${duration} ms`;
    }

    onKeyDown (event) {
        switch (activeTab) {
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
                if (event.code == 'KeyS' && event.ctrlKey) {
                    this.saveEntity()
                    event.preventDefault();
                }
                break;
        }
        // console.log(`KeyboardEvent: code='${event.code}', ctrl:${event.ctrlKey}, alt:${event.altKey}`);
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
    monacoTheme = "light";

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
        return await this.postRequest(request, `${database}/${tag}`);
    }

    async restRequest (method, body, database, container, id) {
        let path = `./rest/${database}`;
        if (container)  path = `${path}/${container}`;
        if (id)         path = `${path}/${id}`;
        
        let init = {        
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

    errorAsHtml (error) {
        return `<code style="white-space: pre-line; color:red">${error}</code>`;
    }

    setTheme () {
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            document.documentElement.setAttribute('data-theme', 'dark');
            this.monacoTheme = "vs-dark";
        }
    }

    openTab (tabName) {
        activeTab = tabName;
        var tabContents = document.getElementsByClassName("tabContent");
        var tabs = document.getElementsByClassName("tab");
        for (var i = 0; i < tabContents.length; i++) {
            const tabContent = tabContents[i]
            tabContent.style.display = tabContent.id == tabName ? "block" : "none";
            if (tabContent.id == tabName) {
                tabs[i].classList.add("selected");
            } else {
                tabs[i].classList.remove("selected");
            }
        }
        this.layoutEditors();
    }

    selectedCatalog;
    selectedEntity;

    async loadCluster () {
        const tasks = [
            { "task": "query", "container": "catalogs",  "filter":{ "op": "true" }},
            { "task": "query", "container": "schemas",   "filter":{ "op": "true" }}
        ];
        catalogExplorer.innerHTML = 'read catalogs <span class="spinner"></span>';
        const response = await this.postRequestTasks("cluster", tasks, "catalogs");
        const content = response.json;
        var error = this.getTaskError (content, 0);
        if (error) {
            catalogExplorer.innerHTML = this.errorAsHtml(error);
            return 
        }
        const catalogs  = content.containers[0].entities;
        const schemas   = content.containers[1].entities;
        var ulCatalogs = document.createElement('ul');
        ulCatalogs.onclick = (ev) => {
            var path = ev.composedPath();
            const database = path[0].innerText;
            var schema = schemas.find(s => s.id == database);
            var service = schema.jsonSchemas[schema.schemaPath].definitions[schema.schemaName + "Service"];
            this.listCommands(database, service.commands);
            // var style = path[1].childNodes[1].style;
            // style.display = style.display == "none" ? "" : "none";
        }
        for (var catalog of catalogs) {
            var liCatalog = document.createElement('li');
            var catalogLabel = document.createElement('div');
            catalogLabel.innerText = catalog.id;
            liCatalog.append(catalogLabel)
            ulCatalogs.append(liCatalog);
            if (catalog.containers.length > 0) {
                var ulContainers = document.createElement('ul');
                ulContainers.onclick = (ev) => {
                    ev.stopPropagation();
                    var path = ev.composedPath();
                    const selectedElement = path[0];
                    // in case of a multiline text selection selectedElement is the parent
                    if (selectedElement.tagName.toLowerCase() != "div")
                        return;
                    if (this.selectedCatalog) this.selectedCatalog.classList.remove("selected");
                    const database  = path[3].childNodes[0].innerText;
                    this.selectedCatalog = selectedElement;
                    this.selectedCatalog.classList = "selected";
                    const container = this.selectedCatalog.innerText;
                    // console.log(database, container);
                    var schema = schemas.find(s => s.id == database);
                    this.loadEntities(database, container, schema);
                }
                liCatalog.append(ulContainers);
                for (const container of catalog.containers) {
                    var liContainer = document.createElement('li');
                    var containerLabel = document.createElement('div');
                    containerLabel.innerText = container;
                    liContainer.append(containerLabel)
                    ulContainers.append(liContainer);
                }
            }
        }
        this.createEntitySchemas(schemas)
        catalogExplorer.textContent = "";
        catalogExplorer.appendChild(ulCatalogs);
    }

    createEntitySchemas (catalogSchemas) {
        var monacoSchemas = [];
        for (var catalogSchema of catalogSchemas) {
            var jsonSchemas     = catalogSchema.jsonSchemas;
            var database        = catalogSchema.id;
            var dbSchema        = jsonSchemas[catalogSchema.schemaPath];
            var dbType          = dbSchema.definitions[catalogSchema.schemaName];
            var containers      = dbType.properties;
            var typeMap    = {};
            for (var containerName in containers) {
                var container   = containers[containerName];
                var type        = container.additionalProperties.$ref;
                var hashPos     = type.indexOf("#");
                type = type.substring(2, hashPos);
                typeMap[type] = containerName;
            }

            for (var schemaName in jsonSchemas) {
                var schema      = jsonSchemas[schemaName];
                var url         = database + "/" + schemaName;
                var schemaEntry = {
                    uri:   "http://" + url,
                    schema: schema            
                }
                monacoSchemas.push(schemaEntry);
                var container = typeMap[schemaName];
                if (container) {
                    var url = `entity://${database}.${container}.json`; // e.g. 'entity://main_db.orders.json'
                    schemaEntry.fileMatch = [url]; // associate with our model
                }
            }
        }
        this.addSchemas(monacoSchemas);
    }

    getEntityType(schema, container) {
        var dbSchema = schema.jsonSchemas[schema.schemaPath].definitions[schema.schemaName];
        var ref = dbSchema.properties[container].additionalProperties["$ref"];
        var lastSlashPos = ref.lastIndexOf('/');
        return ref.substring(lastSlashPos + 1);
    }

    listCommands (database, commands) {
        commandValueContainer.style.display = "";
        this.layoutEditors();
        this.entityModel?.setValue("");
        readEntitiesDB.innerHTML = `<a title="database" href="./rest/${database}" target="_blank" rel="noopener noreferrer">${database}</a>`;
        readEntities.innerHTML   = ` commands`;
        entityId.innerHTML      = "";
        entityType.innerText    = "";

        var ulCommands = document.createElement('ul');
        ulCommands.onclick = (ev) => {
            var path = ev.composedPath();
            const selectedElement = path[0];
            // in case of a multiline text selection selectedElement is the parent
            if (selectedElement.tagName.toLowerCase() != "li")
                return;
            if (this.selectedEntity) this.selectedEntity.classList.remove("selected");
            this.selectedEntity = selectedElement;

            const command = this.selectedEntity.innerText;
            entityId.innerHTML    = `<a title="command" href="./rest/${database}?command=${command}" target="_blank" rel="noopener noreferrer">${command}</a>`;
            this.selectedEntity.classList = "selected";
        }
        for (const command in commands) {
            var liCommand = document.createElement('li');
            liCommand.innerText = command;
            ulCommands.append(liCommand);
        }
        entityExplorer.innerText = ""
        entityExplorer.appendChild(ulCommands);
    }

    async loadEntities (database, container, schema) {
        commandValueContainer.style.display = "none";
        this.layoutEditors();
        this.setEntityValue(database, container, "");
        const tasks =  [{ "task": "query", "container": container, "filter":{ "op": "true" }}];
        if (schema) {
            const entityLabel = this.getEntityType (schema, container);            
            entityType.innerText  = entityLabel;
            catalogSchema.innerHTML  = `<a title="database schema" href="./rest/${database}?command=CatalogSchema" target="_blank" rel="noopener noreferrer">${schema.schemaName}</a>`;
        }
        readEntitiesDB.innerHTML = `<a title="database" href="./rest/${database}" target="_blank" rel="noopener noreferrer">${database}/</a>`;
        var containerLink        = `<a title="container" href="./rest/${database}/${container}" target="_blank" rel="noopener noreferrer">${container}/</a>`;
        readEntities.innerHTML   = `${containerLink} <span class="spinner"></span>`;
        const response = await this.postRequestTasks(database, tasks, container);

        const content = response.json;
        entityId.innerHTML      = "";
        writeResult.innerHTML   = "";
        readEntities.innerHTML  = containerLink;
        var error = this.getTaskError (content, 0);
        if (error) {
            entityExplorer.innerHTML = this.errorAsHtml(error);
            return;
        }
        const ids = content.tasks[0].ids;
        var ulIds = document.createElement('ul');
        ulIds.onclick = (ev) => {
            var path = ev.composedPath();
            const selectedElement = path[0];
            // in case of a multiline text selection selectedElement is the parent
            if (selectedElement.tagName.toLowerCase() != "li")
                return;
            if (this.selectedEntity) this.selectedEntity.classList.remove("selected");
            this.selectedEntity = selectedElement;

            const entityId = this.selectedEntity.innerText;
            this.selectedEntity.classList = "selected";
            // console.log(entityId);
            this.loadEntity(database, container, entityId);
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
        entityId:   undefined
    }

    async loadEntity (database, container, id) {
        this.entityIdentity = {
            database:   database,
            container:  container,
            entityId:   id
        };
        var entityLink          = `<a title="entity id" href="./rest/${database}/${container}/${id}" target="_blank" rel="noopener noreferrer">${id}</a>`
        entityId.innerHTML      = `${entityLink} <span class="spinner"></span>`;
        writeResult.innerHTML   = "";
        const response  = await this.restRequest("GET", null, database, container, id);        
        const content   = await response.text();
        entityId.innerHTML = entityLink;
        if (!response.ok) {
            this.setEntityValue(database, container, content);
            return;
        }
        // console.log(entityJson);
        this.setEntityValue(database, container, content);
    }

    async saveEntity () {
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

        const response = await this.restRequest("PUT", jsonValue, this.entityIdentity.database, container, id);
        if (!response.ok) {
            const error = await response.text();
            writeResult.innerHTML = `<span style="color:red">Save failed: ${error}</code>`;
            return;
        }
        writeResult.innerHTML = "Save successful";
        // add as HTML element to entityExplorer if new
        if (this.entityIdentity.entityId != id) {
            this.entityIdentity.entityId = id;
            entityId.innerHTML = id;
            if (this.selectedEntity)
                this.selectedEntity.classList.remove("selected");
            var liId = document.createElement('li');
            liId.innerText = id;
            liId.classList = "selected";
            const ulIds= entityExplorer.querySelector("ul");
            ulIds.append(liId);
            this.selectedEntity = liId;
            this.selectedEntity.scrollIntoView();
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

    setEntityValue (database, container, value) {
        var url = `entity://${database}.${container}.json`;
        this.entityModel = this.entityModels[url];
        if (!this.entityModel) {
            var entityUri   = monaco.Uri.parse(url);
            this.entityModel = monaco.editor.createModel(null, "json", entityUri);
            this.entityModels[url] = this.entityModel;
        }
        this.entityEditor.setModel (this.entityModel);
        this.entityModel.setValue(value);
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
        var jsonSchemaResponse  = await fetch("protocol/json-schema.json");
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
    commandValue            = document.getElementById("commandValue");
    entityContainer         = document.getElementById("entityContainer");

    allMonacoSchemas = [];

    addSchemas (monacoSchemas) {
        this.allMonacoSchemas.push(...monacoSchemas);
        // [DiagnosticsOptions | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.languages.json.DiagnosticsOptions.html
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
            validate: true,
            schemas: this.allMonacoSchemas
        });
    }

    async setupEditors ()
    {
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
            this.requestEditor.updateOptions({
                lineNumbers:    "off",
                minimap:        { enabled: false },
                theme:          this.monacoTheme,
            });
            this.requestModel = monaco.editor.createModel(null, "json", requestUri);
            this.requestEditor.setModel (this.requestModel);

            var defaultRequest = `{
    "msg": "sync",
    "tasks": [
        {
        "task":  "command",
        "name":  "Echo",
        "value": "Hello World"
        }
    ]
}`;
            this.requestModel.setValue(defaultRequest);
        }

        // --- create response editor
        {
            this.responseEditor = monaco.editor.create(responseContainer, { /* model: model */ });
            this.responseEditor.updateOptions({
                lineNumbers:    "off",
                minimap:        { enabled: false }
            });
            this.responseModel = monaco.editor.createModel(null, "json", responseUri);
            this.responseEditor.setModel (this.responseModel);
        }

        // --- create entity editor
        {
            this.entityEditor = monaco.editor.create(entityContainer, { });
            this.entityEditor.updateOptions({
                lineNumbers:    "off",
                minimap:        { enabled: false }
            });
        }
        // --- create command value editor
        {
            this.commandValueEditor = monaco.editor.create(commandValue, { });
            this.commandValueEditor.updateOptions({
                lineNumbers:    "off",
                minimap:        { enabled: false }
            });
        }

        window.onresize = () => {
            this.layoutEditors();        
        };
    }

    layoutEditors () {
        // console.log("layoutEditors - activeTab: " + activeTab)
        switch (activeTab) {
            case "playground":
                this.requestEditor?.layout();
                this.responseEditor?.layout();
                break;
            case "explorer":
                commandValue.children[0].style.width = "100px"; // required to shrink width
                entityContainer.children[0].style.width = "100px"; // required to shrink width
                this.commandValueEditor?.layout();
                this.entityEditor?.layout();
                break;
        }
    }

    addTableResize () {
        var tdElm;
        var startOffset;
        const selector = document.querySelectorAll("table td");

        Array.prototype.forEach.call(selector, (td) =>
        {
            if (!td.classList.contains("vbar"))
                return;
            td.style.position = 'relative';

            var grip = document.createElement('div');
            grip.innerHTML = "&nbsp;";
            grip.style.top = 0;
            grip.style.right = 0;
            grip.style.bottom = 0;
            grip.style.width = '7px'; // 
            grip.style.position = 'absolute';
            grip.style.cursor = 'col-resize';
            grip.style.userSelect = 'none'; // disable text selection while dragging
            grip.addEventListener('mousedown', (e) => {
                var previous = td.previousElementSibling;
                tdElm = previous;
                startOffset = previous.offsetWidth - e.pageX;
            });

            td.appendChild(grip);
        });

        document.addEventListener('mousemove', (e) => {
        if (tdElm) {
            var width = startOffset + e.pageX + 'px'
            tdElm.style.width = width;
            var elem = tdElm.children[0];
            elem.style.width    = width;
            this.layoutEditors();
            // console.log("---", width)
        }
        });

        document.addEventListener('mouseup', () => {
            tdElm = undefined;
        });
    }
}

export const app = new App();
window.addEventListener("keydown", event => app.onKeyDown(event), true);
