import { DbContainers } from "../../../../../Json.Tests/assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster.js";
import { EventMessage, SyncRequest } from "../../../../../Json.Tests/assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol.js";
import { EntityChange, SubscribeChanges } from "../../../../../Json.Tests/assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol.Tasks.js";
import { ClusterTree }  from "./components.js";
import { el }           from "./types.js";
import { app }          from "./index.js";

const subscriptionTree      = el("subscriptionTree");
const scrollToEnd           = el("scrollToEnd") as HTMLInputElement;
const formatEvents          = el("formatEvents") as HTMLInputElement;

function str(value: any) {
    return JSON.stringify(value);
}

class ContainerSub {
    subscribed: boolean;
    events:     number;

    constructor() {
        this.subscribed = false;
        this.events     = 0;
    }
}

class DatabaseSub {
    containerSubs : { [container: string] : ContainerSub} = {};
}

// ----------------------------------------------- Events -----------------------------------------------
export class Events
{
    private readonly clusterTree:   ClusterTree;
    private readonly databaseSubs:  { [database: string] : DatabaseSub } = {}

    public constructor() {
        this.clusterTree = new ClusterTree();
    }

    public initEvents(dbContainers: DbContainers[]) : void {
        const tree      = this.clusterTree;
        const ulCluster = tree.createClusterUl(dbContainers);
        tree.onSelectDatabase = (databaseName: string, classList: DOMTokenList) => {
            if (classList.length > 0) {
                return;
            }
            console.log(`onSelectDatabase ${databaseName}`);
        };
        tree.onSelectContainer = (databaseName: string, containerName: string, classList: DOMTokenList) => {
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

    public clearAllEvents() : void {
        app.eventsEditor.setValue("");
    }

    private static event2String (ev: EventMessage, format: boolean) : string {
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

    public addSubscriptionEvent(ev: EventMessage) : void {
        const editor    = app.eventsEditor;
        const model     = editor.getModel();
        const length    = model.getValue().length;
        let   evStr     = Events.event2String (ev, formatEvents.checked);

        if (length == 0) {
            model.setValue("[]");
        } else {
            evStr = `,${evStr}`;
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
                    app. clusterTree.setContainerText(ev.db, task.container, text);
                    break;
                }
            }
        }
        editor.executeEdits("addSubscriptionEvent", [{ range: range, text: evStr, forceMoveMarkers: true }], callback);
    }

    public toggleContainerSub(databaseName: string, containerName: string) : void {
        const containerSubs = this.databaseSubs[databaseName].containerSubs;
        const containerSub = containerSubs[containerName];
        let changes: EntityChange[] = [];
        if (!containerSub.subscribed) {
            containerSub.subscribed = true;
            changes = ["create", "upsert", "patch", "delete"];
            this.clusterTree.addContainerClass(databaseName, containerName, "subscribed");
            app. clusterTree.addContainerClass(databaseName, containerName, "subscribed");
        } else {
            containerSub.subscribed = false;
            this.clusterTree.removeContainerClass(databaseName, containerName, "subscribed");
            app. clusterTree.removeContainerClass(databaseName, containerName, "subscribed");
        }
        const subscribeChanges: SubscribeChanges = {
            task:       "subscribeChanges",
            changes:    changes,
            container: containerName
        };
        const syncRequest: SyncRequest = {
            msg:        "sync",
            database:   databaseName,
            tasks:      [subscribeChanges]
        };
        const request = JSON.stringify(syncRequest);
        app.playground.connect((error: string) => {
            if (error) {
                return;
            }
            app.playground.sendWebSocketRequest(request);
        });
    }
}
