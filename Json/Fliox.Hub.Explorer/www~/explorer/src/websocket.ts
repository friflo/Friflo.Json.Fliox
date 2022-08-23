import { ErrorResponse, SyncRequest, SyncResponse, ProtocolMessage_Union, EventMessage } from "../../../../../Json.Tests/assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol";


export class WebSocketResponse {
    json:       string;
    message:    SyncResponse | ErrorResponse;
    start:      number;
    end:        number;
}

class WebSocketRequest {
    promise:    Promise<WebSocketResponse>;
    start:      number;
    resolve:    (value: WebSocketResponse | PromiseLike<WebSocketResponse>) => void;
    reject:     (reason?: any)                                              => void;

    constructor() {
        this.promise = new Promise<WebSocketResponse>((resolve, reject) => {
            this.resolve    = resolve;
            this.reject     = reject;
        });
    }
}

export class WebSocketClient {

    private webSocket:  WebSocket;
    private requests    = new Map<number, WebSocketRequest>();

    public  onClose:        (e: CloseEvent)     => void = (e)        => { console.log(`onClose. code ${e.code}`); };
    public  onEvents:       (ev: EventMessage)  => void = (ev)       => { console.log(`onEvent. ev: ${ev}`); };
    public  onRecvError:    (error: string)     => void = (error)    => { console.log(`onRecvError. error: ${error}`); };


    public isOpen() : boolean {
        return this.webSocket?.readyState == 1;   // 1 == OPEN
    }
    
    public connect(uri: string) : Promise<string> {
        return new Promise<string>((resolve, reject) =>
        {
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
            connection.onmessage = (e: MessageEvent) => {
                const end       = performance.now();
                const json      = e.data;
                const message   = JSON.parse(json) as ProtocolMessage_Union;
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
                        request.resolve({json: json, message: message, start: request.start, end: end});
                        break;
                    }
                    case "ev":
                        this.onEvents(message);
                        break;
                    default:
                        this.onRecvError(`received invalid message: ${json}`);
                }
            };
        });
    }

    private rejectPendingRequests(error: string) {
        for (const requests of this.requests.values()) {
            requests.reject(error);
        }
        this.requests.clear();
    }

    public close() : void {
        this.webSocket.close();
        this.webSocket = null;
    }

    public async syncRequest(request: SyncRequest) : Promise<WebSocketResponse> {
        const reqId = request.req;
        if (!reqId) {
            throw `missing request property: 'req'`;
        }
        const jsonRequest       = JSON.stringify(request);
        const wsRequest         = new WebSocketRequest ();
        if (this.requests.has(reqId)) {
            throw `req id already in use: ${reqId}`;
        }
        this.requests.set(reqId, wsRequest);
        wsRequest.start         = performance.now();
        this.webSocket.send(jsonRequest);

        return wsRequest.promise;
    }
}
