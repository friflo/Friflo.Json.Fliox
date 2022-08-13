import { ClusterTree } from "./components.js";
import { el } from "./types.js";
import { app } from "./index.js";
const subscriptionTree = el("subscriptionTree");
const scrollToEnd = el("scrollToEnd");
const formatEvents = el("formatEvents");
function str(value) {
    return JSON.stringify(value);
}
class ContainerSub {
    constructor() {
        this.subscribed = false;
        this.events = 0;
    }
}
class DatabaseSub {
    constructor() {
        this.containerSubs = {};
    }
}
// ----------------------------------------------- Events -----------------------------------------------
export class Events {
    constructor() {
        this.databaseSubs = {};
        this.clusterTree = new ClusterTree();
    }
    initEvents(dbContainers) {
        const tree = this.clusterTree;
        const ulCluster = tree.createClusterUl(dbContainers);
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
        subscriptionTree.textContent = "";
        subscriptionTree.appendChild(ulCluster);
        for (const database of dbContainers) {
            const databaseSub = new DatabaseSub();
            this.databaseSubs[database.id] = databaseSub;
            for (const container of database.containers) {
                databaseSub.containerSubs[container] = new ContainerSub();
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
        const tasksJson = ev.tasks.map(task => JSON.stringify(task));
        const tasks = tasksJson.join(",\n        ");
        return `{
    "msg":"ev", "seq":${str(ev.seq)}, "src":${str(ev.src)}, "clt":${str(ev.clt)}, "db":${str(ev.db)},
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
        const databaseSub = this.databaseSubs[ev.db];
        for (const task of ev.tasks) {
            switch (task.task) {
                case "patch":
                case "create":
                case "upsert":
                case "delete": {
                    const containerSub = databaseSub.containerSubs[task.container];
                    containerSub.events++;
                    const text = containerSub.events.toString();
                    this.clusterTree.setContainerText(ev.db, task.container, text);
                    app.clusterTree.setContainerText(ev.db, task.container, text);
                    break;
                }
            }
        }
        editor.executeEdits("addSubscriptionEvent", [{ range: range, text: evStr, forceMoveMarkers: true }], callback);
    }
    toggleContainerSub(databaseName, containerName) {
        const containerSubs = this.databaseSubs[databaseName].containerSubs;
        const containerSub = containerSubs[containerName];
        let changes = [];
        if (!containerSub.subscribed) {
            containerSub.subscribed = true;
            changes = ["create", "upsert", "patch", "delete"];
            this.uiContainerSubscribed(databaseName, containerName, true);
            const text = containerSub.events.toString();
            this.uiContainerText(databaseName, containerName, text);
        }
        else {
            containerSub.subscribed = false;
            this.uiContainerSubscribed(databaseName, containerName, false);
            if (containerSub.events == 0) {
                this.uiContainerText(databaseName, containerName, "");
            }
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
    uiContainerText(databaseName, containerName, text) {
        this.clusterTree.setContainerText(databaseName, containerName, text);
        app.clusterTree.setContainerText(databaseName, containerName, text);
    }
}
//# sourceMappingURL=events.js.map