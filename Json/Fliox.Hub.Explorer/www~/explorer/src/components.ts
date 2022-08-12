import { DbContainers } from "../../../../../Json.Tests/assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster";
import { createEl } from "./types.js";


export class ClusterTree {
    private selectedTreeEl: HTMLElement;

    onSelectDatabase  : (databaseName: string) => void;
    onSelectContainer : (databaseName: string, containerName: string) => void;

    private selectTreeElement(element: HTMLElement) {
        if (this.selectedTreeEl)
            this.selectedTreeEl.classList.remove("selected");
        this.selectedTreeEl =element;
        element.classList.add("selected");
    }

    public createClusterUl(dbContainers: DbContainers[]) : HTMLUListElement {
        const ulCluster = createEl('ul');
        ulCluster.onclick = (ev) => {
            const path = ev.composedPath() as HTMLElement[];
            const databaseElement = path[0];
            if (databaseElement.classList.contains("caret")) {
                path[2].classList.toggle("active");
                return;
            }
            const treeEl = path[1];
            if (this.selectedTreeEl == databaseElement) {
                if (treeEl.classList.contains("active"))
                    treeEl.classList.remove("active");
                else 
                    treeEl.classList.add("active");
                return;
            }
            treeEl.classList.add("active");
            this.selectTreeElement(databaseElement);

            const databaseName  = databaseElement.childNodes[1].textContent;
            this.onSelectDatabase(databaseName);
        };
        let firstDatabase = true;
        for (const dbContainer of dbContainers) {
            const liDatabase = createEl('li');

            const divDatabase           = createEl('div');
            const dbCaret               = createEl('div');
            dbCaret.classList.value     = "caret";
            const dbLabel               = createEl('span');
            dbLabel.innerText           = dbContainer.id;
            divDatabase.title           = "database";
            dbLabel.style.pointerEvents = "none";

            const containerTag    = createEl('span');
            containerTag.innerHTML= "tag";

            divDatabase.append(dbCaret);
            divDatabase.append(dbLabel);
            // divDatabase.append(containerTag);
            liDatabase.appendChild(divDatabase);
            ulCluster.append(liDatabase);
            if (firstDatabase) {
                firstDatabase = false;
                liDatabase.classList.add("active");
                this.selectTreeElement(divDatabase);
            }
            const ulContainers = createEl('ul');
            ulContainers.onclick = (ev) => {
                ev.stopPropagation();
                const path = ev.composedPath() as HTMLElement[];
                const containerElement = path[0];
                // in case of a multiline text selection selectedElement is the parent
                if (containerElement.tagName.toLowerCase() != "div")
                    return;
                this.selectTreeElement(path[1]);
                const containerNameDiv  = this.selectedTreeEl.children[0] as HTMLDivElement;
                const containerName     = containerNameDiv.innerText.trim();
                const databaseName      = path[3].childNodes[0].childNodes[1].textContent;
                this.onSelectContainer(databaseName, containerName);
            };
            liDatabase.append(ulContainers);
            for (const containerName of dbContainer.containers) {
                const liContainer       = createEl('li');
                liContainer.title       = "container";
                const containerLabel    = createEl('div');
                containerLabel.innerHTML= "&nbsp;" + containerName;
                liContainer.append(containerLabel);

                const containerTag    = createEl('div');
                containerTag.innerHTML= "tag";
                // liContainer.append(containerTag);

                ulContainers.append(liContainer);
            }
        }
        return ulCluster;
    }
}