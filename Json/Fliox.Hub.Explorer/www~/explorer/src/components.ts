import { DbContainers, DbMessages } from "../../../../../Json.Tests/assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster";
import { createEl } from "./types.js";


class DatabaseTags {
    containerTags:  { [container: string] : HTMLElement } = {}
    messageTags:    { [message:   string] : HTMLElement } = {}
}

export class ClusterTree {
    private selectedTreeEl: HTMLElement;
    private databaseTags:  { [database: string] : DatabaseTags} = {};

    onSelectDatabase    : (databaseName: string,                        classList: DOMTokenList) => void;
    onSelectContainer   : (databaseName: string, containerName: string, classList: DOMTokenList) => void;
    onSelectMessage     : (databaseName: string, messageName: string,   classList: DOMTokenList) => void;
    onSelectMessages    : (databaseName: string,                        classList: DOMTokenList) => void;

    private selectTreeElement(element: HTMLElement) {
        if (this.selectedTreeEl)
            this.selectedTreeEl.classList.remove("selected");
        this.selectedTreeEl =element;
        element.classList.add("selected");
    }

    public createClusterUl(dbContainers: DbContainers[], dbMessages: DbMessages[] | null) : HTMLUListElement {
        const ulCluster = createEl('ul');
        ulCluster.onclick = (ev) => {
            const path          = ev.composedPath() as HTMLElement[];
            const databaseEl    = ClusterTree.findTreeEl (path, "clusterDatabase");
            const caretEl       = ClusterTree.findTreeEl (path, "caret");
            if (caretEl) {
                databaseEl.parentElement.classList.toggle("active");
                return;
            }
            const databaseName  = databaseEl.childNodes[1].textContent;
            const treeEl        = databaseEl.parentElement;
            if (this.selectedTreeEl == databaseEl) {
                if (treeEl.classList.contains("active"))
                    treeEl.classList.remove("active");
                else 
                    treeEl.classList.add("active");
                return;
            }
            treeEl.classList.add("active");
            this.selectTreeElement(databaseEl);
            this.onSelectDatabase(databaseName, path[0].classList);
        };
        let firstDatabase = true;
        for (const dbContainer of dbContainers) {
            const databaseName = dbContainer.id;
            const databaseTags = new DatabaseTags();
            this.databaseTags[dbContainer.id] = databaseTags;

            const liDatabase = createEl('li');
            liDatabase.classList.add('treeParent');

            const divDatabase           = createEl('div');
            const dbCaret               = createEl('div');
            dbCaret.classList.value     = "caret";
            const dbLabel               = createEl('span');
            dbLabel.innerText           = dbContainer.id;
            divDatabase.title           = "database";
            divDatabase.className       = "clusterDatabase";



            divDatabase.append(dbCaret);
            divDatabase.append(dbLabel);
            /* const containerTag    = createEl('span');
            containerTag.className = "sub";
            divDatabase.append(containerTag); */
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
                const path              = ev.composedPath() as HTMLElement[];
                const messagesEl        = ClusterTree.findTreeEl (path, "dbMessages");
                if (messagesEl) {
                    const caretEl           = ClusterTree.findTreeEl (path, "caret");
                    if (caretEl) {
                        messagesEl.parentElement.classList.toggle("active");
                        return;
                    }
                    const databaseEl        = messagesEl.parentNode.parentNode.parentNode;
                    this.selectTreeElement(messagesEl);
                    const databaseName      = databaseEl.childNodes[0].childNodes[1].textContent;
                    this.onSelectMessages(databaseName, path[0].classList);
                    return;
                }
                const messageEl         = ClusterTree.findTreeEl (path, "dbMessage");
                if (messageEl) {
                    const databaseEl        = messageEl.parentNode.parentNode.parentNode.parentNode;
                    this.selectTreeElement(messageEl);
                    const messageNameDiv    = messageEl.children[0] as HTMLDivElement;
                    const messageName       = messageNameDiv.innerText.trim();
                    const databaseName      = databaseEl.childNodes[0].childNodes[1].textContent;
                    this.onSelectMessage(databaseName, messageName, path[0].classList);
                    return;
                }
                const containerEl       = ClusterTree.findTreeEl (path, "clusterContainer");
                if (containerEl) {
                    const databaseEl        = containerEl.parentNode.parentNode;
                    this.selectTreeElement(containerEl);
                    const containerNameDiv  = containerEl.children[0] as HTMLDivElement;
                    const containerName     = containerNameDiv.innerText.trim();
                    const databaseName      = databaseEl.childNodes[0].childNodes[1].textContent;
                    this.onSelectContainer(databaseName, containerName, path[0].classList);
                }
            };
            liDatabase.append(ulContainers);
            if (dbMessages) {
                const messages = dbMessages.find(entry => entry.id == databaseName);
                const commandsLi = this.createMessages(databaseTags, messages);
                ulContainers.append(commandsLi);
            }
            for (const containerName of dbContainer.containers) {
                const liContainer       = createEl('li');
                liContainer.title       = "container";
                liContainer.className   = "clusterContainer";
                const containerLabel    = createEl('div');
                containerLabel.innerHTML= "&nbsp;" + containerName;
                liContainer.append(containerLabel);

                const containerTag      = createEl('div');
                containerTag.className  = "sub";
                containerTag.title      = "subscribe container changes\n(creates + upserts, deletes, patches)";
                liContainer.append(containerTag);
                databaseTags.containerTags[containerName] = containerTag;

                ulContainers.append(liContainer);
            }
        }
        return ulCluster;
    }

    private createMessages(databaseTags: DatabaseTags, dbMessages: DbMessages) : HTMLLIElement {

        const liMessages            = createEl('li');
        liMessages.classList.add('treeParent');
        const divMessages           = createEl('div');
        const dbCaret               = createEl('div');
        dbCaret.classList.value     = "caret";
        const dbLabel               = createEl('span');
        dbLabel.innerText           = "messages";
        dbLabel.style.opacity       = "0.6";
        divMessages.title           = "messages";
        divMessages.className       = "dbMessages";

        divMessages.append(dbCaret);
        divMessages.append(dbLabel);

        const messagesTag      = createEl('div');
        messagesTag.className  = "sub";
        messagesTag.title      = `subscribe messages / commands`;
        divMessages.append(messagesTag);
        // databaseTags.containerTags[containerName] = containerTag;
        databaseTags.messageTags["*"] = messagesTag;

        const ulMessages        = createEl('ul');
        this.addMessages (databaseTags, ulMessages, dbMessages.commands);
        this.addMessages (databaseTags, ulMessages, dbMessages.messages);

        liMessages.append(divMessages);
        liMessages.append(ulMessages);
        return liMessages;
    }

    private addMessages(databaseTags: DatabaseTags, ulMessages: HTMLUListElement, messages: string[]) {
        for (const message of messages) {
            const liMessage         = createEl('li');
            liMessage.classList.add("dbMessage");
            const divMessage        = createEl('div');
            divMessage.innerText    = message;
            const messageTag        = createEl('div');
            messageTag.className    = "sub";
            messageTag.title        = `subscribe ${message}`;
            databaseTags.messageTags[message] = messageTag;
            liMessage.append(divMessage);
            liMessage.append(messageTag);
            ulMessages.append(liMessage);
        }
    }

    // --- containerTags 
    public addContainerClass (database: string, container: string, className: "subscribed") : void {
        const el = this.databaseTags[database].containerTags[container];
        el.classList.add(className);
    }

    public removeContainerClass (database: string, container: string, className: "subscribed") : void {
        const el = this.databaseTags[database].containerTags[container];
        el.classList.remove(className);
    }

    public setContainerText (database: string, container: string, text: string) : void {
        const el = this.databaseTags[database].containerTags[container];
        el.innerHTML = text;
    }

    // --- messageTags 
    public addMessageClass (database: string, message: string, className: "subscribed") : void {
        const el = this.databaseTags[database].messageTags[message];
        el.classList.add(className);
    }

    public removeMessageClass (database: string, message: string, className: "subscribed") : void {
        const el = this.databaseTags[database].messageTags[message];
        el.classList.remove(className);
    }

    public setMessageText (database: string, message: string, text: string) : void {
        const el = this.databaseTags[database].messageTags[message];
        el.innerHTML = text;
    }  

    private static findTreeEl(path: HTMLElement[], itemClass: string) {
        for (const el of path) {
            if (el.classList?.contains(itemClass))
                return el;
        }
        return null;
    }
}