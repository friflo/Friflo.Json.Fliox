import { DbContainers } from "../../../../../Json.Tests/assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster.js";
import { EventMessage, SyncRequest } from "../../../../../Json.Tests/assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol.js";
import { EntityChange, SubscribeChanges } from "../../../../../Json.Tests/assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol.Tasks.js";
import { ClusterTree }  from "./components.js";
import { el }           from "./types.js";
import { app }          from "./index.js";

const subscriptionTree      = el("subscriptionTree");
const scrollToEnd           = el("scrollToEnd") as HTMLInputElement;
const formatEvents          = el("formatEvents") as HTMLInputElement;

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

    constructor() {
        this.subscribed = false;
        this.creates    = 0;
        this.upserts    = 0;
        this.deletes    = 0;
        this.patches    = 0;        
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
                case "patch": {
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
    "msg":"ev"${KV("seq", ev.seq)}${KV("src", ev.src)}${KV("clt", ev.clt)}${KV("db", ev.db)},
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
        editor.executeEdits("addSubscriptionEvent", [{ range: range, text: evStr, forceMoveMarkers: true }], callback);
    }

    public toggleContainerSub(databaseName: string, containerName: string) : ContainerSub {
        const containerSubs = this.databaseSubs[databaseName].containerSubs;
        const containerSub = containerSubs[containerName];
        let changes: EntityChange[] = [];
        if (!containerSub.subscribed) {
            containerSub.subscribed = true;
            changes = ["create", "upsert", "patch", "delete"];
            this.uiContainerSubscribed(databaseName, containerName, true);
            this.uiContainerText(databaseName, containerName, containerSub);
        } else {
            containerSub.subscribed = false;
            this.uiContainerSubscribed(databaseName, containerName, false);
            this.uiContainerText(databaseName, containerName, containerSub);
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
        return containerSub;
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

    private uiContainerText(databaseName: string, containerName: string, cs: ContainerSub) {
        let text = "";
        if (cs.subscribed || cs.creates + cs.upserts + cs.deletes + cs.patches > 0) {
            text = `<span class="creates">${cs.creates + cs.upserts}</span> <span class="deletes">${cs.deletes}</span> <span class="patches">${cs.patches}</span>`;
        }
        this.clusterTree.setContainerText(databaseName, containerName, text);
        app. clusterTree.setContainerText(databaseName, containerName, text);
    }
}
