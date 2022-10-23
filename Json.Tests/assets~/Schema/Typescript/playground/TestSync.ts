import { ProtocolMessage_Union } from "../Protocol/Friflo.Json.Fliox.Hub.Protocol"

// check assignment with using a type compiles successful
var exampleSync: ProtocolMessage_Union =
{
    msg: "sync",
    tasks: [
        {
            "task":         "message",
            "name":         "DbEcho",
            "param":        { "some": "data" }
        },
        {
            "task":         "read",
            "container":    "Article",            
            "ids": ["article-galaxy", "article-ipad"]            
        },
        {
            "task":         "read",
            "container":    "Order",
            "ids": ["order-1"],
            "references": [
                {
                    "selector": ".customer",
                    "container": "Customer"
                }
            ]
        },
        {
            "task": "query",
            "container": "Article"
        },
        {
            "task":         "query",
            "container":    "Article",
            "filter":       ".name == 'Smartphone'"
        },
        {
            "task":         "query",
            "container":    "Article",
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
            "task":         "merge",
            "container":    "Article",
            "patches": [
                {
                    "id": "new-article",
                    "name": null
                }
            ]
        },
        {
            "task":         "subscribeMessage",
            "name":         "*",
            "remove":   false
        },
        {
            "task":         "subscribeChanges",
            "container":    "Article", 
            "changes": ["create", "upsert", "delete", "merge"]
        }
    ]
}

export function testSync() {
}
