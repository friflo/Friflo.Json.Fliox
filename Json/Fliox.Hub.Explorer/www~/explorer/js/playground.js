import { el, createEl } from "./types.js";
import { App, app } from "./index.js";
import { WebSocketClient } from "./websocket.js";
const responseState = el("response-state");
const subscriptionCount = el("subscriptionCount");
const eventCount = el("eventCount");
const subscriptionSeq = el("subscriptionSeq");
const socketStatus = el("socketStatus");
const reqIdElement = el("req");
const ackElement = el("ack");
const cltElement = el("clt");
const selectExample = el("example");
//
const defaultUser = el("user");
const defaultToken = el("token");
// ----------------------------------------------- Playground -----------------------------------------------
export class Playground {
    getClientId() { var _a; return (_a = this.wsClient) === null || _a === void 0 ? void 0 : _a.clt; }
    constructor() {
        this.websocketCount = 0;
        this.eventCount = 0; // number of received events. Reset for every new wsClient
        this.groupPrefix = "0";
        this.groupCount = 0;
        this.wsClient = new WebSocketClient(() => {
            return { user: defaultUser.value, token: defaultToken.value };
        });
        this.wsClient.onClose = (e) => {
            socketStatus.innerText = "closed (code: " + e.code + ")";
            responseState.innerText = "";
        };
        this.wsClient.onEvents = (eventMessage) => {
            const events = eventMessage.ev;
            this.eventCount += events.length;
            const countStr = String(this.eventCount);
            subscriptionCount.innerText = countStr;
            eventCount.innerText = countStr;
            const seq = eventMessage.seq;
            for (const ev of events) {
                app.events.addSubscriptionEvent(ev, seq);
            }
            // multiple clients can use the same WebSocket. Use the latest
            subscriptionSeq.innerText = seq ? String(seq) : " - ";
            ackElement.innerText = seq ? String(seq) : " - ";
        };
    }
    connectWebsocket() {
        this.wsClient.close();
        this.connect();
    }
    async connect() {
        if (this.wsClient.isOpen()) {
            return null;
        }
        try {
            return await this.connectWebSocket();
        }
        catch (err) {
            const errMsg = `connect failed: ${err}`;
            socketStatus.innerText = errMsg;
            return errMsg;
        }
    }
    // Single requirement to the WebSocket Uri: path have to start with the endpoint. e.g. /fliox/
    getWebsocketUri() {
        const loc = window.location;
        const protocol = loc.protocol == "http:" ? "ws:" : "wss:";
        // add websocketCount to path to identify WebSocket in DevTools > Network.
        const nr = `${++this.websocketCount}`.padStart(3, "0"); // ws-001
        const endpoint = loc.pathname.substring(0, loc.pathname.lastIndexOf("/") + 1); // /fliox/    
        const uri = `${protocol}//${loc.host}${endpoint}ws-${nr}`;
        return uri;
    }
    async connectWebSocket() {
        const uri = this.getWebsocketUri();
        // const uri    = `ws://google.com:8080/`; // test connection timeout
        socketStatus.innerHTML = 'connecting <span class="spinner"></span>';
        const error = await this.wsClient.connect(uri);
        this.eventCount = 0;
        if (error) {
            socketStatus.innerText = "error";
            return error;
        }
        socketStatus.innerHTML = "connected <small>🟢</small>";
        return null;
    }
    closeWebsocket() {
        this.wsClient.close();
    }
    setClientId(clientId) {
        if (!clientId)
            return;
        this.wsClient.clt = clientId;
        cltElement.innerText = clientId;
    }
    addUserToken(jsonRequest) {
        var _a;
        const endBracket = jsonRequest.lastIndexOf("}");
        if (endBracket == -1)
            return jsonRequest;
        const before = jsonRequest.substring(0, endBracket);
        const after = jsonRequest.substring(endBracket);
        const clt = (_a = this.wsClient.clt) !== null && _a !== void 0 ? _a : undefined;
        let userToken = JSON.stringify({ user: defaultUser.value, token: defaultToken.value, clt: clt });
        userToken = userToken.substring(1, userToken.length - 1);
        return `${before},${userToken}${after}`;
    }
    async sendSyncRequest() {
        const wsClient = this.wsClient;
        if (!wsClient.isOpen()) {
            app.responseModel.setValue(`Request failed. WebSocket not connected`);
            responseState.innerHTML = "";
            return;
        }
        let jsonRequest = app.requestModel.getValue();
        jsonRequest = this.addUserToken(jsonRequest);
        const syncRequest = JSON.parse(jsonRequest);
        // Enable overrides of WebSocket specific members
        //  if (syncRequest.req !== undefined) { this.req           = syncRequest.req; }
        //  if (syncRequest.ack !== undefined) { this.lastEventSeq  = syncRequest.ack; }
        //  if (syncRequest.clt !== undefined) { this.wsClient.clt  = syncRequest.clt; }
        responseState.innerHTML = '<span class="spinner"></span>';
        const response = await this.sendWsClientRequest(syncRequest);
        const duration = response.end - response.start;
        const content = app.formatJson(app.config.formatResponses, response.json);
        app.responseModel.setValue(content);
        responseState.innerHTML = `· ${duration.toFixed(1)} ms`;
    }
    async sendWebSocketRequest(syncRequest) {
        syncRequest.user = defaultUser.value;
        syncRequest.token = defaultToken.value;
        return await this.sendWsClientRequest(syncRequest);
    }
    async sendWsClientRequest(syncRequest) {
        var _a;
        // Add WebSocket specific members to request
        const response = await this.wsClient.syncRequest(syncRequest);
        reqIdElement.innerText = String(this.wsClient.getReqId());
        cltElement.innerText = (_a = this.wsClient.clt) !== null && _a !== void 0 ? _a : " - ";
        return response;
    }
    async postSyncRequest() {
        let jsonRequest = app.requestModel.getValue();
        jsonRequest = this.addUserToken(jsonRequest);
        responseState.innerHTML = '<span class="spinner"></span>';
        const start = performance.now();
        let duration;
        try {
            const response = await App.postRequest(jsonRequest, "?POST");
            let content = response.text;
            content = app.formatJson(app.config.formatResponses, content);
            duration = performance.now() - start;
            app.responseModel.setValue(content);
        }
        catch (error) {
            duration = performance.now() - start;
            app.responseModel.setValue("POST error: " + error.message);
        }
        responseState.innerHTML = `· ${duration.toFixed(1)} ms`;
    }
    // --------------------------------------- example requests ---------------------------------------
    async onExampleChange() {
        const exampleName = selectExample.value;
        if (exampleName == "") {
            app.requestModel.setValue("");
            return;
        }
        const option = selectExample.options[selectExample.selectedIndex];
        if (option.dataset["source"] == "remote") {
            const response = await fetch(exampleName);
            const example = await response.text();
            app.requestModel.setValue(example);
            return;
        }
        const value = JSON.stringify(defaultExamples[exampleName], null, 4);
        app.requestModel.setValue(value);
    }
    async initExampleRequestList() {
        // [html - How do I make a placeholder for a 'select' box? - Stack Overflow] https://stackoverflow.com/questions/5805059/how-do-i-make-a-placeholder-for-a-select-box
        let option = createEl("option");
        option.value = "";
        option.disabled = true;
        option.selected = true;
        option.hidden = true;
        option.text = "Select request ...";
        selectExample.add(option);
        // --- add default examples
        for (const example in defaultExamples) {
            const name = example.replace(".sync.json", "");
            if (this.groupPrefix != name[0]) {
                this.groupPrefix = name[0];
                this.groupCount++;
            }
            option = createEl("option");
            option.value = name;
            option.dataset["source"] = "default";
            option.text = (this.groupCount % 2 ? "\xA0\xA0" : "") + name;
            option.style.backgroundColor = this.groupCount % 2 ? "#ffffff" : "#eeeeff";
            selectExample.add(option);
        }
    }
    async addRemoteExamples(examplesPath) {
        // --- add examples from remote folder
        const response = await fetch(examplesPath);
        if (!response.ok)
            return;
        const exampleRequests = await response.json();
        for (const example of exampleRequests) {
            if (!example.endsWith(".json"))
                continue;
            const name = example.replace(".sync.json", "");
            if (this.groupPrefix != name[0]) {
                this.groupPrefix = name[0];
                this.groupCount++;
            }
            const option = createEl("option");
            option.value = examplesPath + "/" + example;
            option.dataset["source"] = "remote";
            option.text = (this.groupCount % 2 ? "\xA0\xA0" : "") + name;
            option.style.backgroundColor = this.groupCount % 2 ? "#ffffff" : "#eeeeff";
            selectExample.add(option);
        }
    }
}
export const defaultExamples = {
    "00-empty": {
        "msg": "sync",
        "tasks": [
            {
                "task": "cmd",
                "name": "std.Echo",
                "param": "Hello World"
            }
        ]
    },
    "01-command": {
        "msg": "sync",
        "tasks": [
            {
                "task": "cmd",
                "name": "std.Echo",
                "param": "Hello World"
            }
        ],
        "info": [
            "Send a command with an optional param.",
            "Requires a message handler to return a result.",
            "'std.Echo' is a standard host command echoing the param.",
            "The host forward command as an event to subscribed clients."
        ]
    },
    "02-message": {
        "msg": "sync",
        "tasks": [
            {
                "task": "msg",
                "name": "SomeMessage",
                "param": "Hello Message"
            }
        ],
        "info": [
            "Send a message with an optional param.",
            "The host simply confirm its arrival.",
            "In contrast to a command it returns no result.",
            "A message handler at the host is optional.",
            "The host forward message as an event to subscribed clients."
        ]
    },
    "03-batch": {
        "msg": "sync",
        "tasks": [
            {
                "task": "cmd",
                "name": "std.Echo",
                "param": "Hello Moon"
            },
            {
                "task": "cmd",
                "name": "std.Echo",
                "param": "Hello Sun"
            }
        ],
        "info": [
            "A single request can contain multiple tasks.",
            "E.g. two commands in this this example"
        ]
    }
};
//# sourceMappingURL=playground.js.map