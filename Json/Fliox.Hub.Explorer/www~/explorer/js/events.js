import { ClusterTree } from "./components.js";
import { el } from "./types.js";
import { app } from "./index.js";
const subscriptionTree = el("subscriptionTree");
const scrollToEnd = el("scrollToEnd");
const formatEvents = el("formatEvents");
function str(value) {
    return JSON.stringify(value);
}
class DatabaseSub {
    constructor() {
        this.containerSubs = [];
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
        editor.executeEdits("addSubscriptionEvent", [{ range: range, text: evStr, forceMoveMarkers: true }], callback);
    }
    toggleContainerSub(databaseName, containerName) {
        const containerSubs = this.databaseSubs[databaseName].containerSubs;
        const index = containerSubs.indexOf(containerName);
        let changes = [];
        if (index == -1) {
            containerSubs.push(containerName);
            changes = ["create", "upsert", "patch", "delete"];
        }
        else {
            containerSubs.splice(index, 1);
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
        app.playground.sendWebSocketRequest(request);
    }
}
//# sourceMappingURL=events.js.map