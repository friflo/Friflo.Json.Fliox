import { ClusterTree } from "./components.js";
import { el } from "./types.js";
import { app } from "./index.js";
const subscriptionTree = el("subscriptionTree");
const scrollToEnd = el("scrollToEnd");
const formatEvents = el("formatEvents");
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
    initEvents(dbContainers, dbMessages) {
        const tree = this.clusterTree;
        const ulCluster = tree.createClusterUl(dbContainers, dbMessages);
        tree.onSelectDatabase = (databaseName, classList) => {
            if (classList.length > 0) {
                return;
            }
            console.log(`onSelectDatabase ${databaseName}`);
        };
        tree.onSelectContainer = (databaseName, containerName, classList) => {
            if (classList.length > 0) {
                this.toggleContainerSub(databaseName, containerName);
                return;
            }
            console.log(`onSelectContainer ${databaseName} ${containerName}`);
        };
        tree.onSelectMessage = (databaseName, messageName, classList) => {
            if (classList.length > 0) {
                this.toggleMessageSub(databaseName, messageName);
                return;
            }
            console.log(`onSelectMessage ${databaseName} ${messageName}`);
        };
        tree.onSelectMessages = (databaseName, classList) => {
            if (classList.length > 0) {
                this.toggleMessageSub(databaseName, "*");
                return;
            }
            console.log(`onSelectMessageGroup ${databaseName}`);
        };
        subscriptionTree.textContent = "";
        subscriptionTree.appendChild(ulCluster);
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
    "msg":"ev"${KV("seq", ev.seq)}${KV("src", ev.src)}${KV("clt", ev.clt)}${KV("db", ev.db)},
    "tasks": [
        ${tasks}
    ]
}`;
    }
    addSubscriptionEvent(ev) {
        const editor = app.eventsEditor;
        const model = editor.getModel();
        const length = model.getValue().length;
        let evStr = Events.event2String(ev, formatEvents.checked);
        const msg = new SubEvent(evStr, ev);
        this.subEvents.push(msg);
        if (length == 0) {
            model.setValue("[]");
        }
        else {
            evStr = `,${evStr}`;
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
        this.updateUI(ev);
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
        const subscribeChanges = {
            task: "subscribeChanges",
            changes: changes,
            container: containerName
        };
        const syncRequest = {
            msg: "sync",
            database: databaseName,
            tasks: [subscribeChanges]
        };
        const request = JSON.stringify(syncRequest);
        app.playground.connect((error) => {
            if (error) {
                return;
            }
            app.playground.sendWebSocketRequest(request);
        });
        return containerSub;
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
        let text = "";
        if (cs.subscribed || cs.creates + cs.upserts + cs.deletes + cs.patches > 0) {
            text = `<span class="creates">${cs.creates + cs.upserts}</span> <span class="deletes">${cs.deletes}</span> <span class="patches">${cs.patches}</span>`;
        }
        this.clusterTree.setContainerText(databaseName, containerName, text);
        app.clusterTree.setContainerText(databaseName, containerName, text);
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
            text = `<span class="creates">${cs.events}</span>`;
        }
        this.clusterTree.setMessageText(databaseName, messageName, text);
    }
}
//# sourceMappingURL=events.js.map