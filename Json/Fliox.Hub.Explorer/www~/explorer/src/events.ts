import { DbContainers, DbMessages } from "../../../../../Json.Tests/assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster.js";
import { SyncEvent, SyncRequest } from "../../../../../Json.Tests/assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol.js";
import { EntityChange, SubscribeChanges, SubscribeMessage } from "../../../../../Json.Tests/assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol.Tasks.js";
import { ClusterTree }  from "./components.js";
import { el }           from "./types.js";
import { app }          from "./index.js";

const subscriptionTree  = el("subscriptionTree");
const scrollToEnd       = el("scrollToEnd")     as HTMLInputElement;
const prettifyEvents    = el("prettifyEvents")  as HTMLInputElement;
const subFilter         = el("subFilter")       as HTMLSpanElement;
const logCount          = el("logCount")        as HTMLSpanElement;
const eventSrcFilter    = el("eventSrcFilter")  as HTMLInputElement;
const eventSeqStart     = el("eventSeqStart")   as HTMLInputElement;
const eventSeqEnd       = el("eventSeqEnd")     as HTMLInputElement;

export const eventsInfo = `
    info

* Subscribe to container changes and messages by clicking the 'sub' tag of a tree item
    Try out by generating 'upsert' events in the 'Explorer' tab
    - select a container
    - select an entity in the container and click 'Save'

* Show subscription events by clicking its tree entry in the 'Pub-Sub' tab

A WebSocket is used to send push events to the client.
In case of a WebSocket connection loss the host store events targeted to the client
and send them after the client reconnects.
    Try out a disconnect using the 'Playground' tab
    - Click 'close'
    - Generate some 'upsert' as described above
    - Click 'connect' in 'Playground' tab
    - Click 'send' - any request
The events generated during the disconnect are now available in the 'Pub-Sub' tab.`;

function firstKV(key: string, value: any) {
    if (value === undefined)
        return "";
    return `"${key}":${JSON.stringify(value)}`;    
}

function KV(key: string, value: any) {
    if (value === undefined)
        return "";
    return `, "${key}":${JSON.stringify(value)}`;    
}

class ContainerSub {
    subscribed: boolean;
    creates:    number;
    upserts:    number;
    deletes:    number;
    patches:    number;
    error:      string;

    constructor() {
        this.subscribed = false;
        this.creates    = 0;
        this.upserts    = 0;
        this.deletes    = 0;
        this.patches    = 0;
        this.error      = null;
    }
}

class MessageSub {
    subscribed: boolean;
    events:     number;
    error:      string;

    constructor() {
        this.subscribed = false;
        this.events     = 0;
        this.error      = null;
    }
}

class DatabaseSub {
    containerSubs   : { [container: string] : ContainerSub} = {};
    messageSubs     : { [message:   string] : MessageSub} = {};
}

class SubEvent {
    readonly    db:         string;
    readonly    msg:        string;
    readonly    seq:        number;
    readonly    src:        string;
    readonly    messages:   string[];
    readonly    containers: string[];

    public filterSeqSrc(users: string[], seqStart: number, seqEnd: number) : boolean {
        return  (this.seq >= seqStart)  && 
                (this.seq <= seqEnd)    &&
                (users == null || users.includes(this.src));
    }

    private static readonly  internNames : { [name: string]: string} = {};

    private static internName (name: string) {
        const intern = SubEvent.internNames[name];
        if (intern)
            return intern;
        SubEvent.internNames[name] = name;
        return name;
    }

    constructor(msg: string, ev: SyncEvent) {
        this.msg                    = SubEvent.internName(msg);
        this.db                     = SubEvent.internName(ev.db);
        this.seq                    = ev.seq;
        this.src                    = SubEvent.internName(ev.src);
        const messages:   string[]  = []; 
        const containers: string[]  = [];
        for (const task of ev.tasks) {
            switch (task.task) {
                case "message":
                case "command": {
                    const msgName = SubEvent.internName(task.name);
                    messages.push(msgName);
                    break;
                }
                case "create":
                case "upsert":
                case "delete":
                case "merge": {
                    const containerName = SubEvent.internName(task.container);
                    containers.push(containerName);
                    break;
                }
            }
        }
        if (messages.length > 0) {
            this.messages = messages;
        }
        if (containers.length > 0) {
            this.containers = containers;
        }
    }
}

// ----------------------------------------------- Events -----------------------------------------------
export class Events
{
    private readonly    clusterTree:    ClusterTree;
    private readonly    databaseSubs:   { [database: string] : DatabaseSub } = {}
    private readonly    subEvents:      SubEvent[] = [];
    private             filter:         EventFilter;
    private             userFilter:     string[] = null;
    private             seqStart    = 0;
    private             seqEnd      = Number.MAX_SAFE_INTEGER;
    private             logCount    = 0;


    public constructor() {
        this.clusterTree    = new ClusterTree();
    }

    public selectAllEvents(element: HTMLElement) : void {
        const on = this.clusterTree.toggleTreeElement(element);
        const filter = on ? new EventFilter(true, null, true, null, true, null) : EventFilter.None();
        this.setEditorLog(filter);
    }

    public initEvents(dbContainers: DbContainers[], dbMessages: DbMessages[]) : void {

        const tree      = this.clusterTree;
        const ulCluster = tree.createClusterUl(dbContainers, dbMessages);
        tree.onSelectDatabase = (elem: HTMLElement, classList: DOMTokenList, databaseName: string) => {
            if (classList.length > 0) {
                return;
            }
            const on = tree.toggleTreeElement(elem);
            const filter = on ? new EventFilter(false, databaseName, true, null, true, null) : EventFilter.None();
            this.setEditorLog(filter);
        };
        tree.onSelectContainer = (elem: HTMLElement, classList: DOMTokenList, databaseName: string, containerName: string) => {
            if (classList.length > 0) {
                this.toggleContainerSub(databaseName, containerName);
                return;
            }
            const on = tree.toggleTreeElement(elem);
            const filter = on ? new EventFilter(false, databaseName, false, containerName, false, null) : EventFilter.None();
            this.setEditorLog(filter);
        };
        tree.onSelectMessage = (elem: HTMLElement, classList: DOMTokenList, databaseName: string, messageName: string) => {
            if (classList.length > 0) {
                this.toggleMessageSub(databaseName, messageName);
                return;
            }
            const on = tree.toggleTreeElement(elem);
            const filter = on ? new EventFilter(false, databaseName, false, null, false, messageName) : EventFilter.None();
            this.setEditorLog(filter);
        };
        tree.onSelectMessages = (elem: HTMLElement, classList: DOMTokenList, databaseName: string) => {
            if (classList.length > 0) {
                this.toggleMessageSub(databaseName, "*");
                return;
            }
            const on = tree.toggleTreeElement(elem);
            const filter = on ? new EventFilter(false, databaseName, false, null, true, null) : EventFilter.None();
            this.setEditorLog(filter);
        };
        ulCluster.style.margin = "0";
        subscriptionTree.appendChild(ulCluster);
        this.filter = EventFilter.None();
        const firstDb   = ulCluster.children[0] as HTMLElement;
        if (firstDb) {
            firstDb.classList.add("active");
        }

        for (const database of dbContainers) {
            const databaseSub = new DatabaseSub();
            this.databaseSubs[database.id] = databaseSub;
            for (const container of database.containers) {
                databaseSub.containerSubs[container] = new ContainerSub();
            }
            const dbMessage = dbMessages.find(entry => entry.id == database.id);
            databaseSub.messageSubs["*"] = new MessageSub();
            for (const command of dbMessage.commands) {
                databaseSub.messageSubs[command] = new MessageSub();
            }
            for (const message of dbMessage.messages) {
                databaseSub.messageSubs[message] = new MessageSub();
            }
        }
        eventSrcFilter.onblur      = () =>                  { this.setLogFilter(); };
        eventSrcFilter.onkeydown   = (ev: KeyboardEvent) => { if (ev.key  == 'Enter') this.setLogFilter(); };

        eventSeqStart.onblur      = () =>                   { this.setLogFilter(); };
        eventSeqStart.onkeydown   = (ev: KeyboardEvent) =>  { if (ev.key  == 'Enter') this.setLogFilter(); };

        eventSeqEnd.onblur      = () =>                     { this.setLogFilter(); };
        eventSeqEnd.onkeydown   = (ev: KeyboardEvent) =>    { if (ev.key  == 'Enter') this.setLogFilter(); };

    }

    private setLogFilter() {
        const srcValue  = eventSrcFilter.value;
        this.userFilter = srcValue  ? srcValue.split(",") : null;

        const seqStart  = eventSeqStart.value;
        this.seqStart   = seqStart  ? parseInt(seqStart) : 0;

        const seqEnd    = eventSeqEnd.value;
        this.seqEnd     = seqEnd    ? parseInt(seqEnd) : Number.MAX_SAFE_INTEGER;

        this.setEditorLog(this.filter);
    }

    public clearAllEvents() : void {
        app.eventsEditor.setValue("");
    }

    private static event2String (ev: SyncEvent, format: boolean) : string {
        if (!format) {
            return JSON.stringify(ev, null, 4);
        }
        // const tasksJson = ev.tasks.map(task => JSON.stringify(task));
        const tasksJson: string[] = [];
        for (const task of ev.tasks) {
            switch (task.task) {
                case "message":
                case "command": {
                    const json = JSON.stringify(task);
                    tasksJson.push(json);
                    break;
                }
                case "create":
                case "upsert": {
                    const entities      = task.entities.map(entity => JSON.stringify(entity));
                    const entitiesJson  = entities.join(",\n            ");
                    const json = `{"task":"${task.task}"${KV("container", task.container)}${KV("keyName", task.keyName)}, "entities":[
            ${entitiesJson}
        ]}`;
                    tasksJson.push(json);
                    break;
                }
                case "delete": {
                    const ids           = task.ids.map(entity => JSON.stringify(entity));
                    const idsJson       = ids.join(",\n            ");
                    const json = `{"task":"${task.task}"${KV("container", task.container)}, "ids":[
            ${idsJson}
        ]}`;
                    tasksJson.push(json);
                    break;
                }
                case "merge": {
                    const patches       = task.patches.map(patch => JSON.stringify(patch));
                    const patchesJson   = patches.join(",\n            ");
                    const json = `{"task":"${task.task}"${KV("container", task.container)}, "patches":[
            ${patchesJson}
        ]}`;
                    tasksJson.push(json);
                    break;
                }
            }
        }
        const tasks = tasksJson.join(",\n        ");
        return `{
    ${firstKV("seq", ev.seq)}${KV("src", ev.src)}${KV("db", ev.db)}${KV("isOrigin", ev.isOrigin)},
    "tasks": [
        ${tasks}
    ]
}`;
    }

    private setEditorLog(filter: EventFilter) {
        this.filter         = filter;
        const filterResult  = filter.filterEvents(this.subEvents, this.userFilter, this.seqStart, this.seqEnd);
        subFilter.innerText = filter.getFilterName();
        this.logCount       = filterResult.eventCount;
        logCount.innerText  = String(this.logCount);

        const editor        = app.eventsEditor;
        editor.setValue(filterResult.logs);
        const pos           = editor.getModel().getPositionAt (filterResult.lastLog);
        editor.revealPositionNearTop(pos);
    }

    public addSubscriptionEvent(ev: SyncEvent) : void {
        const evStr     = Events.event2String (ev, prettifyEvents.checked);
        const msg       = new SubEvent(evStr, ev);
        this.subEvents.push (msg);
        this.updateUI(ev);

        if (!this.filter.match(msg))
            return;
        if (!msg.filterSeqSrc(this.userFilter, this.seqStart, this.seqEnd))
            return;

        this.logCount++;
        logCount.innerText  = String(this.logCount);
        this.addLog(evStr);
    }

    private addLog(evStr: string) {
        const editor    = app.eventsEditor;
        const model     = editor.getModel();
        const value     = model.getValue();
        const length    = value.length;

        if (length == 0) {
            model.setValue("[]");
        } else {
            evStr = value == "[]" ? evStr : `,${evStr}`;
        }
        const endPos    = model.getPositionAt(length);
        const match     = model.findPreviousMatch ("]", endPos, false, true, null, false);
        // const pos       = lastPos;
        const pos       = new monaco.Position(match.range.startLineNumber, match.range.startColumn);
        const range     = new monaco.Range(pos.lineNumber, pos.column, pos.lineNumber, pos.column);

        let callback: monaco.editor.ICursorStateComputer = null;
        if (scrollToEnd.checked) {
            callback = (inverseEditOperations) => {
                const inverseRange = inverseEditOperations[0].range;
                window.setTimeout(() => { 
                    editor.revealRange (inverseRange);
                    const start         = inverseRange.getStartPosition();
                    const startRange    = new monaco.Range (start.lineNumber, start.column + 1, start.lineNumber, start.column + 1);
                    editor.setSelection(startRange);
                    // editor.setSelection(inverseRange);
                }, 1);            
                return null;
            };
        } 
        editor.executeEdits("addSubscriptionEvent", [{ range: range, text: evStr, forceMoveMarkers: true }], callback);
    }

    private updateUI(ev: SyncEvent) {
        const databaseSub = this.databaseSubs[ev.db];
        for (const task of ev.tasks) {
            switch (task.task) {
                case "command":
                case "message": {
                    const allMessageSub = databaseSub.messageSubs["*"];
                    allMessageSub.events++;
                    this.uiMessageText(ev.db, "*", allMessageSub, "event");

                    const messageSub = databaseSub.messageSubs[task.name];
                    messageSub.events++;
                    this.uiMessageText(ev.db, task.name, messageSub, "event");
                    break;
                }
                case "upsert": 
                case "create": {
                    const containerSub = databaseSub.containerSubs[task.container];
                    containerSub.creates += task.entities.length;
                    this.uiContainerText(ev.db, task.container, containerSub, "event");
                    break;
                }
                case "delete": {
                    const containerSub = databaseSub.containerSubs[task.container];
                    containerSub.deletes += task.ids.length;
                    this.uiContainerText(ev.db, task.container, containerSub, "event");
                    break;
                }
                case "merge": {
                    const containerSub = databaseSub.containerSubs[task.container];
                    containerSub.patches += task.patches.length;
                    this.uiContainerText(ev.db, task.container, containerSub, "event");
                    break;
                }
            }
        }
    }

    private async sendSubscriptionRequest(syncRequest: SyncRequest) : Promise<string>{
        const error = await app.playground.connect();
        if (error)
            return error;
        const response = await app.playground.sendWebSocketRequest(syncRequest);
        const message   = response.message;
        if (message.msg == "error") {
            return message.message;
        }
        const task =  message.tasks[0];
        if (task.task == "error") {
            return task.message;
        }
        return null;
    }

    // ----------------------------------- container subs -----------------------------------
    public async toggleContainerSub(databaseName: string, containerName: string) : Promise<void> {
        const containerSubs = this.databaseSubs[databaseName].containerSubs;
        const containerSub  = containerSubs[containerName];
        containerSub.error  = null;
        let changes: EntityChange[] = [];
        if (!containerSub.subscribed) {
            containerSub.subscribed = true;
            changes = ["create", "upsert", "merge", "delete"];
            this.uiContainerSubscribed(databaseName, containerName, true);
            this.uiContainerText(databaseName, containerName, containerSub, null);
        } else {
            containerSub.subscribed = false;
            this.uiContainerSubscribed(databaseName, containerName, false);
            this.uiContainerText(databaseName, containerName, containerSub, null);
        }
        const subscribeChanges: SubscribeChanges = { task: "subscribeChanges", changes: changes, container: containerName };
        const syncRequest:      SyncRequest      = { msg: "sync", database: databaseName, tasks: [subscribeChanges] };

        const err = await this.sendSubscriptionRequest(syncRequest);
        if (err) {
            containerSub.error = err;
            this.uiContainerText(databaseName, containerName, containerSub, null);
        }
    }

    private uiContainerSubscribed(databaseName: string, containerName: string, enable: boolean) {
        if (enable) {
            this.clusterTree.addContainerClass(databaseName, containerName, "subscribed");
            app. clusterTree.addContainerClass(databaseName, containerName, "subscribed");
            return;
        }
        this.clusterTree.removeContainerClass(databaseName, containerName, "subscribed");
        app. clusterTree.removeContainerClass(databaseName, containerName, "subscribed");
    }

    private uiContainerText(databaseName: string, containerName: string, cs: ContainerSub, trigger: "event") {
        if (cs.error) {
            const error = cs.subscribed ? cs.error : null;
            this.clusterTree.setContainerError(databaseName, containerName, error);
            app. clusterTree.setContainerError(databaseName, containerName, error);
            return;
        }
        let values: string[] = null;
        if (cs.subscribed || cs.creates + cs.upserts + cs.deletes + cs.patches > 0) {
            values = [`${cs.creates + cs.upserts}`, `${cs.deletes}`, `${cs.patches}`];
        }
        this.clusterTree.setContainerText(databaseName, containerName, values, trigger);
        app. clusterTree.setContainerText(databaseName, containerName, values, trigger);
    }

    // ----------------------------------- message subs -----------------------------------
    public async toggleMessageSub(databaseName: string, messageName: string) : Promise<void> {
        const messageSubs   = this.databaseSubs[databaseName].messageSubs;
        const messageSub    = messageSubs[messageName];
        messageSub.error    = null;
        let remove = false;
        if (!messageSub.subscribed) {
            messageSub.subscribed   = true;
            this.uiMessageSubscribed(databaseName, messageName, true);
            this.uiMessageText(databaseName, messageName, messageSub, null);
        } else {
            remove = true;
            messageSub.subscribed = false;
            this.uiMessageSubscribed(databaseName, messageName, false);
            this.uiMessageText(databaseName, messageName, messageSub, null);
        }
        const subscribeMessage: SubscribeMessage = { task: "subscribeMessage", remove: remove, name: messageName };
        const syncRequest:      SyncRequest      = { msg: "sync", database: databaseName, tasks: [subscribeMessage] };

        const err = await this.sendSubscriptionRequest(syncRequest);
        if (err) {
            messageSub.error = err;
            this.uiMessageText(databaseName, messageName, messageSub, null);
        }
    }

    private uiMessageSubscribed(databaseName: string, message: string, enable: boolean) {
        if (enable) {
            this.clusterTree.addMessageClass(databaseName, message, "subscribed");
            return;
        }
        this.clusterTree.removeMessageClass(databaseName, message, "subscribed");
    }

    private uiMessageText(databaseName: string, messageName: string, cs: MessageSub, trigger: "event") {
        if (cs.error) {
            this.clusterTree.setMessageError(databaseName, messageName, cs.subscribed ? cs.error : null);
            return;
        }
        let text = "";
        if (cs.subscribed || cs.events > 0) {
            text = `${cs.events}`;
        }
        this.clusterTree.setMessageText(databaseName, messageName, text, trigger);
    }
}

// ----------------------------------- EventFilter -----------------------------------
type FilterResult = {
    readonly logs:          string;
    readonly lastLog:       number;
    readonly eventCount:    number;
}

class EventFilter {
    private readonly allEvents:     boolean;
    private readonly db:            string;
    private readonly allDbEvents:   boolean;
    private readonly container:     string;
    private readonly message:       string;
    private readonly allMessages:   boolean;

    constructor(allEvents: boolean, db: string, allDbEvents: boolean, container: string, allMessages: boolean, message: string) {
        this.allEvents      = allEvents;
        this.db             = db;
        this.allDbEvents    = allDbEvents;
        this.allMessages    = allMessages;
        this.message        = message;
        this.container      = container;
    }

    public static None() : EventFilter {
        return new EventFilter(false, null, false, null, false, null);
    }

    public match(ev: SubEvent) : boolean {
        if (this.allEvents)
            return true;
        if (ev.db != this.db)
            return false;
        const containers = ev.containers;
        if (this.allDbEvents && containers?.length > 0)
            return true;
        if (containers?.includes(this.container))
            return true;
        const messages = ev.messages;
        if (this.allMessages && messages?.length > 0)
            return true;
        if (messages?.includes(this.message))
            return true;
        return false;
    }

    public getFilterName() : string {
        if (this.allEvents)
            return "all";
        const name = this.db;
        if (name == null)
            return "none";
        if (this.allDbEvents)
            return name;
        if (this.allMessages)
            return `${name} · messages`;
        if (this.container)
            return `${name} / ${this.container}`;
        return `${name} · ${this.message}`;
    }

    public filterEvents(events: SubEvent[], users: string[], seqStart: number, seqEnd: number) : FilterResult {
        const matches: string[] = [];
        for (const ev of events) {
            if (!ev.filterSeqSrc(users, seqStart, seqEnd))
                continue;
            if (!this.match(ev)) 
                continue;
            matches.push(ev.msg);
        }
        const logs      = `[${matches.join(',')}]`;
        const lastLog   = matches.length == 0 ? 0 : logs.length - matches[matches.length - 1].length;
        const result: FilterResult = {
            logs:       logs,
            lastLog:    lastLog,
            eventCount: matches.length,
        };
        return result;
    }
}
