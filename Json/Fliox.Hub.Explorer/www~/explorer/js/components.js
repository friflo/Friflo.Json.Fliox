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
    selectTreeElement(elem) {
        if (this.selectedTreeEl)
            this.selectedTreeEl.classList.remove("selected");
        this.selectedTreeEl = elem;
        elem.classList.add("selected");
    }
    toggleTreeElement(elem) {
        if (this.selectedTreeEl) {
            this.selectedTreeEl.classList.remove("selected");
            if (elem == this.selectedTreeEl) {
                this.selectedTreeEl = null;
                return false;
            }
        }
        this.selectedTreeEl = elem;
        elem.classList.add("selected");
        return true;
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
            this.onSelectDatabase(databaseEl, path[0].classList, databaseName);
        };
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
            const ulContainers = createEl('ul');
            ulContainers.onclick = (ev) => {
                ev.stopPropagation();
                const path = ev.composedPath();
                const classList = path[0].classList;
                const messagesEl = ClusterTree.findTreeEl(path, "dbMessages");
                if (messagesEl) {
                    const caretEl = ClusterTree.findTreeEl(path, "caret");
                    if (caretEl) {
                        messagesEl.parentElement.classList.toggle("active");
                        return;
                    }
                    const databaseEl = messagesEl.parentNode.parentNode.parentNode;
                    const databaseName = databaseEl.childNodes[0].childNodes[1].textContent;
                    this.onSelectMessages(messagesEl, classList, databaseName);
                    return;
                }
                const messageEl = ClusterTree.findTreeEl(path, "dbMessage");
                if (messageEl) {
                    const databaseEl = messageEl.parentNode.parentNode.parentNode.parentNode;
                    const messageNameDiv = messageEl.children[0];
                    const messageName = messageNameDiv.innerText.trim();
                    const databaseName = databaseEl.childNodes[0].childNodes[1].textContent;
                    this.onSelectMessage(messageEl, classList, databaseName, messageName);
                    return;
                }
                const containerEl = ClusterTree.findTreeEl(path, "clusterContainer");
                if (containerEl) {
                    const databaseEl = containerEl.parentNode.parentNode;
                    const containerNameDiv = containerEl.children[0];
                    const containerName = containerNameDiv.innerText.trim();
                    const databaseName = databaseEl.childNodes[0].childNodes[1].textContent;
                    this.onSelectContainer(containerEl, classList, databaseName, containerName);
                }
            };
            liDatabase.append(ulContainers);
            if (dbMessages) {
                const messages = dbMessages.find(entry => entry.id == databaseName);
                const commandsLi = this.createMessages(databaseTags, messages);
                ulContainers.append(commandsLi);
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
    createMessages(databaseTags, dbMessages) {
        const liMessages = createEl('li');
        liMessages.classList.add('treeParent');
        const divMessages = createEl('div');
        const dbCaret = createEl('div');
        dbCaret.classList.value = "caret";
        const dbLabel = createEl('span');
        dbLabel.innerText = "messages";
        dbLabel.style.opacity = "0.6";
        divMessages.title = "messages";
        divMessages.className = "dbMessages";
        divMessages.append(dbCaret);
        divMessages.append(dbLabel);
        const messagesTag = createEl('div');
        messagesTag.className = "sub";
        messagesTag.title = `subscribe messages / commands`;
        divMessages.append(messagesTag);
        // databaseTags.containerTags[containerName] = containerTag;
        databaseTags.messageTags["*"] = messagesTag;
        const ulMessages = createEl('ul');
        this.addMessages(databaseTags, ulMessages, dbMessages.commands);
        this.addMessages(databaseTags, ulMessages, dbMessages.messages);
        liMessages.append(divMessages);
        liMessages.append(ulMessages);
        return liMessages;
    }
    addMessages(databaseTags, ulMessages, messages) {
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
    }
    setSpan(el, text, title, classList) {
        el.innerText = '';
        if (!title)
            return;
        const span = createEl('span');
        for (const className of classList) {
            span.classList.add(className);
        }
        span.innerText = text;
        span.title = title;
        el.append(span);
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
    setContainerError(database, container, text) {
        const el = this.databaseTags[database].containerTags[container];
        this.setSpan(el, "error", text, ["error"]);
    }
    setContainerText(database, container, texts, trigger) {
        const el = this.databaseTags[database].containerTags[container];
        if (!texts) {
            el.innerText = "";
            return;
        }
        if (el.children.length == 0) {
            el.innerHTML = `<span class="creates"></span> <span class="deletes"></span> <span class="patches"></span>`;
            for (const child of el.children) {
                child.addEventListener("animationend", () => {
                    child.classList.remove("updated", "updated2");
                });
            }
        }
        ClusterTree.setTextChildren(el, texts, trigger);
    }
    static setTextChildren(el, texts, trigger) {
        const children = el.children;
        for (let n = 0; n < children.length; n++) {
            const child = children[n];
            const text = texts[n];
            if (texts[n] == child.innerText)
                continue;
            child.innerText = text;
            if (trigger != "event")
                continue;
            const classList = child.classList;
            if (classList.contains('updated')) {
                classList.remove('updated');
                classList.add('updated2');
            }
            else {
                classList.add('updated');
                classList.remove('updated2');
            }
        }
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
    setMessageError(database, message, text) {
        const el = this.databaseTags[database].messageTags[message];
        this.setSpan(el, "error", text, ["error"]);
    }
    setMessageText(database, message, text, trigger) {
        const el = this.databaseTags[database].messageTags[message];
        if (!text) {
            el.innerText = "";
            return;
        }
        if (el.children.length == 0) {
            el.innerHTML = `<span class="creates"></span>`;
            const child = el.firstChild;
            child.addEventListener("animationend", () => { child.classList.remove("updated", "updated2"); });
        }
        ClusterTree.setTextChildren(el, [text], trigger);
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