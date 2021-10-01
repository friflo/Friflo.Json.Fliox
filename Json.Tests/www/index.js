

// --------------------------------------- WebSocket ---------------------------------------
var connection;
var websocketCount = 0;
var reqId = 1;

export function connectWebsocket() {
    var loc             = window.location;
    var uri             = `ws://${loc.host}/websocket-${++websocketCount}`;
    var socketStatus    = document.getElementById("socketStatus");
    try {
        connection = new WebSocket(uri);
    } catch (err) {
        socketStatus.innerText = "connect failed: err";
        return;
    }
    connection.onopen = function () {
        socketStatus.innerText = "connected 🟢";
        console.log('WebSocket connected');
        reqId = 1;
    };

    connection.onclose = function (e) {
        socketStatus.innerText = "closed. code: " + e.code;
        console.log('WebSocket closed');
    };

    // Log errors
    connection.onerror = function (error) {
        socketStatus.innerText = "error";
        console.log('WebSocket Error ' + error);
    };

    // Log messages from the server
    connection.onmessage = function (e) {
        // var data = JSON.parse(e.data);
        // console.log('server:', e.data);
        responseModel.setValue(e.data)
    };
}

export function closeWebsocket() {
    connection.close();
}

export function sendSyncRequest() {
    var reqIdElement = document.getElementById("reqId");
    if (!connection || connection.readyState != 1) { // 1 == OPEN {
        reqIdElement.innerText = "n/a (Request failed. WebSocket not connected)";
        return;
    }
    var jsonRequest = requestModel.getValue();
    try {
        var request = JSON.parse(jsonRequest);
        request.reqId = reqId;
        jsonRequest = JSON.stringify(request);                
    } catch { }
    connection.send(jsonRequest);
    reqIdElement.innerText = reqId;
    reqId++;
}

export async function onExampleChange() {
    var selectElement = document.getElementById("example");
    var exampleName = selectElement.value;
    if (exampleName == "")
        return;
    var response = await fetch(exampleName);
    var example = await response.text();
    requestModel.setValue(example)
}

export async function loadExampleRequestList() {
    var folder = './example-requests'
    var response = await fetch(folder);
    var exampleRequests = await response.json();
    var selectElement = document.getElementById("example");
    var exampleGroup = "0";
    for (var example of exampleRequests) {
        if (!example.endsWith(".json"))
            continue;
        var name = example.substring(folder.length);
        if (exampleGroup != name[0]) {
            selectElement.add(document.createElement("option"));
            exampleGroup = name[0];
        }
        var option = document.createElement("option");
        option.value    = example;
        option.text     = name;
        selectElement.add(option);
    }
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

export async function setupEditors()
{
    // --- setup JSON Schema for monaco
    var requestUri  = monaco.Uri.parse("request://jsonRequest.json"); // a made up unique URI for our model
    var responseUri = monaco.Uri.parse("request://jsonResponse.json"); // a made up unique URI for our model
    var schemas     = await createProtocolSchemas();

    for (let i = 0; i < schemas.length; i++) {
        if (schemas[i].uri == "http://protocol/json-schema/Friflo.Json.Fliox.DB.Protocol.SyncRequest.json") {
            schemas[i].fileMatch = [requestUri.toString()]; // associate with our model
        }
        if (schemas[i].uri == "http://protocol/json-schema/Friflo.Json.Fliox.DB.Protocol.SyncResponse.json") {
            schemas[i].fileMatch = [responseUri.toString()]; // associate with our model
        }
    }
    monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
        validate: true,
        schemas: schemas
    });
    // --- create request editor
    { 
        var requestEditor = monaco.editor.create(document.getElementById("requestContainer"), { /* model: model */ });
        requestEditor.updateOptions({
            lineNumbers:    "off",
            minimap:        { enabled: false }
        });
        requestModel = monaco.editor.createModel(null, "json", requestUri);
        requestEditor.setModel (requestModel);

        var defaultRequest = `{
    "type": "syncX",
    "tasks": [
        {
            "task":  "message",
            "name":  "Echo",
            "value": "some value"
        }
    ]
}`;
        requestModel.setValue(defaultRequest);
    }

    // --- create response editor
    {
        var responseEditor = monaco.editor.create(document.getElementById("responseContainer"), { /* model: model */ });
        responseEditor.updateOptions({
            lineNumbers:    "off",
            minimap:        { enabled: false }
        });
        responseModel = monaco.editor.createModel(null, "json", responseUri);
        responseEditor.setModel (responseModel);
    }
}