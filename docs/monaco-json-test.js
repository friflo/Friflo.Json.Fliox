// Copy/Paste code below to Monaco Editor Playground
// at https://microsoft.github.io/monaco-editor/playground.html
//

const schemas = [{
	uri: 		"http://myserver/foo-schema.json#/definitions/Main",
	fileMatch:  ["http://db/test.json"], // associate with our model
	schema: {
		$ref: "http://myserver/foo-schema.json#/definitions/Main",
	}
},	
{
	uri: 		"http://myserver/foo-schema.json",
	schema: {
		definitions: {
			Main: { 
				type: "object",
				properties: {
					p1:   	{ enum: ["v1", "v2"] },
					p2:   	{ $ref: "./bar-schema.json" }, // reference the second schema
					test: 	{ $ref: "#/definitions/Test" }, // reference the second schema
					test2: 	{ $ref: "./bar-schema.json#/definitions/Test2" } // reference the second schema
				},
				additionalProperties: false
			},
			Test: {
				type: "object",
				properties: {
					name: { type: "string" }
				},
				additionalProperties: false
			}
		}
	}
},
{
	uri: "http://myserver/bar-schema.json", // id of the first schema
	schema: {
		type: "object",
		properties: {
			q1: { enum: ["x1", "x2"] }
		},
		definitions: {
			Test2: {
				type: "object",
				properties: {
					name2: { type: "string" }
				},
				additionalProperties: false
			}
		}
	}
}]

// --- set JSON Schemas
monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
	schemas: schemas,
	validate: true
});


// --- 1. create JSON model
const jsonContent =
`{
  "p1": "v3",
  "p2": {
    "q1": "x1"
  },
  "test": { 
    "name": "Joe"
  },
  "test2": { 
    "name2": "xyz"
  }
}

`;
const jsonModel = monaco.editor.createModel(jsonContent, "json", monaco.Uri.parse("http://db/test.json"));


// --- 2. create editor and set JSON model 
const editor = monaco.editor.create(document.getElementById("container"), {
	model: jsonModel
});
