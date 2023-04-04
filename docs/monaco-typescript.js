// Copy/Paste code below to Monaco Editor Playground
// at https://microsoft.github.io/monaco-editor/playground.html
//
// 1. create Typescript module with createModel() which can be used as import in editor
// 2. add global available Typescript types with addExtraLib()
//    https://stackoverflow.com/questions/43058191/how-to-use-addextralib-in-monaco-with-an-external-type-definition
// 3. create Typescript module with createModel() intended for editing
// 4. create monaco text editor and add module created in 3.
// 5. hide lines in text editor

// compiler options
monaco.languages.typescript.typescriptDefaults.setCompilerOptions({
    target:                 monaco.languages.typescript.ScriptTarget.ES2016,
    allowNonTsExtensions:   true,
    moduleResolution:       monaco.languages.typescript.ModuleResolutionKind.NodeJs,
    module:                 monaco.languages.typescript.ModuleKind.CommonJS,
    noEmit:                 true,
    lib:                    ["es2016"], // omit DOM types
    noLib:                  false,
    typeRoots:              ["node_modules/@types"]
});

monaco.languages.typescript.typescriptDefaults.setDiagnosticsOptions({
    noSemanticValidation: false,
    noSyntaxValidation: false
})


// --- 1. add Typescript module as ITextModel to editor
const testContent = `
/** test docs for class */
export class Test {
    id      : string;
    name	: string;
}
`;
const testModel = monaco.editor.createModel(testContent, "typescript",	monaco.Uri.file("node_modules/@types/test/index.d.ts"));


// --- 2. add global Typescript types as string to with addExtraLib()
const globalTypes = `
/** test docs for function */
function testFunction(value: string) : string;
`;
monaco.languages.typescript.typescriptDefaults.addExtraLib(globalTypes);


// --- 3. create main file shown in the editor intended for editing
const mainContent =
`// --- hidden text line ---
import * as t from "test"

type Test = { id: string, name: string, val: number };
const filter: (o: Test) => boolean =
o => o.name == "abc"

testFunction("xyz");

const test = new t.Test();
`;
const mainModel = monaco.editor.createModel(mainContent,"typescript", monaco.Uri.parse("file:///main.ts"));



// --- 4. create editor and show main model in the editor
const editor = monaco.editor.create(document.getElementById("container"), {
	model: mainModel, 
	// language: "typescript",
	// automaticLayout: true, 
});

// --- 5. hide lines in text editor
const hiddenAreas = [new monaco.Range(1,0,1,0)];
editor.setHiddenAreas(hiddenAreas); // internal editor method


// editor.setModel(testModel) // change model displayed in editor


const readonlyRange = new monaco.Range (1, 0, 3, 0) 
editor.onKeyDown (e => {
	return;
    const contains = editor.getSelections ().findIndex (range => readonlyRange.intersectRanges (range))
	if (e.ctrlKey && e.code == "KeyA") {
        e.stopPropagation () 
        e.preventDefault () 
		return;
	}
	switch (e.code) {
		case  "ArrowUp":
		case  "ArrowDown":
		case  "ArrowLeft":
		case  "ArrowRight":
		case  "End":
		case  "Home":
		return;
	}
    if (contains !== -1) {
        e.stopPropagation ()
        e.preventDefault () // for Ctrl+C, Ctrl+V
    }
})