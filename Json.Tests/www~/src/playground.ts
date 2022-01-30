import { el }           from "./types.js"
import { App, app }     from "./index.js";


const responseState     = el("response-state");
const subscriptionCount = el("subscriptionCount");
const subscriptionSeq   = el("subscriptionSeq");
const socketStatus      = el("socketStatus");
const reqIdElement      = el("req");
const ackElement        = el("ack");
const cltElement        = el("clt");
//
const defaultUser       = el("user")            as HTMLInputElement;
const defaultToken      = el("token")           as HTMLInputElement;


// --- WebSocket ---
let connection:         WebSocket;
let websocketCount      = 0;
let req                 = 1;
let clt: string | null  = null;
let requestStart: number;
let subSeq              = 0;
let subCount            = 0;


// ----------------------------------------------- Playground -----------------------------------------------
export class Playground
{
    connectWebsocket () {
        if (connection) {
            connection.close();
            connection = null;
        }
        const loc     = window.location;
        const nr      = ("" + (++websocketCount)).padStart(3, "0");
        const uri     = `ws://${loc.host}/ws-${nr}`;
        // const uri  = `ws://google.com:8080/`; // test connection timeout
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
            const duration = new Date().getTime() - requestStart;
            const data = JSON.parse(e.data);
            // console.log('server:', e.data);
            switch (data.msg) {
            case "resp":
            case "error":
                clt = data.clt;
                cltElement.innerText    = clt ?? " - ";
                const content           = app.formatJson(app.config.formatResponses, e.data);
                app.responseModel.setValue(content)
                responseState.innerHTML = `· ${duration} ms`;
                break;
            case "ev":
                subscriptionCount.innerText = String(++subCount);
                subSeq = data.seq;
                // multiple clients can use the same WebSocket. Use the latest
                if (clt == data.clt) {
                    subscriptionSeq.innerText   = subSeq ? String(subSeq) : " - ";
                    ackElement.innerText        = subSeq ? String(subSeq) : " - ";
                }
                break;
            }
        };
    }

    closeWebsocket  () {
        connection.close();
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

    sendSyncRequest () {
        if (!connection || connection.readyState != 1) { // 1 == OPEN {
            app.responseModel.setValue(`Request ${req} failed. WebSocket not connected`)
            responseState.innerHTML = "";
        } else {
            let jsonRequest = app.requestModel.getValue();
            jsonRequest = this.addUserToken(jsonRequest);
            try {
                const request     = JSON.parse(jsonRequest);
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
        reqIdElement.innerText  =  String(req);
    }

    async postSyncRequest () {
        let jsonRequest         = app.requestModel.getValue();
        jsonRequest             = this.addUserToken(jsonRequest);
        responseState.innerHTML = '<span class="spinner"></span>';
        let start = new Date().getTime();
        let duration: number;
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
}
