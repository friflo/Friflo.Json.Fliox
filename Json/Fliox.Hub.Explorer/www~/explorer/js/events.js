import { ClusterTree } from "./components.js";
import { el } from "./types.js";
import { app } from "./index.js";
const subscriptionTree = el("subscriptionTree");
const scrollToEnd = el("scrollToEnd");
// ----------------------------------------------- Events -----------------------------------------------
export class Events {
    constructor() {
        this.clusterTree = new ClusterTree();
    }
    initEvents(dbContainers) {
        const tree = this.clusterTree;
        const ulCluster = tree.createClusterUl(dbContainers);
        tree.onSelectDatabase = (databaseName) => {
            console.log(`onSelectDatabase ${databaseName}`);
        };
        tree.onSelectContainer = (databaseName, containerName) => {
            console.log(`onSelectContainer ${databaseName} ${containerName}`);
        };
        subscriptionTree.textContent = "";
        subscriptionTree.appendChild(ulCluster);
    }
    addSubscriptionEvent(eventMessage) {
        const editor = app.eventsEditor;
        const model = editor.getModel();
        const length = model.getValue().length;
        let ev = JSON.stringify(eventMessage, null, 2);
        if (length == 0) {
            model.setValue("[]");
        }
        else {
            ev = `,${ev}`;
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
        editor.executeEdits("addSubscriptionEvent", [{ range: range, text: ev, forceMoveMarkers: true }], callback);
    }
}
//# sourceMappingURL=events.js.map