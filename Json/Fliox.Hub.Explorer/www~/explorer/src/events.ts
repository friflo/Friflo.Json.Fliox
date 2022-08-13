import { DbContainers } from "../../../../../Json.Tests/assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster.js";
import { ClusterTree }  from "./components.js";
import { el }           from "./types.js";
import { app }          from "./index.js";
import { EventMessage } from "../../../../../Json.Tests/assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol.js";

const subscriptionTree      = el("subscriptionTree");
const scrollToEnd           = el("scrollToEnd") as HTMLInputElement;
const formatEvents          = el("formatEvents") as HTMLInputElement;

function str(value: any) {
    return JSON.stringify(value);
}
// ----------------------------------------------- Events -----------------------------------------------
export class Events
{
    private readonly clusterTree: ClusterTree;

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
                return;
            }
            console.log(`onSelectContainer ${databaseName} ${containerName}`);
        };
        subscriptionTree.textContent = "";
        subscriptionTree.appendChild(ulCluster);
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
        editor.executeEdits("addSubscriptionEvent", [{ range: range, text: evStr, forceMoveMarkers: true }], callback);
    }
}
