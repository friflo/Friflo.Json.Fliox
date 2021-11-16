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



export function connectWebsocket() {
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
    connection.onopen = function () {
        socketStatus.innerHTML = "connected <small>🟢</small>";
        console.log('WebSocket connected');
        req         = 1;
        subCount    = 0;
    };

    connection.onclose = function (e) {
        socketStatus.innerText = "closed (code: " + e.code + ")";
        responseState.innerText = "";
        console.log('WebSocket closed');
    };

    // Log errors
    connection.onerror = function (error) {
        socketStatus.innerText = "error";
        console.log('WebSocket Error ' + error);
    };

    // Log messages from the server
    connection.onmessage = function (e) {
        var duration = new Date().getTime() - requestStart;
        var data = JSON.parse(e.data);
        // console.log('server:', e.data);
        switch (data.msg){ 
            case "resp":
            case "error":
                clt = data.clt;
                cltElement.innerText  = clt ?? " - ";
                responseModel.setValue(e.data)
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

export function closeWebsocket() {
    connection.close();
}

export function sendSyncRequest() {
    if (!connection || connection.readyState != 1) { // 1 == OPEN {
        responseModel.setValue(`Request ${req} failed. WebSocket not connected`)
        responseState.innerHTML = "";
    } else {
        var jsonRequest = requestModel.getValue();
        jsonRequest = jsonRequest.replace("{{user}}",  defaultUser.value);
        jsonRequest = jsonRequest.replace("{{token}}", defaultToken.value);
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

export async function postSyncRequest() {
    var jsonRequest = requestModel.getValue();
    jsonRequest = jsonRequest.replace("{{user}}",  defaultUser.value);
    jsonRequest = jsonRequest.replace("{{token}}", defaultToken.value);

    responseState.innerHTML = '<span class="spinner"></span>';
    let start = new Date().getTime();
    let init = {        
        method: 'POST',
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json'
        },
        body: jsonRequest
    }
    var duration;
    try {
        const rawResponse = await fetch('./', init);
        const content = await rawResponse.text();
        duration = new Date().getTime() - start;
        responseModel.setValue(content);
    } catch(error) {
        duration = new Date().getTime() - start;
        responseModel.setValue("POST error: " + error.message);
    }
    responseState.innerHTML = `· ${duration} ms`;
}

// --------------------------------------- example requests ---------------------------------------
export async function onExampleChange() {
    var exampleName = selectExample.value;
    if (exampleName == "") {
        requestModel.setValue("")
        return;
    }
    var response = await fetch(exampleName);
    var example = await response.text();
    requestModel.setValue(example)
}

export async function loadExampleRequestList() {
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

// --------------------------------------- Browser ---------------------------------------
var monacoTheme = "light";

async function postRequest(database, tasks) {
    database = database == "default" ? undefined : database;
    const request = JSON.stringify({
        "msg":      "sync",
        "database": database,
        "tasks":    tasks,
        "user":     defaultUser.value,
        "token":    defaultToken.value
    });
    let init = {        
        method:  'POST',
        headers: { 'Content-Type': 'application/json' },
        body:    request
    }
    return await fetch(`./`, init);
}

export function setTheme () {
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        document.documentElement.setAttribute('data-theme', 'dark');
        monacoTheme = "vs-dark";
    }
}

export function openTab (tabName) {
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
    layoutEditors();
}

var selectedCatalog;
var selectedEntity;

export async function loadCluster() {
    const tasks = [{ "task": "query", "container": "catalogs", "filter":{ "op": "true" }}];
    const rawResponse = await postRequest("cluster", tasks);
    const content = await rawResponse.json();
    const catalogs = content.containers[0].entities;
    var ulCatalogs = document.createElement('ul');
    ulCatalogs.onclick = (ev) => {
        var path = ev.composedPath();
        var style = path[1].childNodes[1].style;
        style.display = style.display == "none" ? "" : "none";
    }
    for (var catalog of catalogs) {
        var liCatalog = document.createElement('li');
        var catalogLabel = document.createElement('div');
        catalogLabel.innerText = catalog.name;
        liCatalog.append(catalogLabel)
        ulCatalogs.append(liCatalog);
        if (catalog.containers.length > 0) {
            var ulContainers = document.createElement('ul');
            ulContainers.onclick = (ev) => {
                ev.stopPropagation();
                if (selectedCatalog) selectedCatalog.classList.remove("selected");
                var path = ev.composedPath();
                const database  = path[3].childNodes[0].innerText;
                selectedCatalog = path[0];
                selectedCatalog.classList = "selected";
                const container = selectedCatalog.innerText;
                // console.log(database, container);
                loadEntities(database, container);
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
    catalogExplorer.textContent = "";
    catalogExplorer.appendChild(ulCatalogs);
}

export async function loadEntities(database, container) {
    entityModel.setValue("");    
    const tasks =  [{ "task": "query", "container": container, "filter":{ "op": "true" }}];
    const rawResponse = await postRequest(database, tasks);
    const content = await rawResponse.json();
    const ids = content.tasks[0].ids;
    var ulIds = document.createElement('ul');
    ulIds.onclick = (ev) => {
        if (selectedEntity) selectedEntity.classList.remove("selected");
        var path = ev.composedPath();
        selectedEntity = path[0];
        const entityId = selectedEntity.innerText;
        selectedEntity.classList = "selected";
        // console.log(entityId);
        loadEntity(database, container, entityId);
    }
    for (var id of ids) {
        var liId = document.createElement('li');
        liId.innerText = id;
        ulIds.append(liId);
    }
    entityExplorer.textContent = "";
    entityExplorer.appendChild(ulIds);
}

export async function loadEntity(database, container, entityId) {
    const tasks = [{ "task": "read", "container": container, "reads": [{ "ids": [entityId] }] }];
    const rawResponse = await postRequest(database, tasks);
    const content = await rawResponse.json();
    const entityValue = content.containers[0].entities[0];
    const entityJson = JSON.stringify(entityValue, null, 2);
    // console.log(entityJson);

    entityModel.setValue(entityJson);
}



// --------------------------------------- monaco editor ---------------------------------------
// [Monaco Editor Playground] https://microsoft.github.io/monaco-editor/playground.html#extending-language-services-configure-json-defaults

async function createProtocolSchemas() {

    // configure the JSON language support with schemas and schema associations
    var schemaUrlsResponse  = await fetch("/protocol/json-schema/directory");
    var schemaUrls          = await schemaUrlsResponse.json();
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
    for (let i = 0; i < schemaUrls.length; i++) {
        var schemaName      = schemaUrls[i]
        var url             = "protocol/json-schema/" + schemaName;
        var schemaResponse  = await fetch(url);
        var schema          = await schemaResponse.json();
        var schemaEntry = {
            uri:    "http://" + url,
            schema: schema            
        }
        schemas.push(schemaEntry);
    }
    return schemas;
}

var requestModel;
var responseModel;
var entityModel;

var requestEditor;
var responseEditor;
var entityEditor;

const requestContainer  = document.getElementById("requestContainer");
const responseContainer = document.getElementById("responseContainer")
const entityContainer   = document.getElementById("entityContainer");

export async function setupEditors()
{
    // --- setup JSON Schema for monaco
    var requestUri  = monaco.Uri.parse("request://jsonRequest.json");   // a made up unique URI for our model
    var responseUri = monaco.Uri.parse("request://jsonResponse.json");  // a made up unique URI for our model
    var entityUri   = monaco.Uri.parse("request://jsonEntity.json");    // a made up unique URI for our model
    var schemas     = await createProtocolSchemas();

    for (let i = 0; i < schemas.length; i++) {
        if (schemas[i].uri == "http://protocol/json-schema/Friflo.Json.Fliox.Hub.Protocol.ProtocolRequest.json") {
            schemas[i].fileMatch = [requestUri.toString()]; // associate with our model
        }
        if (schemas[i].uri == "http://protocol/json-schema/Friflo.Json.Fliox.Hub.Protocol.ProtocolMessage.json") {
            schemas[i].fileMatch = [responseUri.toString()]; // associate with our model
        }
    }
    monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
        validate: true,
        schemas: schemas
    });
    // --- create request editor
    { 
        requestEditor = monaco.editor.create(requestContainer, { /* model: model */ });
        requestEditor.updateOptions({
            lineNumbers:    "off",
            minimap:        { enabled: false },
            theme:          monacoTheme,
        });
        requestModel = monaco.editor.createModel(null, "json", requestUri);
        requestEditor.setModel (requestModel);

        var defaultRequest = `{
  "msg": "sync",
  "tasks": [
    {
      "task":  "command",
      "name":  "Echo",
      "value": "Hello World"
    }
  ],
  "user":   "{{user}}",
  "token":  "{{token}}"
}`;
        requestModel.setValue(defaultRequest);
    }

    // --- create response editor
    {
        responseEditor = monaco.editor.create(responseContainer, { /* model: model */ });
        responseEditor.updateOptions({
            lineNumbers:    "off",
            minimap:        { enabled: false }
        });
        responseModel = monaco.editor.createModel(null, "json", responseUri);
        responseEditor.setModel (responseModel);
    }

    // --- create entity editor
    {
        entityEditor = monaco.editor.create(entityContainer, { });
        entityEditor.updateOptions({
            lineNumbers:    "off",
            minimap:        { enabled: false }
        });
        entityModel = monaco.editor.createModel(null, "json", entityUri);
        entityEditor.setModel (entityModel);
    }

    window.onresize = () => {
        layoutEditors();        
    };
}

function layoutEditors() {
    console.log("layoutEditors - activeTab: " + activeTab)
    switch (activeTab) {
        case "playground":
            requestEditor?.layout();
            responseEditor?.layout();
            break;
        case "explorer":
            entityEditor?.layout();
            break;
    }
}

export function addTableResize () {
    var thElm;
    var startOffset;

    Array.prototype.forEach.call(
      document.querySelectorAll("table td"),
      function (th) {
        th.style.position = 'relative';

        var grip = document.createElement('div');
        grip.innerHTML = "&nbsp;";
        grip.style.top = 0;
        grip.style.right = 0;
        grip.style.bottom = 0;
        grip.style.width = '7px'; // 
        grip.style.position = 'absolute';
        grip.style.cursor = 'col-resize';
        grip.style.userSelect = 'none'; // disable text selection while dragging
        grip.addEventListener('mousedown', function (e) {
            thElm = th;
            startOffset = th.offsetWidth - e.pageX;
        });

        th.appendChild(grip);
      });

    document.addEventListener('mousemove', function (e) {
      if (thElm) {
        var width = startOffset + e.pageX + 'px'
        thElm.style.width = width;
        var elem = thElm.children[0];
        elem.style.width    = width;
        layoutEditors();
        // console.log("---", width)
      }
    });

    document.addEventListener('mouseup', function () {
        thElm = undefined;
    });
}