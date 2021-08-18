import { JsonFlowSchema } from "../JsonFlowSchema/Friflo.Json.Flow.Schema.JSON"

// check assignment with using a type compiles successful
var exampleSync: JsonFlowSchema = {
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

export function testJsonFlowSchema() {
}
