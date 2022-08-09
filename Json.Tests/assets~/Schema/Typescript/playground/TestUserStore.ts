import { Right_Union } from "../UserStore/Friflo.Json.Fliox.Hub.Host.Auth.Rights"
import { Role } from "../UserStore/Friflo.Json.Fliox.Hub.DB.UserAuth"

// check assignment with using a type compiles successful
var exampleRole: Role = {
    id: "some-id",
    rights: [
        {
            type:           "allow",
            database:       "db",
            description:    "allow description"
        },
        {
            type:           "operation",
            database:       "db",
            containers:     [ { name: "articles", operations:["read", "query", "upsert"], subscribeChanges: ["upsert"] }]
        },
        {
            type:           "sendMessage",
            database:       "db",
            names:          ["test-mess*"]
        },
        {
            type:           "subscribeMessage",
            database:       "db",
            names:          ["test-sub*"]
        },
        { 
            type:           "predicate",
            names:          ["TestPredicate"]
        },
        {
            type:           "task",
            database:       "db",
            types:          ["read"]
        }
    ]
}

// check using a Discriminated Union compiles successful
function usePolymorphType (right: Right_Union) {
    switch (right.type) {
        case "allow":
            break;
        case "sendMessage":
            var names: string[] = right.names;
            break;
        case "predicate":
            var names: string[] = right.names;
            break;
    }
}

export function testUserStore() {
}

