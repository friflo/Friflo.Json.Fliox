import { el, createEl }     from "./types.js";
import { App, app }         from "./index.js";


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
    private connection:     WebSocket;
    private websocketCount  = 0;
    private req             = 1;
    private clt:            string | null  = null;
    private requestStart:   number;
    private subSeq          = 0;
    private subCount        = 0;

    public connectWebsocket (): void {
        if (this.connection) {
            this.connection.close();
            this.connection = null;
        }
        const loc   = window.location;
        const nr    = ("" + (++this.websocketCount)).padStart(3, "0");
        const path  = loc.pathname.substring(0, loc.pathname.lastIndexOf("/") + 1);
        const uri   = `ws://${loc.host}${path}ws-${nr}`;
        // const uri  = `ws://google.com:8080/`; // test connection timeout
        socketStatus.innerHTML = 'connecting <span class="spinner"></span>';
        try {
            const connection = this.connection = new WebSocket(uri);

            connection.onopen = () => {
                socketStatus.innerHTML = "connected <small>🟢</small>";
                console.log('WebSocket connected');
                this.req         = 1;
                this.subCount    = 0;
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
                const duration = new Date().getTime() - this.requestStart;
                const data = JSON.parse(e.data);
                // console.log('server:', e.data);
                switch (data.msg) {
                    case "resp":
                    case "error": {
                        this.clt = data.clt;
                        cltElement.innerText    = this.clt ?? " - ";
                        const content           = app.formatJson(app.config.formatResponses, e.data);
                        app.responseModel.setValue(content);
                        responseState.innerHTML = `· ${duration} ms`;
                        break;
                    }
                    case "ev": {
                        subscriptionCount.innerText = String(++this.subCount);
                        const subSeq = this.subSeq = data.seq;
                        // multiple clients can use the same WebSocket. Use the latest
                        if (this.clt == data.clt) {
                            subscriptionSeq.innerText   = subSeq ? String(subSeq) : " - ";
                            ackElement.innerText        = subSeq ? String(subSeq) : " - ";
                        }
                        break;
                    }
                }
            };
        } catch (err) {
            socketStatus.innerText = "connect failed: err";
            return;
        }
    }

    public closeWebsocket  () : void {
        this.connection.close();
    }

    private addUserToken (jsonRequest: string) {
        const endBracket  = jsonRequest.lastIndexOf("}");
        if (endBracket == -1)
            return jsonRequest;
        const before      = jsonRequest.substring(0, endBracket);
        const after       = jsonRequest.substring(endBracket);
        let   userToken   = JSON.stringify({ user: defaultUser.value, token: defaultToken.value});
        userToken       = userToken.substring(1, userToken.length - 1);
        return `${before},${userToken}${after}`;
    }

    public sendSyncRequest (): void {
        const connection = this.connection;
        if (!connection || connection.readyState != 1) { // 1 == OPEN {
            app.responseModel.setValue(`Request ${this.req} failed. WebSocket not connected`);
            responseState.innerHTML = "";
        } else {
            let jsonRequest = app.requestModel.getValue();
            jsonRequest = this.addUserToken(jsonRequest);
            try {
                const request     = JSON.parse(jsonRequest);
                if (request) {
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
                }
                jsonRequest = JSON.stringify(request);                
            } catch { }
            responseState.innerHTML = '<span class="spinner"></span>';
            connection.send(jsonRequest);
            this.requestStart = new Date().getTime();
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
            let content     = await response.text;
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

        const folder    = './example-requests';
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
