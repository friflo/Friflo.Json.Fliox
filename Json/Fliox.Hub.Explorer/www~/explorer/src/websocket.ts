import { ErrorResponse, SyncRequest, SyncResponse, ProtocolMessage_Union, EventMessage } from "Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol";


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

export class UserToken {
    user:   string;
    token:  string;
}

export class WebSocketClient
{
    public  clt:            string | null  = null;  // client id
    private webSocket:      WebSocket;
    private req             = 1;                    // incrementing request id. Starts with 1 for every new wsClient
    private requests        = new Map<number, WebSocketRequest>();
    private ackTimer:       NodeJS.Timeout = null;
    private ackTimePending  = false;
    private lastEventSeq    = 0;                    // last received event seq. Used to acknowledge received the event via SyncRequest.ack


    private readonly    getUserToken:   ()                  => UserToken;
    public              onClose:        (e: CloseEvent)     => void = (e)        => { console.log(`onClose. code ${e.code}`); }
    public              onEvents:       (ev: EventMessage)  => void = (ev)       => { console.log(`onEvent. ev: ${ev}`); }
    public              onRecvError:    (error: string)     => void = (error)    => { console.log(`onRecvError. error: ${error}`); }

    constructor(getUserToken: () => UserToken) {
        this.getUserToken = getUserToken;
    }

    public getReqId() : number {
        return this.req;
    }

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
                        if (message.clt) {
                            this.clt = message.clt; // ProtocolResponse.clt is set by Host if not set in SynRequest
                        }
                        request.resolve({json: json, message: message, start: request.start, end: end});
                        break;
                    }
                    case "ev": {
                        this.lastEventSeq   = message.seq;
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

    private acknowledgeEvents() {
        // console.log(`acknowledgeEvents. ack: ${this.lastEventSeq}`);
        // acknowledge event by sending a SyncRequest with SyncRequest.ack set to the last received seq
        const syncRequest: SyncRequest = { msg: "sync", tasks: [], info: "acknowledge event" };
        this.syncRequest(syncRequest);
    }

    private rejectPendingRequests(error: string) {
        for (const requests of this.requests.values()) {
            requests.reject(error);
        }
        this.requests.clear();
    }

    public close() : void {
        this.webSocket?.close();
        this.webSocket = null;
    }

    public async syncRequest(request: SyncRequest) : Promise<WebSocketResponse> {
        if (!this.isOpen) {
            throw "WebSocket not connect";
        }
        const reqId         = this.req++;
        request.req         = reqId;
        request.ack         = this.lastEventSeq;
        if (this.clt) {
            request.clt         = this.clt;
        }
        const userToken     = this.getUserToken();
        if (!request.user)  request.user    = userToken.user;
        if (!request.token) request.token   = userToken.token;

        const jsonRequest   = JSON.stringify(request);
        const wsRequest     = new WebSocketRequest ();
        if (this.requests.has(reqId)) {
            throw `req id already in use: ${reqId}`;
        }
        this.requests.set(reqId, wsRequest);
        if (this.ackTimePending) {
            this.ackTimePending = false;
            clearTimeout(this.ackTimer);
            this.ackTimer       = null;
        }
        wsRequest.start         = performance.now();
        this.webSocket.send(jsonRequest);

        return wsRequest.promise;
    }
}
