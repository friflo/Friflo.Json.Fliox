export class WebSocketResponse {
}
class WebSocketRequest {
    constructor() {
        this.promise = new Promise((resolve, reject) => {
            this.resolve = resolve;
            this.reject = reject;
        });
    }
}
export class UserToken {
}
export class WebSocketClient {
    constructor(getUserToken) {
        this.clt = null; // client id
        this.req = 1; // incrementing request id. Starts with 1 for every new wsClient
        this.requests = new Map();
        this.ackTimer = null;
        this.ackTimePending = false;
        this.lastEventSeq = 0; // last received event seq. Used to acknowledge received the event via SyncRequest.ack
        this.onClose = (e) => { console.log(`onClose. code ${e.code}`); };
        this.onEvents = (ev) => { console.log(`onEvent. ev: ${ev}`); };
        this.onRecvError = (error) => { console.log(`onRecvError. error: ${error}`); };
        this.getUserToken = getUserToken;
    }
    getReqId() {
        return this.req;
    }
    isOpen() {
        var _a;
        return ((_a = this.webSocket) === null || _a === void 0 ? void 0 : _a.readyState) == 1; // 1 == OPEN
    }
    connect(uri) {
        return new Promise((resolve, reject) => {
            const connection = this.webSocket = new WebSocket(uri);
            connection.onopen = () => {
                console.log('WebSocket connected');
                resolve(null);
            };
            connection.onclose = (e) => {
                const msg = `WebSocket closed. code: ${e.code}`;
                console.log(msg);
                this.onClose(e);
                this.rejectPendingRequests(msg);
            };
            connection.onerror = () => {
                const msg = `WebSocket error - readyState: ${connection.readyState}`;
                console.log(msg);
                reject(msg);
                this.rejectPendingRequests(msg);
            };
            connection.onmessage = (e) => {
                const end = performance.now();
                const json = e.data;
                const message = JSON.parse(json);
                // console.log('server:', data);
                switch (message.msg) {
                    case "resp":
                    case "error": {
                        const reqId = message.req;
                        if (!reqId) {
                            this.onRecvError(`missing field 'req'. was: ${json}`);
                            return;
                        }
                        const request = this.requests.get(reqId);
                        if (!request) {
                            this.onRecvError(`request not found. req: ${reqId}`);
                            return;
                        }
                        this.requests.delete(reqId);
                        if (message.clt) {
                            this.clt = message.clt; // ProtocolResponse.clt is set by Host if not set in SynRequest
                        }
                        request.resolve({ json: json, message: message, start: request.start, end: end });
                        break;
                    }
                    case "ev": {
                        this.lastEventSeq = message.seq;
                        this.onEvents(message);
                        if (!this.ackTimePending) {
                            this.ackTimePending = true;
                            this.ackTimer = setTimeout(() => this.acknowledgeEvents(), 1000);
                        }
                        break;
                    }
                    default:
                        this.onRecvError(`received invalid message: ${json}`);
                }
            };
        });
    }
    acknowledgeEvents() {
        // console.log(`acknowledgeEvents. ack: ${this.lastEventSeq}`);
        // acknowledge event by sending a SyncRequest with SyncRequest.ack set to the last received seq
        const syncRequest = { msg: "sync", tasks: [], info: "acknowledge event" };
        this.syncRequest(syncRequest);
    }
    rejectPendingRequests(error) {
        for (const requests of this.requests.values()) {
            requests.reject(error);
        }
        this.requests.clear();
    }
    close() {
        var _a;
        (_a = this.webSocket) === null || _a === void 0 ? void 0 : _a.close();
        this.webSocket = null;
    }
    async syncRequest(request) {
        if (!this.isOpen) {
            throw "WebSocket not connect";
        }
        const reqId = this.req++;
        request.req = reqId;
        request.ack = this.lastEventSeq;
        if (this.clt) {
            request.clt = this.clt;
        }
        const userToken = this.getUserToken();
        if (!request.user)
            request.user = userToken.user;
        if (!request.token)
            request.token = userToken.token;
        const jsonRequest = JSON.stringify(request);
        const wsRequest = new WebSocketRequest();
        if (this.requests.has(reqId)) {
            throw `req id already in use: ${reqId}`;
        }
        this.requests.set(reqId, wsRequest);
        if (this.ackTimePending) {
            this.ackTimePending = false;
            clearTimeout(this.ackTimer);
            this.ackTimer = null;
        }
        wsRequest.start = performance.now();
        this.webSocket.send(jsonRequest);
        return wsRequest.promise;
    }
}
//# sourceMappingURL=websocket.js.map