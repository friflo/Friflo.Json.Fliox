import { JsonSchema } from "../JsonSchema/Friflo.Json.Flow.Schema.JSON"

// check assignment with using a type compiles successful
var exampleSync: JsonSchema = {
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
