import { ClusterTree } from "./components.js";
import { el } from "./types.js";
const subscriptionTree = el("subscriptionTree");
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
}
//# sourceMappingURL=events.js.map