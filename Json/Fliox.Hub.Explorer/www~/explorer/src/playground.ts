import { el, createEl }     from "./types.js";
import { App, app }         from "./index.js";
import { WebSocketClient } from "./websocket.js";
import { SyncRequest } from "../../../../../Json.Tests/assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol.js";


const responseState     = el("response-state");
const subscriptionCount = el("subscriptionCount");
const subscriptionSeq   = el("subscriptionSeq");
const socketStatus      = el("socketStatus");
const reqIdElement      = el("req");
const ackElement        = el("ack");
const cltElement        = el("clt");
const selectExample     = el("example")         as HTMLSelectElement;

//
const defaultUser       = el("user")            as HTMLInputElement;
const defaultToken      = el("token")           as HTMLInputElement;


// ----------------------------------------------- Playground -----------------------------------------------
export class Playground
{
    // --- WebSocket ---
    private wsClient:       WebSocketClient;
    private websocketCount  = 0;
    private req             = 1;
    private clt:            string | null  = null;
    private requestStart:   number;
    private subSeq          = 0;
    private subCount        = 0;

    public getClientId() : string { return this.clt; }

    public connectWebsocket (): void {
        if (this.wsClient) {
            this.wsClient.close();
            this.wsClient = null;
        }
        this.connect();
    }

    public async connect (): Promise<string> {
        if (this.wsClient) {
            return null;
        }
        try {
            return await this.connectWebSocket();
        } catch (err) {
            const errMsg = `connect failed: ${err}`;
            socketStatus.innerText = errMsg;
            return errMsg;
        }
    }

    private async connectWebSocket() : Promise <string> {
        const loc       = window.location;
        const protocol  = loc.protocol == "http:" ? "ws:" : "wss:";
        const nr        = ("" + (++this.websocketCount)).padStart(3, "0");
        const path      = loc.pathname.substring(0, loc.pathname.lastIndexOf("/") + 1);        
        const uri       = `${protocol}//${loc.host}${path}ws-${nr}`;
        // const uri  = `ws://google.com:8080/`; // test connection timeout
        socketStatus.innerHTML = 'connecting <span class="spinner"></span>';

        this.wsClient = new WebSocketClient();

        this.wsClient.onClose = (e) => {
            socketStatus.innerText = "closed (code: " + e.code + ")";
            responseState.innerText = "";
        };
        this.wsClient.onEvent = (data) => {
            subscriptionCount.innerText = String(++this.subCount);
            const subSeq = this.subSeq = data.seq;
            // multiple clients can use the same WebSocket. Use the latest
            if (this.clt == data.clt) {
                subscriptionSeq.innerText   = subSeq ? String(subSeq) : " - ";
                ackElement.innerText        = subSeq ? String(subSeq) : " - ";
                app.events.addSubscriptionEvent(data);
            }
        };
        const error     = await this.wsClient.connect(uri);

        this.subCount   = 0;
        if (error) {
            socketStatus.innerText = "error";
            return error;
        }
        socketStatus.innerHTML = "connected <small>🟢</small>";        
        return null;
    }

    public closeWebsocket  () : void {
        this.wsClient.close();
        this.wsClient = null;
    }

    private addUserToken (jsonRequest: string) {
        const endBracket    = jsonRequest.lastIndexOf("}");
        if (endBracket == -1)
            return jsonRequest;
        const before        = jsonRequest.substring(0, endBracket);
        const after         = jsonRequest.substring(endBracket);
        let   userToken     = JSON.stringify({ user: defaultUser.value, token: defaultToken.value});
        userToken           = userToken.substring(1, userToken.length - 1);
        return `${before},${userToken}${after}`;
    }

    public sendSyncRequest (): void {
        const jsonRequest = app.requestModel.getValue();
        this.sendWebSocketRequest(jsonRequest);
    }

    public async sendWebSocketRequest (jsonRequest: string): Promise<void> {
        const wsClient = this.wsClient;
        if (!wsClient || !wsClient.isOpen()) {
            app.responseModel.setValue(`Request ${this.req} failed. WebSocket not connected`);
            responseState.innerHTML = "";
        } else {
            jsonRequest             = this.addUserToken(jsonRequest);
            const request           = JSON.parse(jsonRequest) as SyncRequest;

            // Enable overrides of WebSocket specific members
            if (request.req !== undefined) { this.req      = request.req; }
            if (request.ack !== undefined) { this.subSeq   = request.ack; }
            if (request.clt !== undefined) { this.clt      = request.clt; }
            
            // Add WebSocket specific members to request
            request.req     = this.req;
            request.ack     = this.subSeq;
            if (this.clt) {
                request.clt     = this.clt;
            }
            responseState.innerHTML = '<span class="spinner"></span>';
            this.requestStart       = new Date().getTime();
            const response          = await wsClient.syncRequest(request);

            const duration          = new Date().getTime() - this.requestStart;
            this.clt                = response.message.clt;
            cltElement.innerText    = this.clt ?? " - ";
            const content           = app.formatJson(app.config.formatResponses, response.json);
            app.responseModel.setValue(content);
            responseState.innerHTML = `· ${duration} ms`;
        }
        this.req++;
        reqIdElement.innerText  =  String(this.req);
    }

    public async postSyncRequest (): Promise<void> {
        let jsonRequest         = app.requestModel.getValue();
        jsonRequest             = this.addUserToken(jsonRequest);
        responseState.innerHTML = '<span class="spinner"></span>';
        const start = new Date().getTime();
        let  duration: number;
        try {
            const response  = await App.postRequest(jsonRequest, "POST");
            let content     = response.text;
            content         = app.formatJson(app.config.formatResponses, content);
            duration        = new Date().getTime() - start;
            app.responseModel.setValue(content);
        } catch(error) {
            duration = new Date().getTime() - start;
            app.responseModel.setValue("POST error: " + error.message);
        }
        responseState.innerHTML = `· ${duration} ms`;
    }

    // --------------------------------------- example requests ---------------------------------------
    public async onExampleChange () : Promise<void> {
        const exampleName = selectExample.value;
        if (exampleName == "") {
            app.requestModel.setValue("");
            return;
        }
        const response = await fetch(exampleName);
        const example = await response.text();
        app.requestModel.setValue(example);
    }

    public async loadExampleRequestList () : Promise<void> {
        // [html - How do I make a placeholder for a 'select' box? - Stack Overflow] https://stackoverflow.com/questions/5805059/how-do-i-make-a-placeholder-for-a-select-box
        let option      = createEl("option");
        option.value    = "";
        option.disabled = true;
        option.selected = true;
        option.hidden   = true;
        option.text     = "Select request ...";
        selectExample.add(option);

        const folder    = './explorer/example-requests';
        const response  = await fetch(folder);
        if (!response.ok)
            return;
        const exampleRequests   = await response.json();
        let   groupPrefix       = "0";
        let   groupCount        = 0;
        for (const example of exampleRequests) {
            if (!example.endsWith(".json"))
                continue;
            const name = example.replace(".sync.json", "");
            if (groupPrefix != name[0]) {
                groupPrefix = name[0];
                groupCount++;
            }
            option = createEl("option");
            option.value                    = folder + "/" + example;
            option.text                     = (groupCount % 2 ? "\xA0\xA0" : "") + name;
            option.style.backgroundColor    = groupCount % 2 ? "#ffffff" : "#eeeeff";
            selectExample.add(option);
        }
    }    
}
