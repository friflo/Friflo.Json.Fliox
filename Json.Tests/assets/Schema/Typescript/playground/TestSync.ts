import { DatabaseMessage } from "../Sync/Friflo.Json.Flow.Sync"

// check assignment with using a type compiles successful
var exampleSync: DatabaseMessage = {
    req: {
        type: "sync",
        tasks: [
            {
                "task":         "message",
                "name":         "Echo",
                "value":        { "some": "data" }
            },
            {
                "task":         "read",
                "container":    "Article",
                "reads": [
                    { "ids": ["article-galaxy", "article-ipad"] }
                ]
            },
            {
                "task":         "read",
                "container":    "Order",
                "reads": [
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
                "filterLinq": "true",
                "filter": { "op":"true" }
            },
            {
                "task":         "query",
                "container":    "Article",
                "filterLinq":   "true",
                "filter": {
                    "op":"equal",
                    "left": {"op":"field",  "name":  ".name"},
                    "right":{"op":"string", "value": "Smartphone"}
                }
            },
            {
                "task":         "query",
                "container":    "Article",
                "filterLinq":   "true",
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
                "entities": {
                    "new-article": {
                        "value": {
                            "id": "new-article",
                            "name":"New Article S10"
                        }
                    }
                }
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
                "changes": ["create", "update", "delete", "patch"]
            }
        ]
    }
}
