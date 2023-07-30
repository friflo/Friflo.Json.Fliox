import { ProtocolMessage_Union } from "../Protocol/Friflo.Json.Fliox.Hub.Protocol"

// check assignment with using a type compiles successful
var exampleSync: ProtocolMessage_Union =
{
    msg: "sync",
    tasks: [
        {
            "task": "msg",
            "name": "std.Echo",
            "param": { "some": "data" }
        },
        {
            "task": "read",
            "cont": "Article",            
            "ids": ["article-galaxy", "article-ipad", 1, 2]
        },
        {
            "task": "read",
            "cont": "Order",
            "ids": ["order-1"],
            "references": [
                {
                    "selector": ".customer",
                    "cont": "Customer"
                }
            ]
        },
        {
            "task": "query",
            "cont": "Article"
        },
        {
            "task": "query",
            "cont": "Article",
            "filter": ".name == 'Smartphone'"
        },
        {
            "task": "query",
            "cont": "Article",
            "references": [
                {
                    "selector": ".producer",
                    "cont": "Producer",
                    "references": [
                        {
                            "selector": ".employees[*]",
                            "cont": "Employee"
                        }
                    ]
                }
            ]
        },
        {
            "task": "create",
            "cont": "Article",
            "keyName": "id",
            "set": [
                { "id": "new-article", "name":"New Article S10" }
            ]
        },
        {
            "task": "delete",
            "cont": "Article",
            "ids": ["new-article", 1, 2]
        },
        {
            "task": "merge",
            "cont": "Article",
            "set": [
                {
                    "id": "new-article",
                    "name": null
                }
            ]
        },
        {
            "task": "subscribeMessage",
            "name": "*",
            "remove": false
        },
        {
            "task":         "subscribeChanges",
            "cont":    "Article", 
            "changes": ["create", "upsert", "delete", "merge"]
        }
    ]
}

var exampleResponse: ProtocolMessage_Union =
{
    msg: "resp",
    tasks: [
        {
            "task": "query",
            "cont": "orders",
            "set": [{ "id": "new-article", "name":"New Article S10" }]
        }
    ]
}

export function testSync() {
}
