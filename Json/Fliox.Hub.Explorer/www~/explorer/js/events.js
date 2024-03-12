import { ClusterTree } from "./components.js";
import { el } from "./types.js";
import { app } from "./index.js";
const subscriptionTree = el("subscriptionTree");
const scrollToEnd = el("scrollToEnd");
const prettifyEvents = el("prettifyEvents");
const subFilter = el("subFilter");
const logCount = el("logCount");
const eventSrcFilter = el("eventSrcFilter");
const eventSeqStart = el("eventSeqStart");
const eventSeqEnd = el("eventSeqEnd");
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
function firstKV(key, value) {
    if (value === undefined)
        return "";
    return `"${key}":${JSON.stringify(value)}`;
}
function KV(key, value) {
    if (value === undefined)
        return "";
    return `, "${key}":${JSON.stringify(value)}`;
}
class ContainerSub {
    constructor() {
        this.subscribed = false;
        this.creates = 0;
        this.upserts = 0;
        this.deletes = 0;
        this.patches = 0;
        this.error = null;
    }
}
class MessageSub {
    constructor() {
        this.subscribed = false;
        this.events = 0;
        this.error = null;
    }
}
class DatabaseSub {
    constructor() {
        this.containerSubs = {};
        this.messageSubs = {};
    }
}
class SubEvent {
    filterSeqSrc(users, seqStart, seqEnd) {
        return (this.seq >= seqStart) &&
            (this.seq <= seqEnd) &&
            (users == null || users.includes(this.usr));
    }
    static internName(name) {
        const intern = SubEvent.internNames[name];
        if (intern)
            return intern;
        SubEvent.internNames[name] = name;
        return name;
    }
    constructor(msg, db, ev, seq) {
        this.msg = SubEvent.internName(msg);
        this.db = SubEvent.internName(db);
        this.seq = seq;
        this.usr = SubEvent.internName(ev.usr);
        const messages = [];
        const containers = [];
        for (const task of ev.tasks) {
            switch (task.task) {
                case "msg":
                case "cmd": {
                    const msgName = SubEvent.internName(task.name);
                    messages.push(msgName);
                    break;
                }
                case "create":
                case "upsert":
                case "delete":
                case "merge": {
                    const containerName = SubEvent.internName(task.cont);
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
SubEvent.internNames = {};
// ----------------------------------------------- Events -----------------------------------------------
export class Events {
    constructor() {
        this.databaseSubs = {};
        this.subEvents = [];
        this.defaultDB = null;
        this.userFilter = null;
        this.seqStart = 0;
        this.seqEnd = Number.MAX_SAFE_INTEGER;
        this.logCount = 0;
        this.clusterTree = new ClusterTree();
    }
    selectAllEvents(element) {
        const on = this.clusterTree.toggleTreeElement(element);
        const filter = on ? new EventFilter(true, null, true, null, true, null) : EventFilter.None();
        this.setEditorLog(filter);
    }
    initEvents(dbContainers, dbMessages) {
        const tree = this.clusterTree;
        const ulCluster = tree.createClusterUl(dbContainers, dbMessages);
        tree.onSelectDatabase = (elem, classList, databaseName) => {
            if (classList.length > 0) {
                return;
            }
            const on = tree.toggleTreeElement(elem);
            const filter = on ? new EventFilter(false, databaseName, true, null, true, null) : EventFilter.None();
            this.setEditorLog(filter);
        };
        tree.onSelectContainer = (elem, classList, databaseName, containerName) => {
            if (classList.length > 0) {
                this.toggleContainerSub(databaseName, containerName);
                return;
            }
            const on = tree.toggleTreeElement(elem);
            const filter = on ? new EventFilter(false, databaseName, false, containerName, false, null) : EventFilter.None();
            this.setEditorLog(filter);
        };
        tree.onSelectMessage = (elem, classList, databaseName, messageName) => {
            if (classList.length > 0) {
                this.toggleMessageSub(databaseName, messageName);
                return;
            }
            const on = tree.toggleTreeElement(elem);
            const filter = on ? new EventFilter(false, databaseName, false, null, false, messageName) : EventFilter.None();
            this.setEditorLog(filter);
        };
        tree.onSelectMessages = (elem, classList, databaseName) => {
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
        const firstDb = ulCluster.children[0];
        if (firstDb) {
            firstDb.classList.add("active");
        }
        for (const database of dbContainers) {
            const databaseSub = new DatabaseSub();
            this.databaseSubs[database.id] = databaseSub;
            if (database.defaultDB) {
                this.defaultDB = database.id;
            }
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
        eventSrcFilter.onblur = () => { this.setLogFilter(); };
        eventSrcFilter.onkeydown = (ev) => { if (ev.key == 'Enter')
            this.setLogFilter(); };
        eventSeqStart.onblur = () => { this.setLogFilter(); };
        eventSeqStart.onkeydown = (ev) => { if (ev.key == 'Enter')
            this.setLogFilter(); };
        eventSeqEnd.onblur = () => { this.setLogFilter(); };
        eventSeqEnd.onkeydown = (ev) => { if (ev.key == 'Enter')
            this.setLogFilter(); };
    }
    setLogFilter() {
        const srcValue = eventSrcFilter.value;
        this.userFilter = srcValue ? srcValue.split(",") : null;
        const seqStart = eventSeqStart.value;
        this.seqStart = seqStart ? parseInt(seqStart) : 0;
        const seqEnd = eventSeqEnd.value;
        this.seqEnd = seqEnd ? parseInt(seqEnd) : Number.MAX_SAFE_INTEGER;
        this.setEditorLog(this.filter);
    }
    clearAllEvents() {
        app.eventsEditor.setValue("");
    }
    static event2String(ev, seq, format) {
        if (!format) {
            return JSON.stringify(ev, null, 4);
        }
        // const tasksJson = ev.tasks.map(task => JSON.stringify(task));
        const tasksJson = [];
        for (const task of ev.tasks) {
            switch (task.task) {
                case "msg":
                case "cmd": {
                    const json = JSON.stringify(task);
                    tasksJson.push(json);
                    break;
                }
                case "create":
                case "upsert": {
                    const entities = task.set.map(entity => JSON.stringify(entity));
                    const entitiesJson = entities.join(",\n            ");
                    const json = `{"task":"${task.task}"${KV("cont", task.cont)}${KV("keyName", task.keyName)}, "set":[
            ${entitiesJson}
        ]}`;
                    tasksJson.push(json);
                    break;
                }
                case "delete": {
                    const ids = task.ids.map(entity => JSON.stringify(entity));
                    const idsJson = ids.join(",\n            ");
                    const json = `{"task":"${task.task}"${KV("cont", task.cont)}, "ids":[
            ${idsJson}
        ]}`;
                    tasksJson.push(json);
                    break;
                }
                case "merge": {
                    const patches = task.set.map(patch => JSON.stringify(patch));
                    const patchesJson = patches.join(",\n            ");
                    const json = `{"task":"${task.task}"${KV("cont", task.cont)}, "set":[
            ${patchesJson}
        ]}`;
                    tasksJson.push(json);
                    break;
                }
            }
        }
        const tasks = tasksJson.join(",\n        ");
        return `{
    ${firstKV("_seq", seq)}${KV("usr", ev.usr)}${KV("db", ev.db)}${KV("clt", ev.clt)},
    "tasks": [
        ${tasks}
    ]
}`;
    }
    setEditorLog(filter) {
        this.filter = filter;
        const filterResult = filter.filterEvents(this.subEvents, this.userFilter, this.seqStart, this.seqEnd);
        subFilter.innerText = filter.getFilterName();
        this.logCount = filterResult.eventCount;
        logCount.innerText = String(this.logCount);
        const editor = app.eventsEditor;
        editor.setValue(filterResult.logs);
        const pos = editor.getModel().getPositionAt(filterResult.lastLog);
        editor.revealPositionNearTop(pos);
    }
    addSubscriptionEvent(ev, seq) {
        var _a;
        const db = (_a = ev.db) !== null && _a !== void 0 ? _a : this.defaultDB;
        const evStr = Events.event2String(ev, seq, prettifyEvents.checked);
        const msg = new SubEvent(evStr, db, ev, seq);
        this.subEvents.push(msg);
        this.updateUI(db, ev);
        if (!this.filter.match(msg))
            return;
        if (!msg.filterSeqSrc(this.userFilter, this.seqStart, this.seqEnd))
            return;
        this.logCount++;
        logCount.innerText = String(this.logCount);
        this.addLog(evStr);
    }
    addLog(evStr) {
        const editor = app.eventsEditor;
        const model = editor.getModel();
        const value = model.getValue();
        const length = value.length;
        if (length == 0) {
            model.setValue("[]");
        }
        else {
            evStr = value == "[]" ? evStr : `,${evStr}`;
        }
        const endPos = model.getPositionAt(length);
        const match = model.findPreviousMatch("]", endPos, false, true, null, false);
        // const pos       = lastPos;
        const pos = new monaco.Position(match.range.startLineNumber, match.range.startColumn);
        const range = new monaco.Range(pos.lineNumber, pos.column, pos.lineNumber, pos.column);
        let callback = null;
        if (scrollToEnd.checked) {
            callback = (inverseEditOperations) => {
                const inverseRange = inverseEditOperations[0].range;
                window.setTimeout(() => {
                    editor.revealRange(inverseRange);
                    const start = inverseRange.getStartPosition();
                    const startRange = new monaco.Range(start.lineNumber, start.column + 1, start.lineNumber, start.column + 1);
                    editor.setSelection(startRange);
                    // editor.setSelection(inverseRange);
                }, 1);
                return null;
            };
        }
        editor.executeEdits("addSubscriptionEvent", [{ range: range, text: evStr, forceMoveMarkers: true }], callback);
    }
    updateUI(db, ev) {
        const databaseSub = this.databaseSubs[db];
        for (const task of ev.tasks) {
            switch (task.task) {
                case "cmd":
                case "msg": {
                    const allMessageSub = databaseSub.messageSubs["*"];
                    allMessageSub.events++;
                    this.uiMessageText(db, "*", allMessageSub, "event");
                    const messageSub = databaseSub.messageSubs[task.name];
                    // update UI if messages or command is defined in API
                    if (messageSub) {
                        messageSub.events++;
                        this.uiMessageText(db, task.name, messageSub, "event");
                    }
                    break;
                }
                case "upsert":
                case "create": {
                    const containerSub = databaseSub.containerSubs[task.cont];
                    containerSub.creates += task.set.length;
                    this.uiContainerText(db, task.cont, containerSub, "event");
                    break;
                }
                case "delete": {
                    const containerSub = databaseSub.containerSubs[task.cont];
                    containerSub.deletes += task.ids.length;
                    this.uiContainerText(db, task.cont, containerSub, "event");
                    break;
                }
                case "merge": {
                    const containerSub = databaseSub.containerSubs[task.cont];
                    containerSub.patches += task.set.length;
                    this.uiContainerText(db, task.cont, containerSub, "event");
                    break;
                }
            }
        }
    }
    async sendSubscriptionRequest(syncRequest) {
        const error = await app.playground.connect();
        if (error)
            return error;
        const response = await app.playground.sendWebSocketRequest(syncRequest);
        const message = response.message;
        if (message.msg == "error") {
            return message.message;
        }
        const task = message.tasks[0];
        if (task.task == "error") {
            return task.message;
        }
        return null;
    }
    // ----------------------------------- container subs -----------------------------------
    async toggleContainerSub(databaseName, containerName) {
        const containerSubs = this.databaseSubs[databaseName].containerSubs;
        const containerSub = containerSubs[containerName];
        containerSub.error = null;
        let changes = [];
        if (!containerSub.subscribed) {
            containerSub.subscribed = true;
            changes = ["create", "upsert", "merge", "delete"];
            this.uiContainerSubscribed(databaseName, containerName, true);
            this.uiContainerText(databaseName, containerName, containerSub, null);
        }
        else {
            containerSub.subscribed = false;
            this.uiContainerSubscribed(databaseName, containerName, false);
            this.uiContainerText(databaseName, containerName, containerSub, null);
        }
        const subscribeChanges = { task: "subscribeChanges", changes: changes, cont: containerName };
        const syncRequest = { msg: "sync", db: databaseName, tasks: [subscribeChanges] };
        const err = await this.sendSubscriptionRequest(syncRequest);
        if (err) {
            containerSub.error = err;
            this.uiContainerText(databaseName, containerName, containerSub, null);
        }
    }
    uiContainerSubscribed(databaseName, containerName, enable) {
        if (enable) {
            this.clusterTree.addContainerClass(databaseName, containerName, "subscribed");
            app.clusterTree.addContainerClass(databaseName, containerName, "subscribed");
            return;
        }
        this.clusterTree.removeContainerClass(databaseName, containerName, "subscribed");
        app.clusterTree.removeContainerClass(databaseName, containerName, "subscribed");
    }
    uiContainerText(databaseName, containerName, cs, trigger) {
        if (cs.error) {
            const error = cs.subscribed ? cs.error : null;
            this.clusterTree.setContainerError(databaseName, containerName, error);
            app.clusterTree.setContainerError(databaseName, containerName, error);
            return;
        }
        let values = null;
        if (cs.subscribed || cs.creates + cs.upserts + cs.deletes + cs.patches > 0) {
            values = [`${cs.creates + cs.upserts}`, `${cs.deletes}`, `${cs.patches}`];
        }
        this.clusterTree.setContainerText(databaseName, containerName, values, trigger);
        app.clusterTree.setContainerText(databaseName, containerName, values, trigger);
    }
    // ----------------------------------- message subs -----------------------------------
    async toggleMessageSub(databaseName, messageName) {
        const messageSubs = this.databaseSubs[databaseName].messageSubs;
        const messageSub = messageSubs[messageName];
        messageSub.error = null;
        let remove = false;
        if (!messageSub.subscribed) {
            messageSub.subscribed = true;
            this.uiMessageSubscribed(databaseName, messageName, true);
            this.uiMessageText(databaseName, messageName, messageSub, null);
        }
        else {
            remove = true;
            messageSub.subscribed = false;
            this.uiMessageSubscribed(databaseName, messageName, false);
            this.uiMessageText(databaseName, messageName, messageSub, null);
        }
        const subscribeMessage = { task: "subscribeMessage", remove: remove, name: messageName };
        const syncRequest = { msg: "sync", db: databaseName, tasks: [subscribeMessage] };
        const err = await this.sendSubscriptionRequest(syncRequest);
        if (err) {
            messageSub.error = err;
            this.uiMessageText(databaseName, messageName, messageSub, null);
        }
    }
    uiMessageSubscribed(databaseName, message, enable) {
        if (enable) {
            this.clusterTree.addMessageClass(databaseName, message, "subscribed");
            return;
        }
        this.clusterTree.removeMessageClass(databaseName, message, "subscribed");
    }
    uiMessageText(databaseName, messageName, cs, trigger) {
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
class EventFilter {
    constructor(allEvents, db, allDbEvents, container, allMessages, message) {
        this.allEvents = allEvents;
        this.db = db;
        this.allDbEvents = allDbEvents;
        this.allMessages = allMessages;
        this.message = message;
        this.container = container;
    }
    static None() {
        return new EventFilter(false, null, false, null, false, null);
    }
    match(ev) {
        if (this.allEvents)
            return true;
        if (ev.db != this.db)
            return false;
        const containers = ev.containers;
        if (this.allDbEvents && (containers === null || containers === void 0 ? void 0 : containers.length) > 0)
            return true;
        if (containers === null || containers === void 0 ? void 0 : containers.includes(this.container))
            return true;
        const messages = ev.messages;
        if (this.allMessages && (messages === null || messages === void 0 ? void 0 : messages.length) > 0)
            return true;
        if (messages === null || messages === void 0 ? void 0 : messages.includes(this.message))
            return true;
        return false;
    }
    getFilterName() {
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
    filterEvents(events, users, seqStart, seqEnd) {
        const matches = [];
        for (const ev of events) {
            if (!ev.filterSeqSrc(users, seqStart, seqEnd))
                continue;
            if (!this.match(ev))
                continue;
            matches.push(ev.msg);
        }
        const logs = `[${matches.join(',')}]`;
        const lastLog = matches.length == 0 ? 0 : logs.length - matches[matches.length - 1].length;
        const result = {
            logs: logs,
            lastLog: lastLog,
            eventCount: matches.length,
        };
        return result;
    }
}
//# sourceMappingURL=events.js.map