import { createEl } from "./types.js";
class DatabaseTags {
    constructor() {
        this.containerTags = {};
        this.messageTags = {};
    }
}
export class ClusterTree {
    constructor() {
        this.databaseTags = {};
    }
    selectTreeElement(element) {
        if (this.selectedTreeEl)
            this.selectedTreeEl.classList.remove("selected");
        this.selectedTreeEl = element;
        element.classList.add("selected");
    }
    createClusterUl(dbContainers, dbMessages) {
        const ulCluster = createEl('ul');
        ulCluster.onclick = (ev) => {
            const path = ev.composedPath();
            const databaseEl = ClusterTree.findTreeEl(path, "clusterDatabase");
            const caretEl = ClusterTree.findTreeEl(path, "caret");
            if (caretEl) {
                databaseEl.parentElement.classList.toggle("active");
                return;
            }
            const databaseName = databaseEl.childNodes[1].textContent;
            const treeEl = databaseEl.parentElement;
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
            const divDatabase = createEl('div');
            const dbCaret = createEl('div');
            dbCaret.classList.value = "caret";
            const dbLabel = createEl('span');
            dbLabel.innerText = dbContainer.id;
            divDatabase.title = "database";
            divDatabase.className = "clusterDatabase";
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
                const path = ev.composedPath();
                const messagesEl = ClusterTree.findTreeEl(path, "dbMessages");
                if (messagesEl) {
                    const caretEl = ClusterTree.findTreeEl(path, "caret");
                    if (caretEl) {
                        messagesEl.parentElement.classList.toggle("active");
                        return;
                    }
                    return;
                }
                const messageEl = ClusterTree.findTreeEl(path, "dbMessage");
                if (messageEl) {
                    const databaseEl = messageEl.parentNode.parentNode.parentNode.parentNode;
                    this.selectTreeElement(messageEl);
                    const messageNameDiv = messageEl.children[0];
                    const messageName = messageNameDiv.innerText.trim();
                    const databaseName = databaseEl.childNodes[0].childNodes[1].textContent;
                    this.onSelectMessage(databaseName, messageName, path[0].classList);
                    return;
                }
                const containerEl = ClusterTree.findTreeEl(path, "clusterContainer");
                if (containerEl) {
                    const databaseEl = containerEl.parentNode.parentNode;
                    this.selectTreeElement(containerEl);
                    const containerNameDiv = containerEl.children[0];
                    const containerName = containerNameDiv.innerText.trim();
                    const databaseName = databaseEl.childNodes[0].childNodes[1].textContent;
                    this.onSelectContainer(databaseName, containerName, path[0].classList);
                }
            };
            liDatabase.append(ulContainers);
            if (dbMessages) {
                const messages = dbMessages.find(entry => entry.id == databaseName);
                const commandsLi = this.createMessages(databaseTags, "commands", messages.commands);
                ulContainers.append(commandsLi);
                const messagesLi = this.createMessages(databaseTags, "messages", messages.messages);
                ulContainers.append(messagesLi);
            }
            for (const containerName of dbContainer.containers) {
                const liContainer = createEl('li');
                liContainer.title = "container";
                liContainer.className = "clusterContainer";
                const containerLabel = createEl('div');
                containerLabel.innerHTML = "&nbsp;" + containerName;
                liContainer.append(containerLabel);
                const containerTag = createEl('div');
                containerTag.className = "sub";
                containerTag.title = "subscribe container changes\n(creates + upserts, deletes, patches)";
                liContainer.append(containerTag);
                databaseTags.containerTags[containerName] = containerTag;
                ulContainers.append(liContainer);
            }
        }
        return ulCluster;
    }
    createMessages(databaseTags, category, messages) {
        const liMessages = createEl('li');
        liMessages.classList.add('treeParent');
        const divMessages = createEl('div');
        const dbCaret = createEl('div');
        dbCaret.classList.value = "caret";
        const dbLabel = createEl('span');
        dbLabel.innerText = category;
        dbLabel.style.opacity = "0.6";
        divMessages.title = category;
        divMessages.className = "dbMessages";
        divMessages.append(dbCaret);
        divMessages.append(dbLabel);
        const messagesTag = createEl('div');
        messagesTag.className = "sub";
        messagesTag.title = `subscribe ${category}`;
        divMessages.append(messagesTag);
        // databaseTags.containerTags[containerName] = containerTag;
        const ulMessages = createEl('ul');
        for (const message of messages) {
            const liMessage = createEl('li');
            liMessage.classList.add("dbMessage");
            const divMessage = createEl('div');
            divMessage.innerText = message;
            const messageTag = createEl('div');
            messageTag.className = "sub";
            messageTag.title = `subscribe ${message}`;
            databaseTags.messageTags[message] = messageTag;
            liMessage.append(divMessage);
            liMessage.append(messageTag);
            ulMessages.append(liMessage);
        }
        liMessages.append(divMessages);
        liMessages.append(ulMessages);
        return liMessages;
    }
    // --- containerTags 
    addContainerClass(database, container, className) {
        const el = this.databaseTags[database].containerTags[container];
        el.classList.add(className);
    }
    removeContainerClass(database, container, className) {
        const el = this.databaseTags[database].containerTags[container];
        el.classList.remove(className);
    }
    setContainerText(database, container, text) {
        const el = this.databaseTags[database].containerTags[container];
        el.innerHTML = text;
    }
    // --- messageTags 
    addMessageClass(database, message, className) {
        const el = this.databaseTags[database].messageTags[message];
        el.classList.add(className);
    }
    removeMessageClass(database, message, className) {
        const el = this.databaseTags[database].messageTags[message];
        el.classList.remove(className);
    }
    setMessageText(database, message, text) {
        const el = this.databaseTags[database].messageTags[message];
        el.innerHTML = text;
    }
    static findTreeEl(path, itemClass) {
        var _a;
        for (const el of path) {
            if ((_a = el.classList) === null || _a === void 0 ? void 0 : _a.contains(itemClass))
                return el;
        }
        return null;
    }
}
//# sourceMappingURL=components.js.map