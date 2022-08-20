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
export class WebSocketClient {
    constructor() {
        this.requests = {};
        this.onClose = (e) => { console.log(`onClose. code ${e.code}`); };
        this.onEvent = (ev) => { console.log(`onEvent. ev: ${ev}`); };
        this.onRecvError = (error) => { console.log(`onRecvError. error: ${error}`); };
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
                console.log(`WebSocket closed. code: ${e.code}`);
                this.onClose(e);
            };
            connection.onerror = (error) => {
                console.log('WebSocket error ' + error);
                reject(error);
            };
            connection.onmessage = (e) => {
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
                        const request = this.requests[reqId];
                        if (!request) {
                            this.onRecvError(`request not found. req: ${reqId}`);
                            return;
                        }
                        request.resolve({ json: json, message: message });
                        break;
                    }
                    case "ev":
                        this.onEvent(message);
                        break;
                    default:
                        this.onRecvError(`received invalid message: ${json}`);
                }
            };
        });
    }
    close() {
        this.webSocket.close();
        this.webSocket = null;
    }
    async syncRequest(request) {
        const reqId = request.req;
        if (!reqId) {
            throw `missing request property: 'req'`;
        }
        const jsonRequest = JSON.stringify(request);
        const wsRequest = new WebSocketRequest();
        if (this.requests[reqId]) {
            throw `req id already in use: ${reqId}`;
        }
        this.requests[reqId] = wsRequest;
        this.webSocket.send(jsonRequest);
        return wsRequest.promise;
    }
}
//# sourceMappingURL=websocket.js.map