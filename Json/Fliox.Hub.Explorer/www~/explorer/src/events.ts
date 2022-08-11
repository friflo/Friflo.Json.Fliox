import { DbContainers } from "../../../../../Json.Tests/assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster.js";
import { ClusterTree }  from "./components.js";
import { el }           from "./types.js";

const subscriptionTree       = el("subscriptionTree");

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
        tree.onSelectDatabase = (databaseName: string) => {
            console.log(`onSelectDatabase ${databaseName}`);
        };
        tree.onSelectContainer = (databaseName: string, containerName: string) => {
            console.log(`onSelectContainer ${databaseName} ${containerName}`);
        };
        subscriptionTree.textContent = "";
        subscriptionTree.appendChild(ulCluster);
    }
}
