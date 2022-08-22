import { el, createEl } from "./types.js";
import { App, app } from "./index.js";
import { WebSocketClient } from "./websocket.js";
const responseState = el("response-state");
const subscriptionCount = el("subscriptionCount");
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
    constructor() {
        this.websocketCount = 0;
        this.req = 1; // incrementing request id. Starts with 1 for every new wsClient
        this.clt = null; // client id
        this.lastEventSeq = 0; // last received event seq. Used to acknowledge received the event via SyncRequest.ack
        this.eventCount = 0; // number of received events. Reset for every new wsClient
    }
    getClientId() { return this.clt; }
    connectWebsocket() {
        if (this.wsClient) {
            this.wsClient.close();
            this.wsClient = null;
        }
        this.connect();
    }
    async connect() {
        var _a;
        if ((_a = this.wsClient) === null || _a === void 0 ? void 0 : _a.isOpen()) {
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
        this.wsClient = new WebSocketClient();
        this.wsClient.onClose = (e) => {
            socketStatus.innerText = "closed (code: " + e.code + ")";
            responseState.innerText = "";
        };
        this.wsClient.onEvent = (data) => {
            subscriptionCount.innerText = String(++this.eventCount);
            const subSeq = this.lastEventSeq = data.seq;
            // multiple clients can use the same WebSocket. Use the latest
            if (this.clt == data.clt) {
                subscriptionSeq.innerText = subSeq ? String(subSeq) : " - ";
                ackElement.innerText = subSeq ? String(subSeq) : " - ";
                app.events.addSubscriptionEvent(data);
                // acknowledge event by sending a SyncRequest with SyncRequest.ack set to the last received seq
                const syncRequest = { msg: "sync", database: data.db, tasks: [], info: "acknowledge event" };
                this.sendWebSocketRequest(syncRequest);
            }
        };
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
        this.wsClient = null;
    }
    addUserToken(jsonRequest) {
        const endBracket = jsonRequest.lastIndexOf("}");
        if (endBracket == -1)
            return jsonRequest;
        const before = jsonRequest.substring(0, endBracket);
        const after = jsonRequest.substring(endBracket);
        let userToken = JSON.stringify({ user: defaultUser.value, token: defaultToken.value });
        userToken = userToken.substring(1, userToken.length - 1);
        return `${before},${userToken}${after}`;
    }
    async sendSyncRequest() {
        const wsClient = this.wsClient;
        if (!wsClient || !wsClient.isOpen()) {
            app.responseModel.setValue(`Request ${this.req} failed. WebSocket not connected`);
            responseState.innerHTML = "";
            this.req++; // not necessary but makes error message in editor simpler to understand
            reqIdElement.innerText = String(this.req);
            return;
        }
        let jsonRequest = app.requestModel.getValue();
        jsonRequest = this.addUserToken(jsonRequest);
        const syncRequest = JSON.parse(jsonRequest);
        // Enable overrides of WebSocket specific members
        if (syncRequest.req !== undefined) {
            this.req = syncRequest.req;
        }
        if (syncRequest.ack !== undefined) {
            this.lastEventSeq = syncRequest.ack;
        }
        if (syncRequest.clt !== undefined) {
            this.clt = syncRequest.clt;
        }
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
        syncRequest.req = this.req++;
        syncRequest.ack = this.lastEventSeq;
        if (this.clt) {
            syncRequest.clt = this.clt;
        }
        reqIdElement.innerText = String(this.req);
        const response = await this.wsClient.syncRequest(syncRequest);
        this.clt = response.message.clt; // ProtocolResponse.clt is set by Host if not set in SynRequest
        cltElement.innerText = (_a = this.clt) !== null && _a !== void 0 ? _a : " - ";
        return response;
    }
    async postSyncRequest() {
        let jsonRequest = app.requestModel.getValue();
        jsonRequest = this.addUserToken(jsonRequest);
        responseState.innerHTML = '<span class="spinner"></span>';
        const start = performance.now();
        let duration;
        try {
            const response = await App.postRequest(jsonRequest, "POST");
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
        const response = await fetch(exampleName);
        const example = await response.text();
        app.requestModel.setValue(example);
    }
    async loadExampleRequestList() {
        // [html - How do I make a placeholder for a 'select' box? - Stack Overflow] https://stackoverflow.com/questions/5805059/how-do-i-make-a-placeholder-for-a-select-box
        let option = createEl("option");
        option.value = "";
        option.disabled = true;
        option.selected = true;
        option.hidden = true;
        option.text = "Select request ...";
        selectExample.add(option);
        const folder = './explorer/example-requests';
        const response = await fetch(folder);
        if (!response.ok)
            return;
        const exampleRequests = await response.json();
        let groupPrefix = "0";
        let groupCount = 0;
        for (const example of exampleRequests) {
            if (!example.endsWith(".json"))
                continue;
            const name = example.replace(".sync.json", "");
            if (groupPrefix != name[0]) {
                groupPrefix = name[0];
                groupCount++;
            }
            option = createEl("option");
            option.value = folder + "/" + example;
            option.text = (groupCount % 2 ? "\xA0\xA0" : "") + name;
            option.style.backgroundColor = groupCount % 2 ? "#ffffff" : "#eeeeff";
            selectExample.add(option);
        }
    }
}
//# sourceMappingURL=playground.js.map