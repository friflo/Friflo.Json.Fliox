import { JSONSchema } from "../JSONSchema/Friflo.Json.Fliox.Schema.JSON"

// check assignment with using a type compiles successful
var exampleSync: JSONSchema = {
    "definitions": {
        "Entity": {
            "type": "object",
            "isAbstract": true,
            "properties": {
                "id": { "type": "string" }
            },
            "required": [
                "id"
            ],
            "additionalProperties": false
        }
    }    
}

export function testJsonSchema() {
}
