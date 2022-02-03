
import { FieldType, JsonType }  from "../../assets~/Schema/Typescript/JsonSchema/Friflo.Json.Fliox.Schema.JSON";
import { Resource, Config, el, createEl, Entity, parseAst }     from "./types.js";
import { App, app }                                             from "./index.js";
import { EntityEditor }                                         from "./entity-editor.js";

function createMeasureTextWidth(width: number) : HTMLElement {
    const div = document.createElement("div");
    document.body.appendChild(div);
    const style = div.style;
    style.fontSize      = `${width}px`;
    style.height        = "auto";
    style.width         = "auto";
    style.maxWidth      = "1000px"; // ensure not measuring crazy long texts
    style.position      = "absolute";
    style.whiteSpace    = "no-wrap";
    style.visibility    = "hidden";
    return div;
}
const measureTextWidth = createMeasureTextWidth (14);

type CellData = {
    readonly value?:         string,
    readonly count?:         number,
    readonly isObjectArray?: boolean
};

type Column = {
    readonly name:      string,
    readonly path:      string[],
    readonly type:      DataType,
             th?:       HTMLTableCellElement,
             width:     number
}

type TypeName   = "null" | "object" | "string" | "boolean" | "number" | "integer" | "array";

type DataType   = {
    readonly typeName:      TypeName,
    readonly jsonType:      JsonType | FieldType | null;
    readonly isNullable:    boolean;
}

type ParseResult = {
    readonly value?: any,
    readonly error?: string
}

const entityExplorer    = el("entityExplorer");
const writeResult       = el("writeResult");
const readEntitiesDB    = el("readEntitiesDB");
const readEntities      = el("readEntities");
const catalogSchema     = el("catalogSchema");
const explorerTools     = el("explorerTools");


const entityFilter      = el("entityFilter")    as HTMLInputElement;
const filterRow         = el("filterRow");


// ----------------------------------------------- Explorer -----------------------------------------------
/** The public methods of this class must not use DOM elements as parameters or return values.
 * Instead it have to use:
 * - strings or {@link Resource} for database, container & ids.
 * - numbers for rows & columns
 * - {@link Entity} and {@link JsonType} for entities
 */
export class Explorer
{
    private explorer: {
        readonly database:      string;
        readonly container:     string;
        readonly entityType:    JsonType | null;
        readonly entities:      Entity[];

    }
    private             focusedCell:        HTMLTableCellElement    = null;
    private             editCell:           HTMLTextAreaElement     = null;

    private             explorerTable:      HTMLTableElement;
    private readonly    config:             Config

    private             cachedJsonValue:    string                  = null;
    private             cachedJsonAst:      jsonToAst.ValueNode     = null;

    private             entityFields:   { [key: string] : Column }              = {}
    private             selectedRows:   { [key: string] : HTMLTableRowElement } = {};
    private             explorerRows:   { [key: string] : HTMLTableRowElement } = {};


    public getFocusedCell() : { row: number, column: number } | null {
        const focus = this.focusedCell;
        if (!focus)
            return null;
        const row = focus.parentElement as HTMLTableRowElement;
        return { column: focus.cellIndex, row: row.rowIndex };
    }

    public constructor(config: Config) {
        this.config = config;
    }

    private static selectAllHtml=
    `<div title="Select\n- All     (Ctrl + A)\n- None (Esc)" class="navigate selectAll" onclick="app.explorer.selectAllNone()">
       <div style="padding-left: 2px; padding-top: 3px;">
         <span>● ---</span><br>
         <span>● -----</span><br>
         <span>● ----</span>
       </div>
    </div>`;

    public async loadContainer (p: Resource, query: string)  : Promise<void> {
        const storedFilter  = this.config.filters[p.database]?.[p.container];
        const filter        = storedFilter && storedFilter[0] ? storedFilter[0] : "";        
        entityFilter.value  = filter;

        const removeFilterVisibility = query ? "" : "hidden";
        el("removeFilter").style.visibility = removeFilterVisibility;
        
        const entityType        = app.getContainerSchema(p.database, p.container);
        app.filter.database    = p.database;
        app.filter.container   = p.container;
        this.explorer = {
            database:   p.database,
            container:  p.container,
            entityType: entityType,
            entities:   null    // explorer: entities not loaded
        };        
        this.focusedCell = null;
        // const tasks =  [{ "task": "query", "container": p.container, "filterJson":{ "op": "true" }}];
        filterRow.style.visibility      = "";
        entityFilter.style.visibility   = "";
        catalogSchema.innerHTML  = app.getSchemaType(p.database) + "&nbsp;·&nbsp;" + app.getEntityType(p.database, p.container);
        explorerTools.innerHTML  = Explorer.selectAllHtml;
        readEntitiesDB.innerHTML = App.getDatabaseLink(p.database) + "/";
        const containerLink      = `<a title="open container in new tab" href="./rest/${p.database}/${p.container}" target="_blank" rel="noopener noreferrer">${p.container}/</a>`;
        readEntities.innerHTML   = `${containerLink}<span class="spinner"></span>`;

        const response           = await App.restRequest("GET", null, p.database, p.container, null, query);

        const reload = `<span class="reload" title='reload container' onclick='app.explorer.loadContainer(${JSON.stringify(p)})'></span>`;
        writeResult.innerHTML   = "";        
        readEntities.innerHTML  = containerLink + reload;
        if (!response.ok) {
            const error = await response.text();
            entityExplorer.innerHTML = App.errorAsHtml(error, p);
            return;
        }
        const   entities    = await response.json() as Entity[];
        this.explorer       = { ...this.explorer, entities };   // explorer: entities loaded successful

        this.entityFields   = {};
        const   head        = this.createExplorerHead(entityType, this.entityFields);

        const   table       = this.explorerTable = createEl('table');
        table.append(head);
        table.classList.value   = "entities";
        table.onclick = async (ev) => this.explorerOnClick(ev, p);
        
        this.explorerRows = {};
        this.selectedRows = {};
        this.updateExplorerEntities(entities, entityType);
        this.setColumnWidths();
        entityExplorer.innerText = "";
        entityExplorer.appendChild(table);
        // set initial focus cell
        this.setFocusCell(1, 1);
    }

    private async explorerOnClick(ev: MouseEvent, p: Resource) {
        const path          = ev.composedPath() as HTMLElement[];
        if (ev.shiftKey) {
            this.getSelectionFromPath(path, "id");
            const lastRow = this.focusedCell?.parentElement as HTMLTableRowElement;
            if (!lastRow)
                return;            
            await this.selectEntityRange(lastRow.rowIndex);            
            return;
        }
        const select        = ev.ctrlKey ? "toggle" : "id";
        const selectedIds   = this.getSelectionFromPath(path, select);
        if (selectedIds === null)
            return;
        this.setSelectedEntities(selectedIds);
        const params: Resource  = { database: p.database, container: p.container, ids: selectedIds };
        await app.editor.loadEntities(params, false, null);

        const json  = app.entityEditor.getValue();
        const ast   = this.getAstFromJson(json);
        this.selectEditorValue(ast, this.focusedCell);
    }

    private getAstFromJson(json: string) : jsonToAst.ValueNode | null {
        if (json == "")
            return null;
        if (json == this.cachedJsonValue)
            return this.cachedJsonAst;
        const ast = parseAst(json);
        this.cachedJsonAst      = ast;
        this.cachedJsonValue    = json;
        return ast;
    }

    private selectEditorValue(ast: jsonToAst.ValueNode, focus: HTMLTableCellElement) {
        if (!ast || !focus)
            return;
        const row       = focus.parentNode as HTMLTableRowElement;
        const column    = this.getColumnFromCell(focus);
        const path      = column.name;
        const keyName   = EntityEditor.getEntityKeyName(this.explorer.entityType);
        const id        = row.cells[1].innerText;
        const range     = EntityEditor.findPathRange(ast, path, keyName, id);
        if (range.entity) {
            app.entityEditor.revealRange(range.entity);
        }
        if (range.value) {
            app.entityEditor.setSelection(range.value);
            app.entityEditor.revealRange (range.value);            
        } else {
            // clear editor selection as focused cell not found in editor value
            const pos               = range.lastProperty?.getEndPosition()  ?? app.entityEditor.getPosition();
            const line              = pos.lineNumber    ?? 0;
            const column            = pos.column        ?? 0;
            const clearedSelection  = new monaco.Selection(line, column, line, column);
            app.entityEditor.setSelection(clearedSelection);
            // console.log("path not found:", path)
        }        
    }

    private getSelectionFromPath(path: HTMLElement[], select: "toggle" | "id") : string[] {
        // in case of a multiline text selection selectedElement is the parent
        const element = path[0];
        if (element.tagName == "TABLE") {
            return [];
        }
        if (element.tagName != "TD")
            return null;
        const cell          = element as HTMLTableCellElement;
        const row           = cell.parentElement as HTMLTableRowElement;
        const children      = path[1].children; // tr children
        const id            = (children[1] as HTMLElement).innerText;
        const isCheckbox    = cell == children[0];
        const selectedIds   = Object.keys(this.selectedRows);
        if (isCheckbox || select == "toggle") {
            if (Explorer.toggleIds(selectedIds, id) == "added") {
                const cellIndex = isCheckbox ? this.focusedCell?.cellIndex ?? 1 : cell.cellIndex;
                this.setFocusCell(row.rowIndex, cellIndex);
            }
            return selectedIds;
        }
        this.setFocusCell(row.rowIndex, cell.cellIndex);
        // Preserve selection if clicked cell is already selected
        if (selectedIds.indexOf(id) != -1) {
            return selectedIds;
        }
        return [id];
    }

    private static toggleIds(ids: string[], id: string) : "added" | "removed" {
        const index = ids.indexOf(id);
        if (index == -1) {
            ids.push(id);
            return "added";
        }
        ids.splice(index, 1);        
        return "removed";
    }

    public setFocusCellSelectValue(rowIndex: number, cellIndex: number, scroll: "smooth" | null = null) : void {
        const td = this.setFocusCell(rowIndex, cellIndex, scroll);
        if (!td)
            return;
        this.selectCellValue(td);
    }

    private selectCellValue(td: HTMLTableCellElement) {
        const json  = app.entityEditor.getValue();
        const ast   = this.getAstFromJson(json);
        this.selectEditorValue(ast, td);
    }

    private setFocusCell(rowIndex: number, cellIndex: number, scroll: "smooth" | null = null) : HTMLTableCellElement | null {
        const table = this.explorerTable;
        if (rowIndex < 1 || cellIndex < 1)
            return null;
        const rows = table.rows;
        if (rowIndex >= rows.length)
            return null;
        const row = rows[rowIndex];
        if (cellIndex >= row.cells.length)
            return null;

        const td = row.cells[cellIndex];
        this.focusedCell?.classList.remove("focus");
        td.classList.add("focus");
        this.focusedCell = td;
        // td.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        Explorer.ensureVisible(entityExplorer, td, 16, 22, scroll);
        return td;
    }

    // Chrome ignores { scroll-margin-top: 20px; scroll-margin-left: 16px; } for sticky header / first row 
    private static ensureVisible(containerEl: HTMLElement, el: HTMLElement, offsetLeft: number, offsetTop: number, scroll: "smooth" | null) {
        const parentEl  = containerEl.parentElement;
        // const parent    = parentEl.getBoundingClientRect();
        // const container = containerEl.getBoundingClientRect();
        // const cell      = el.getBoundingClientRect();

        const width     = parentEl.clientWidth;
        const height    = parentEl.clientHeight;
        const x         = el.offsetLeft - offsetLeft;  // cell.x - offsetLeft - container.x;
        const y         = el.offsetTop  - offsetTop;   // cell.y - offsetTop  - container.y;

        const minLeft   = parentEl.scrollLeft;
        const minTop    = parentEl.scrollTop;
        const maxLeft   = minLeft + width  - el.clientWidth  - offsetLeft;
        const maxTop    = minTop  + height - el.clientHeight - offsetTop;

        if (x < minLeft ||
            y < minTop  ||
            x > maxLeft ||
            y > maxTop)
        {
            const left = x > maxLeft ? Math.min(x, el.offsetLeft + el.clientWidth  - width)  : Math.min (x, minLeft);
            const top  = y > maxTop  ? Math.min(y, el.offsetTop  + el.clientHeight - height) : Math.min (y, minTop);

            const smooth = scroll == "smooth" || top == parentEl.scrollTop;
            const opt: ScrollToOptions = { left, top, behavior: smooth ? "smooth" : undefined };
            parentEl.scrollTo(opt);
        }
    }

    public async explorerKeyDown(event: KeyboardEvent) : Promise<void> {
        const explorer  = this.explorer;
        const td = this.focusedCell;
        if (!td)
            return;
        if (this.editCell)
            return;
        const table     = this.explorerTable;
        const row       = td.parentElement as HTMLTableRowElement;
        switch (event.code) {
            case 'Home':
                event.preventDefault();
                if (event.ctrlKey) {
                    this.setFocusCellSelectValue (1, td.cellIndex, "smooth");
                } else {
                    this.setFocusCellSelectValue (row.rowIndex, 1, "smooth");
                }
                return;
            case 'End':
                event.preventDefault();
                if (event.ctrlKey) {
                    this.setFocusCellSelectValue (table.rows.length - 1, td.cellIndex, "smooth");
                } else {
                    this.setFocusCellSelectValue (row.rowIndex, row.cells.length - 1, "smooth");
                }                
                return;
            case 'PageUp':
                event.preventDefault();
                this.setFocusCellSelectValue(row.rowIndex - 3, td.cellIndex);
                return;                
            case 'PageDown':
                event.preventDefault();
                this.setFocusCellSelectValue(row.rowIndex + 3, td.cellIndex);
                return;                
            case 'ArrowUp': {
                event.preventDefault();
                this.setFocusCellSelectValue(row.rowIndex - 1, td.cellIndex);
                const focused = this.focusedCell.parentElement as HTMLTableRowElement;
                if (event.ctrlKey && row.rowIndex != focused.rowIndex) {
                    const id = this.getRowId(focused);
                    await this.selectExplorerEntities([id]);
                    this.selectCellValue(this.focusedCell);
                }
                return;
            }
            case 'ArrowDown': {
                event.preventDefault();
                this.setFocusCellSelectValue(row.rowIndex + 1, td.cellIndex);
                const focused = this.focusedCell.parentElement as HTMLTableRowElement;
                if (event.ctrlKey && row.rowIndex != focused.rowIndex) {
                    const id = this.getRowId(focused);
                    await this.selectExplorerEntities([id]);
                    this.selectCellValue(this.focusedCell);
                }
                return;
            }
            case 'ArrowLeft':
                event.preventDefault();
                this.setFocusCellSelectValue(row.rowIndex, td.cellIndex - 1);
                return;
            case 'ArrowRight':
                event.preventDefault();
                this.setFocusCellSelectValue(row.rowIndex, td.cellIndex + 1);
                return;
            case 'Space': {
                event.preventDefault();
                const id        = this.getRowId(row);
                const ids       = Object.keys(this.selectedRows);
                const toggle    = Explorer.toggleIds(ids, id);
                await this.selectExplorerEntities(ids);

                if (toggle == "added")
                    this.selectCellValue(this.focusedCell);
                return;
            }
            case 'Enter': {
                event.preventDefault();
                if (event.shiftKey) {
                    await this.selectEntityRange(row.rowIndex);
                    this.selectCellValue(this.focusedCell);
                    return;
                }
                const ids = [this.getRowId(row)];
                await this.selectExplorerEntities(ids);
                this.selectCellValue(this.focusedCell);
                return;
            }
            case 'KeyA': {
                if (!event.ctrlKey)
                    return;
                event.preventDefault();
                const ids = Object.keys(this.explorerRows);
                this.selectExplorerEntities(ids);
                return;
            }
            case 'Escape': {
                event.preventDefault();
                this.selectExplorerEntities([]);
                return;
            }
            case 'KeyC': {
                if (!event.ctrlKey)
                    return;
                event.preventDefault();
                const editorValue = app.entityEditor.getValue();
                navigator.clipboard.writeText(editorValue);
                return;
            }
            case 'Delete': {
                event.preventDefault();
                const ids   = Object.keys(this.selectedRows);
                app.editor.deleteEntities(explorer.database, explorer.container, ids);
                return;
            }
            case 'F2':
                this.createEditCell(td);
                break;
            default:
                return;
        }        
    }

    public selectAllNone() : void {
        const selectedCount     = Object.keys(this.selectedRows).length;
        if (selectedCount == 0) {
            const ids = Object.keys(this.explorerRows);
            this.selectExplorerEntities(ids);
            return;
        }
        const entitiesCount    = Object.keys(this.explorerRows).length;
        if (selectedCount == entitiesCount) {
            this.selectExplorerEntities([]);
            return;
        }
        this.selectExplorerEntities([]);
    }

    private async selectExplorerEntities(ids: string[]) {
        const explorer  = this.explorer;
        this.setSelectedEntities(ids);
        const params: Resource  = { database: explorer.database, container: explorer.container, ids: ids };
        await app.editor.loadEntities(params, false, null);
    }

    private async selectEntityRange(lastIndex: number) {
        const selection     = Object.values(this.selectedRows);
        let   firstIndex    = selection.length == 0 ? 1 : selection[selection.length - 1].rowIndex;
        if (lastIndex > firstIndex) {
            [lastIndex, firstIndex] = [firstIndex, lastIndex];
        }
        const ids: string[] = [];
        const rows          = this.explorerTable.rows;
        for (let i = lastIndex; i <= firstIndex; i++) {
            ids.push(rows[i].cells[1].textContent);
        }
        await this.selectExplorerEntities(ids);
        this.selectCellValue(this.focusedCell);
    }

    private getRowId(row: HTMLTableRowElement) : string {
        const keyName       = EntityEditor.getEntityKeyName(this.explorer.entityType);
        const table         = this.explorerTable;
        const headerCells   = table.rows[0].cells;
        for (let i = 1; i < headerCells.length; i++) {
            if (headerCells[i].innerText != keyName)
                continue;
            return row.cells[i].innerText;
        }
        return null;
    }

    public setSelectedEntities(ids: string[]) : void {
        const oldSelection = this.selectedRows;
        for (const id in oldSelection) {
            const entityEl = oldSelection[id];
            entityEl.classList.remove("selected");
        }
        const newSelection = this.selectedRows = this.findContainerEntities(ids);
        for (const id in newSelection) {
            newSelection[id].classList.add("selected");
        }
    }

    private createEditCell(td: HTMLTableCellElement) {        
        const row           = td.parentElement as HTMLTableRowElement;
        const id            = this.getRowId(row);
        const edit          = createEl("textarea");
        edit.rows = 1; edit.cols = 1;   // enable textarea to shrink
        edit.style.minWidth     = td.clientWidth  + "px";
        edit.spellcheck         = false;
        const div               = createEl("div");
        div.append (edit);
        this.editCell           = edit;
        let saveChange          = true;
        const oldValue          = td.textContent;
        edit.value              = td.textContent;
        div.dataset["replicatedValue"] = td.textContent;
        edit.oninput        = () => { div.dataset["replicatedValue"] = edit.value; };
        // remove onblur for debugging DOM
        edit.onblur         = () => {
            this.editCell   = null;                    
            edit.remove();
            const column    = this.getColumnFromCell(td);
            let   result: ParseResult = null;
            if (saveChange) {
                result = Explorer.getJsonValue(column, edit.value);
                if (result.error) {
                    console.error("invalid value -", result.error);
                    saveChange = false;
                }
            }
            td.textContent  = saveChange ? edit.value : oldValue;
            td.classList.remove("editCell");
            td.classList.add("focus");
            if (saveChange) {
                this.saveCell(id, result.value, column);
            }
        };
        edit.onkeydown      = (event) => {
            switch (event.code) {
                case 'Escape':
                    event.stopPropagation();
                    saveChange = false;
                    entityExplorer.focus();
                    break;
                case 'Enter':
                    if (event.ctrlKey || event.altKey) {
                        const pos =  edit.selectionStart;
                        edit.value = edit.value.substring(0,pos) + "\n" + edit.value.substring(pos);
                        edit.selectionStart = edit.selectionEnd = pos + 1;
                        div.dataset["replicatedValue"] = edit.value;
                        break;
                    }
                    event.stopPropagation();
                    entityExplorer.focus();
                    break;
            }
        };
        td.classList.add("editCell");
        td.classList.remove("focus");
        td.textContent      = "";
        td.append(div);
        edit.focus();
    }

    private getColumnFromCell(td: HTMLTableCellElement) {
        const thDiv     = this.explorerTable.rows[0].cells[td.cellIndex].firstChild as HTMLDivElement;
        const fieldName = thDiv.getAttribute("fieldName");
        return this.entityFields[fieldName];
    }

    private static getJsonValue(column: Column, valueStr: string) : ParseResult {
        const type = column.type;
        if (valueStr == "null") {
            if (!type.isNullable)
                return { error: "field not nullable" };
            return { value: "null" };
        }
        const fieldType = type.jsonType as FieldType;
        if (type.typeName == "string") {
            if (fieldType?.format == "date-time") {
                if (isNaN(Date.parse(valueStr)))
                    return { error: `invalid Date: ${valueStr}` };
            }
            if (fieldType?.pattern !== undefined) {
                const regEx = new RegExp(fieldType.pattern);
                if (valueStr.match(regEx) == null)
                    return { error: "invalid value" };
            }
            return { value:  JSON.stringify(valueStr) };    
        }
        try {
            const value = JSON.parse(valueStr);
            if (type.typeName == "integer") {
                if (!Number.isInteger(value))
                    return { error: `invalid integer: ${value}` };
            }
            if (type.typeName == "number") {
                if (typeof value != "number")
                    return { error: `invalid number: ${value}` };
            }
            if (fieldType?.minimum !== undefined) {
                if (value < fieldType.minimum)
                    return { error: `value ${value} less than ${fieldType.minimum}` };
            }
            if (fieldType?.maximum !== undefined) {
                if (value > fieldType.maximum)
                    return { error: `value ${value} greater than ${fieldType.maximum}` };
            }
            if (type.typeName == "boolean") {
                if (typeof value != "boolean")
                    return { error: `invalid boolean value: ${value}` };
            }
            return { value: valueStr };
        } catch  {
            const nullableType = type.isNullable ? " | null" : "";
            return { error: `invalid ${type.typeName}${nullableType} value: ${valueStr}` };
        }
    }

    private async saveCell(id: string, jsonValue: string, column: Column) : Promise<void> {
        const fieldName = column.name;
        const keyName   = EntityEditor.getEntityKeyName(column.type.jsonType as JsonType);
        // console.log("saveCell", fieldName, column.type.typeName);

        if (this.selectedRows[id]) {
            const json      = app.entityEditor.getValue();
            const ast       = this.getAstFromJson(json);
            const range     = EntityEditor.findPathRange(ast, fieldName, keyName, id);
            if (range.value) {
                app.entityEditor.executeEdits("", [{ range: range.value, text: jsonValue }]);
            } else {
                const newProperty   = `,\n    "${fieldName}": ${jsonValue}`;
                const line          = range.lastProperty.endLineNumber;
                const col           = range.lastProperty.endColumn;
                const pos           = new monaco.Range(line, col, line, col);
                app.entityEditor.executeEdits("", [{ range: pos, text: newProperty }]);
            }
        }
        const explorer      = this.explorer;
        const entity        = explorer.entities.find(entity => entity[keyName] == id);
        entity[fieldName]   = JSON.parse(jsonValue);
        const json          = JSON.stringify(entity, null, 4);
        await App.restRequest("PUT", json, explorer.database, explorer.container, id, null);
    }

    private static getDataType(fieldType: FieldType) : DataType {
        const   ref = fieldType._resolvedDef;
        if (ref)
            return this.getDataType(ref as unknown as FieldType);
        const oneOf = fieldType.oneOf;
        if (oneOf) {
            const jsonType = fieldType as unknown as JsonType;
            if (jsonType.discriminator) {
                return { typeName: "object", jsonType: fieldType, isNullable: false };
            }
            let isNullable              = false;
            let oneOfType: FieldType    = null;
            for (const item of oneOf) {
                if (item.type == "null") {
                    isNullable = true;
                    continue;
                }
                oneOfType = item;
            }
            const dataType = Explorer.getDataType(oneOfType);
            return { typeName: dataType.typeName, jsonType: dataType.jsonType, isNullable };
        }
        const type = fieldType.type;        
        if (type == "array") {
            const itemType = Explorer.getDataType(fieldType.items);
            return { typeName: "array", jsonType: itemType.jsonType, isNullable: false };
        }
        if (type == "object") {
            return { typeName: "object", jsonType: fieldType, isNullable: false };
        }
        if (!Array.isArray(type))
            return { typeName: fieldType.type, jsonType: fieldType, isNullable: false };

        // e.g. ["string", "null"]
        let isNullable          = false;
        let arrayTypeName: TypeName = null;
        for (const item of type) {
            if (item == "null") {
                isNullable = true;
                continue;
            }
            arrayTypeName = item;
        }
        if (!arrayTypeName)
            throw `missing type in type array`;      
        return { typeName: arrayTypeName, jsonType: null, isNullable };
    }

    private static setColumns(columns: Column[], path: string[], fieldType: FieldType) {
        // if (path[0] == "uint8Null") debugger;
        const type:     DataType    = Explorer.getDataType(fieldType);
        const typeName: TypeName    = type.typeName;
        switch (typeName) {
            case "string":
            case "integer":
            case "number":
            case "boolean":
            case "array": {
                const name = path.join(".");
                columns.push({name: name, path: path, type: type, width: Explorer.defaultColumnWidth });
                break;
            }
            case "object": {
                const addProps = type.jsonType.additionalProperties;
                //    isAny == true   <=>   additionalProperties == {}
                const isAny =   addProps !== null && typeof addProps == "object" && Object.keys(addProps).length == 0;
                if (isAny) {
                    const name = path.join(".");
                    columns.push({name: name, path: path, type: type, width: Explorer.defaultColumnWidth });
                    break;
                }
                const properties = (type.jsonType as JsonType).properties;
                for (const name in properties) {
                    const property  = properties[name];
                    const fieldPath = [...path, name];
                    this.setColumns(columns, fieldPath, property);
                }
                break;
            }
        }
    }

    private createExplorerHead (entityType: JsonType, entityFields: { [key: string] : Column }) : HTMLTableRowElement {
        const keyName   = EntityEditor.getEntityKeyName(entityType);
        if (entityType) {
            const properties    =  entityType.properties;
            for (const fieldName in properties) {
                const fieldType = properties[fieldName];
                const columns   = [] as Column[];
                Explorer.setColumns(columns, [fieldName], fieldType);
                for (const column of columns) {
                    entityFields[column.name] = column;
                }
            }
        } else {
            const type: DataType    = { typeName: "string", jsonType: null, isNullable: false };
            entityFields[keyName]   = { name: keyName, path: [keyName], type, width: Explorer.defaultColumnWidth };
        }
        const   head            = createEl('tr');

        // cell: checkbox
        const   thCheckbox      = createEl('th');
        thCheckbox.style.width  = "16px";
        const   thCheckboxDiv   = createEl('div');        
        thCheckbox.append(thCheckboxDiv);
        head.append(thCheckbox);

        // cell: fields (id, ...)
        for (const fieldName in entityFields) {
            const column        = entityFields[fieldName];
            const th            = createEl('th');
            th.style.width      = `${Explorer.defaultColumnWidth}px`;
            const thIdDiv       = createEl('div');
            const path          = column.path;
            thIdDiv.innerText   = path.length == 1 ? path[0] : `.${path[path.length-1]}`;
            const type          = column.type;
            const jsonType      = type.jsonType as JsonType;
            const fieldType     = `\ntype:   ${jsonType?._typeName ?? type.typeName}${type.isNullable ? "?": ""}`;
            thIdDiv.title       = `name: ${fieldName}${fieldType}`;
            thIdDiv.setAttribute("fieldName", fieldName);
            th.append(thIdDiv);
            const grip          = createEl('div');
            grip.classList.add("thGrip");
            grip.style.cursor   = "ew-resize";
            // grip.style.background   = 'red';
            // grip.style.userSelect = "none"; // disable text selection while dragging */
            grip.addEventListener('mousedown', (e) => this.thStartDrag(e, th) );
            th.appendChild(grip);
            head.append(th);
            column.th = th;
        }

        // cell: last
        const   thLast          = createEl('th');
        thLast.style.width      = "100%";
        head.append(thLast);
        return head;
    }

    private static defaultColumnWidth   = 50;
    private static maxColumnWidth       = 200;

    private static calcWidth(text: string) : number {
        if (text === undefined)
            return 0;
        if (text.length > 40) {
            // avoid measuring long texts
            // 30 characters => 234px. Sample: "012345678901234567890123456789"
            return Explorer.maxColumnWidth;
        }
        measureTextWidth.innerHTML = text;
        return Math.ceil(measureTextWidth.clientWidth);                
    }

    private setColumnWidths() {
        for (const fieldName in this.entityFields) {
            const column = this.entityFields[fieldName];
            column.th.style.width = `${column.width + 10}px`;
        }
    }

    public updateExplorerEntities(entities: Entity[], entityType: JsonType) : void {
        const table         = this.explorerTable;
        let entityCount     = 0;
        const keyName       = EntityEditor.getEntityKeyName(entityType);
        const entityFields  = this.entityFields;
        const fieldCount    = Object.keys(entityFields).length;
        const tds           = [] as HTMLTableCellElement[];
        const rows          = this.explorerRows;
        // console.log("entities", entities);
        for (const entity of entities) {
            tds.length  = 0;
            const id    = entity[keyName];
            let   row   = rows[id];
            if (!row) {
                row = createEl('tr');
                rows[id] = row;

                // cell: add checkbox
                const tdCheckbox    = createEl('td');
                const checked       = createEl('input');
                checked.type        = "checkbox";
                checked.tabIndex    = -1;
                checked.checked     = true;
                tdCheckbox.append(checked);
                row.append(tdCheckbox);
                tds.push(tdCheckbox);

                // cell: add fields
                for (let n = 0; n < fieldCount; n++) {
                    const tdField = createEl('td');
                    row.append(tdField);
                    tds.push(tdField);
                }
                table.append(row);
            } else {
                for (const td of row.childNodes) {
                    tds.push(td as HTMLTableCellElement);
                }            
            }
            // cell: set fields
            const calcWidth = entityCount < 20;
            Explorer.assignRowCells(tds, entity, entityFields, calcWidth);
            entityCount++;
        }
    }

    private static assignRowCells (tds: HTMLTableCellElement[], entity: Entity, entityFields: { [key: string] : Column }, calcWidth: boolean) {
        let tdIndex = 1;
        for (const fieldName in entityFields) {
            // if (fieldName == "derivedClassNull.derivedVal") debugger;
            const column    = entityFields[fieldName];
            const path      = column.path;
            let   value     = entity;
            const pathLen   = path.length;
            let   i         = 0;
            for (; i < pathLen; i++) {
                value = value[path[i]];
                if (value === null || value === undefined || typeof value != "object")
                    break;
            }
            if (i < pathLen - 1)
                value = undefined;
            const td        = tds[tdIndex++];
            // clear all children added previously
            while (td.firstChild) {
                td.removeChild(td.lastChild);
            }
            const content   = Explorer.getCellContent(value);
            const count     = content.count;
            if (count === undefined) {
                td.textContent   = content.value;
            } else {
                const isObjectArray     = content.isObjectArray;
                const countStr          = count == 0 ? '0' : `${count}: `;
                const spanCount         = createEl("span");
                spanCount.textContent   = isObjectArray ? `${countStr} ${fieldName}` : countStr;
                spanCount.classList.add("cellCount");
                td.append(spanCount);
                if (!isObjectArray) {
                    const spanValue         = createEl("span");
                    spanValue.textContent   = content.value;
                    td.append(spanValue);
                } 
            }
            // measure text width is expensive => measure only the first 20 rows
            if (calcWidth) {
                let width           = Explorer.calcWidth(content.value);
                if (count) width   += Explorer.calcWidth(String(count));                
                if (column.width < width) {
                    column.width = width;
                }
            }
        }
    }

    private static getCellContent(value: any) : CellData {
        if (value === undefined)
            return { value: "" };                                       // 
        const type = typeof value;
        if (type != "object")
            return { value: value };                                    // abc
        if (Array.isArray(value)) {
            if (value.length > 0) {
                for (const item of value) {
                    if (typeof item == "object") {                      // 3: objects
                        return { count: value.length, isObjectArray: true};
                    }
                }
                const items = value.map(i => i);
                return { value: items.join(", "), count: value.length}; // 2: abc,xyz
            }
            return { value: "", count: 0 };                             // 0;
        }
        return { value: JSON.stringify(value) };                        // {"foo": "bar", ... }
    }

    public removeExplorerIds(ids: string[]) : void {
        const selected = this.findContainerEntities(ids);
        for (const id in selected)
            selected[id].remove();
        for (const id of ids) {
            delete this.explorerRows[id];
            delete this.selectedRows[id];
        }
    }

    public findRowIndices (ids: string[]) : {[key: string] : number } {
        const result : {[key: string] : number } = {};
        for(const id of ids){
            const li = this.explorerRows[id];
            if (!li)
                continue;
            result[id] = li.rowIndex;
        }
        return result;
    }

    private findContainerEntities (ids: string[]) : {[key: string] : HTMLTableRowElement} {
        const result : {[key: string] : HTMLTableRowElement} = {};
        for(const id of ids){
            const li = this.explorerRows[id];
            if (!li)
                continue;
            result[id] = li;
        }
        return result;
    }

    // ------------------------------ column drag bars ------------------------------
    private thDrag          : HTMLElement;
    private thDragOffset    : number;    

    private thStartDrag(event: MouseEvent, th: HTMLElement) {
        this.thDragOffset           = event.offsetX - (event.target as HTMLElement).clientWidth;
        this.thDrag                 = th;
        document.body.style.cursor  = "ew-resize";
        document.body.onmousemove   = (event)  => this.thOnDrag(event);
        document.body.onmouseup     = ()       => this.thEndDrag();
        event.preventDefault();
    }

    private thOnDrag(event: MouseEvent) {
        const parent            = (this.thDrag.parentNode.parentNode.parentNode.parentNode as HTMLElement);
        const scrollOffset      = parent.scrollLeft;
        let width               = scrollOffset + event.clientX - this.thDragOffset - this.thDrag.offsetLeft;
        if (width < 25) width   = 25;
        this.thDrag.style.width = `${width}px`;
        event.preventDefault();
    }

    private thEndDrag() {
        document.body.onmousemove   = undefined;
        document.body.onmouseup     = undefined;
        document.body.style.cursor  = "auto";
    }
}
