import { ProtocolMessage_Union } from "../Protocol/Friflo.Json.Fliox.Hub.Protocol"

// check assignment with using a type compiles successful
var exampleSync: ProtocolMessage_Union =
{
    msg: "sync",
    tasks: [
        {
            "task":         "message",
            "name":         "Echo",
            "value":        { "some": "data" }
        },
        {
            "task":         "read",
            "container":    "Article",
            "sets": [
                { "ids": ["article-galaxy", "article-ipad"] }
            ]
        },
        {
            "task":         "read",
            "container":    "Order",
            "sets": [
                {
                    "ids": ["order-1"],
                    "references": [
                        {
                            "selector": ".customer",
                            "container": "Customer"
                        }
                    ]
                }
            ]
        },
        {
            "task": "query",
            "container": "Article",
            "filter": { "op":"true" }
        },
        {
            "task":         "query",
            "container":    "Article",
            "filter": {
                "op":"equal",
                "left": {"op":"field",  "name":  ".name"},
                "right":{"op":"string", "value": "Smartphone"}
            }
        },
        {
            "task":         "query",
            "container":    "Article",
            "filter":       { "op":"true" },
            "references": [
                {
                    "selector": ".producer",
                    "container": "Producer",
                    "references": [
                        {
                            "selector": ".employees[*]",
                            "container": "Employee"
                        }
                    ]
                }
            ]
        },
        {
            "task":         "create",
            "container":    "Article",
            "keyName":      "id",
            "entities": [
                { "id": "new-article", "name":"New Article S10" }
            ]
        },
        {
            "task":         "delete",
            "container":    "Article",
            "ids": ["new-article"]
        },
        {
            "task":         "patch",
            "container":    "Article",
            "patches": {
                "new-article": {
                    "patches": [
                        {
                            "op":"replace",
                            "path": ".name",
                            "value": null
                        }
                    ]
                }
            }
        },
        {
            "task":         "subscribeMessage",
            "name":         "*",
            "remove":   false
        },
        {
            "task":         "subscribeChanges",
            "container":    "Article", 
            "changes": ["create", "upsert", "delete", "patch"]
        }
    ]
}

export function testSync() {
}
