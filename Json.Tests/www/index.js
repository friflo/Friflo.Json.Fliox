

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


export function connectWebsocket() {
    if (connection) {
        connection.close();
        connection = null;
    }
    var loc     = window.location;
    var nr      = ("" + (++websocketCount)).padStart(3,0)
    var uri     = `ws://${loc.host}/websocket-${nr}`;
    // var uri  = `ws://google.com:8080/`; // test connection timeout
    socketStatus.innerHTML = 'connecting <span class="spinner"></span>';
    try {
        connection = new WebSocket(uri);
    } catch (err) {
        socketStatus.innerText = "connect failed: err";
        return;
    }
    connection.onopen = function () {
        socketStatus.innerText = "connected 🟢";
        console.log('WebSocket connected');
        req         = 1;
        subCount    = 0;
    };

    connection.onclose = function (e) {
        socketStatus.innerText = "closed (code: " + e.code + ")";
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
        switch (data.type){ 
            case "syncResp":
            case "error":
                clt = data.clt;
                cltElement.innerText  = clt ?? " - ";
                responseModel.setValue(e.data)
                responseState.innerHTML = `· ${duration} ms`;
                break;
            case "sub":
                subscriptionCount.innerText = ++subCount;
                subSeq = data.seq;
                subscriptionSeq.innerText = subSeq ? subSeq : " - ";
                break;
        }
    };
}

export function closeWebsocket() {
    connection.close();
}

export function sendSyncRequest() {
    reqIdElement.innerText  = req;
    ackElement.innerText    = subSeq ? subSeq : " - ";
    if (!connection || connection.readyState != 1) { // 1 == OPEN {
        responseModel.setValue(`Request ${req} failed. WebSocket not connected`)
        responseState.innerHTML = "";
    } else {
        var jsonRequest = requestModel.getValue();
        jsonRequest = jsonRequest.replace("{{user}}",  defaultUser.value);
        jsonRequest = jsonRequest.replace("{{token}}", defaultToken.value);
        try {
            var request     = JSON.parse(jsonRequest);
            request.req     = req;
            request.ack     = subSeq;
            request.clt     = clt;
            jsonRequest = JSON.stringify(request);                
        } catch { }
        responseState.innerHTML = '<span class="spinner"></span>';
        connection.send(jsonRequest);
        requestStart = new Date().getTime();
    }
    req++;
}

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
    selectExample.add(document.createElement("option")); // empty entry on top
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
        var option = document.createElement("option");
        option.value    = example;
        option.text     = (groupCount % 2 ? "\xA0\xA0" : "") + name;
        option.style    = groupCount % 2 ? "background-color: #ffffff;" : "background-color: #eeeeff;"
        selectExample.add(option);
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
var requestEditor;
var responseEditor;

const requestContainer  = document.getElementById("requestContainer");
const responseContainer = document.getElementById("responseContainer")

export async function setupEditors()
{
    // --- setup JSON Schema for monaco
    var requestUri  = monaco.Uri.parse("request://jsonRequest.json"); // a made up unique URI for our model
    var responseUri = monaco.Uri.parse("request://jsonResponse.json"); // a made up unique URI for our model
    var schemas     = await createProtocolSchemas();

    for (let i = 0; i < schemas.length; i++) {
        if (schemas[i].uri == "http://protocol/json-schema/Friflo.Json.Fliox.DB.Protocol.ProtocolRequest.json") {
            schemas[i].fileMatch = [requestUri.toString()]; // associate with our model
        }
        if (schemas[i].uri == "http://protocol/json-schema/Friflo.Json.Fliox.DB.Protocol.ProtocolMessage.json") {
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
            minimap:        { enabled: false }
        });
        requestModel = monaco.editor.createModel(null, "json", requestUri);
        requestEditor.setModel (requestModel);

        var defaultRequest = `{
    "type": "sync",
    "tasks": [
        {
            "task":  "message",
            "name":  "Echo",
            "value": "some value"
        }
    ],
    "user":   "{{user}}",
    "token":  "{{token}}"
}`;
        requestModel.setValue("");
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
        requestEditor.layout();
        responseEditor.layout();
        // console.log("---", width)
      }
    });

    document.addEventListener('mouseup', function () {
        thElm = undefined;
    });
}