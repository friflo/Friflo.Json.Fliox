import { ClusterTree } from "./components.js";
import { el } from "./types.js";
import { app } from "./index.js";
const subscriptionTree = el("subscriptionTree");
const scrollToEnd = el("scrollToEnd");
const formatEvents = el("formatEvents");
const subFilter = el("subFilter");
export const eventsInfo = `

    info

- subscribe to container changes and messages by clicking the 'sub' tag of a tree item
- show subscription event log by clicking its tree entry`;
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
    }
}
class MessageSub {
    constructor() {
        this.subscribed = false;
        this.events = 0;
    }
}
class DatabaseSub {
    constructor() {
        this.containerSubs = {};
        this.messageSubs = {};
    }
}
class SubEvent {
    constructor(msg, ev) {
        this.msg = msg;
        this.db = ev.db;
        const messages = [];
        const containers = [];
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
                case "patch": {
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
    static internName(name) {
        const intern = SubEvent.internNames[name];
        if (intern)
            return intern;
        SubEvent.internNames[name] = name;
        return name;
    }
}
SubEvent.internNames = {};
// ----------------------------------------------- Events -----------------------------------------------
export class Events {
    constructor() {
        this.databaseSubs = {};
        this.subEvents = [];
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
    }
    clearAllEvents() {
        app.eventsEditor.setValue("");
    }
    static event2String(ev, format) {
        if (!format) {
            return JSON.stringify(ev, null, 4);
        }
        // const tasksJson = ev.tasks.map(task => JSON.stringify(task));
        const tasksJson = [];
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
                    const entities = task.entities.map(entity => JSON.stringify(entity));
                    const entitiesJson = entities.join(",\n            ");
                    const json = `{"task":"${task.task}"${KV("container", task.container)}${KV("keyName", task.keyName)}, "entities":[
            ${entitiesJson}
        ]}`;
                    tasksJson.push(json);
                    break;
                }
                case "delete": {
                    const ids = task.ids.map(entity => JSON.stringify(entity));
                    const idsJson = ids.join(",\n            ");
                    const json = `{"task":"${task.task}"${KV("container", task.container)}, "ids":[
            ${idsJson}
        ]}`;
                    tasksJson.push(json);
                    break;
                }
                case "patch": {
                    const patches = task.patches.map(patch => JSON.stringify(patch));
                    const patchesJson = patches.join(",\n            ");
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
    "msg":"ev"${KV("seq", ev.seq)}${KV("src", ev.src)}${KV("clt", ev.clt)}${KV("db", ev.db)}${KV("isOrigin", ev.isOrigin)},
    "tasks": [
        ${tasks}
    ]
}`;
    }
    setEditorLog(filter) {
        this.filter = filter;
        const filterResult = filter.filterEvents(this.subEvents);
        subFilter.innerText = filter.getFilterName();
        const editor = app.eventsEditor;
        editor.setValue(filterResult.logs);
        const pos = editor.getModel().getPositionAt(filterResult.lastLog);
        editor.revealPositionNearTop(pos);
    }
    addSubscriptionEvent(ev) {
        const acknowledgeEvent = true;
        if (acknowledgeEvent) {
            const syncRequest = { msg: "sync", database: ev.db, tasks: [], info: "acknowledge received event" };
            const request = JSON.stringify(syncRequest);
            app.playground.sendWebSocketRequest(request);
        }
        const evStr = Events.event2String(ev, formatEvents.checked);
        const msg = new SubEvent(evStr, ev);
        this.subEvents.push(msg);
        this.updateUI(ev);
        if (!this.filter.match(msg))
            return;
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
    updateUI(ev) {
        const databaseSub = this.databaseSubs[ev.db];
        for (const task of ev.tasks) {
            switch (task.task) {
                case "command":
                case "message": {
                    const allMessageSub = databaseSub.messageSubs["*"];
                    allMessageSub.events++;
                    this.uiMessageText(ev.db, "*", allMessageSub);
                    const messageSub = databaseSub.messageSubs[task.name];
                    messageSub.events++;
                    this.uiMessageText(ev.db, task.name, messageSub);
                    break;
                }
                case "upsert":
                case "create": {
                    const containerSub = databaseSub.containerSubs[task.container];
                    containerSub.creates += task.entities.length;
                    this.uiContainerText(ev.db, task.container, containerSub);
                    break;
                }
                case "delete": {
                    const containerSub = databaseSub.containerSubs[task.container];
                    containerSub.deletes += task.ids.length;
                    this.uiContainerText(ev.db, task.container, containerSub);
                    break;
                }
                case "patch": {
                    const containerSub = databaseSub.containerSubs[task.container];
                    containerSub.patches += task.patches.length;
                    this.uiContainerText(ev.db, task.container, containerSub);
                    break;
                }
            }
        }
    }
    // ----------------------------------- container subs -----------------------------------
    toggleContainerSub(databaseName, containerName) {
        const containerSubs = this.databaseSubs[databaseName].containerSubs;
        const containerSub = containerSubs[containerName];
        let changes = [];
        if (!containerSub.subscribed) {
            containerSub.subscribed = true;
            changes = ["create", "upsert", "patch", "delete"];
            this.uiContainerSubscribed(databaseName, containerName, true);
            this.uiContainerText(databaseName, containerName, containerSub);
        }
        else {
            containerSub.subscribed = false;
            this.uiContainerSubscribed(databaseName, containerName, false);
            this.uiContainerText(databaseName, containerName, containerSub);
        }
        const subscribeChanges = { task: "subscribeChanges", changes: changes, container: containerName };
        const syncRequest = { msg: "sync", database: databaseName, tasks: [subscribeChanges] };
        const request = JSON.stringify(syncRequest);
        app.playground.connect((error) => {
            if (error) {
                return;
            }
            app.playground.sendWebSocketRequest(request);
        });
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
    uiContainerText(databaseName, containerName, cs) {
        let values = null;
        if (cs.subscribed || cs.creates + cs.upserts + cs.deletes + cs.patches > 0) {
            values = [`${cs.creates + cs.upserts}`, `${cs.deletes}`, `${cs.patches}`];
        }
        this.clusterTree.setContainerText(databaseName, containerName, values);
        app.clusterTree.setContainerText(databaseName, containerName, values);
    }
    // ----------------------------------- message subs -----------------------------------
    toggleMessageSub(databaseName, messageName) {
        const messageSubs = this.databaseSubs[databaseName].messageSubs;
        const messageSub = messageSubs[messageName];
        let remove = false;
        if (!messageSub.subscribed) {
            messageSub.subscribed = true;
            this.uiMessageSubscribed(databaseName, messageName, true);
            this.uiMessageText(databaseName, messageName, messageSub);
        }
        else {
            remove = true;
            messageSub.subscribed = false;
            this.uiMessageSubscribed(databaseName, messageName, false);
            this.uiMessageText(databaseName, messageName, messageSub);
        }
        const subscribeMessage = {
            task: "subscribeMessage",
            remove: remove,
            name: messageName
        };
        const syncRequest = {
            msg: "sync",
            database: databaseName,
            tasks: [subscribeMessage]
        };
        const request = JSON.stringify(syncRequest);
        app.playground.connect((error) => {
            if (error) {
                return;
            }
            app.playground.sendWebSocketRequest(request);
        });
        return messageSub;
    }
    uiMessageSubscribed(databaseName, message, enable) {
        if (enable) {
            this.clusterTree.addMessageClass(databaseName, message, "subscribed");
            return;
        }
        this.clusterTree.removeMessageClass(databaseName, message, "subscribed");
    }
    uiMessageText(databaseName, messageName, cs) {
        let text = "";
        if (cs.subscribed || cs.events > 0) {
            text = `${cs.events}`;
        }
        this.clusterTree.setMessageText(databaseName, messageName, text);
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
    filterEvents(events) {
        const matches = [];
        for (const ev of events) {
            if (!this.match(ev))
                continue;
            matches.push(ev.msg);
        }
        const logs = `[${matches.join(',')}]`;
        const lastLog = matches.length == 0 ? 0 : logs.length - matches[matches.length - 1].length;
        const result = {
            logs: logs,
            lastLog: lastLog
        };
        return result;
    }
}
//# sourceMappingURL=events.js.map