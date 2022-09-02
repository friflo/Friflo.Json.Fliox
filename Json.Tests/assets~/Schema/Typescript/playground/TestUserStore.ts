import { Right_Union } from "../UserStore/Friflo.Json.Fliox.Hub.Host.Auth.Rights"
import { Role } from "../UserStore/Friflo.Json.Fliox.Hub.DB.UserAuth"

// check assignment with using a type compiles successful
var exampleRole: Role = {
    id: "some-id",
    rights: [
        {
            type:           "db",
            database:       "db-name",
            description:    "allow description"
        },
        {
            type:           "dbContainer",
            database:       "db-name",
            containers:     [ { name: "articles", operations:["read", "query", "upsert"], subscribeChanges: ["upsert"] }]
        },
        {
            type:           "sendMessage",
            database:       "db-name",
            names:          ["test-mess*"]
        },
        {
            type:           "subscribeMessage",
            database:       "db-name",
            names:          ["test-sub*"]
        },
        { 
            type:           "predicate",
            names:          ["TestPredicate"]
        },
        {
            type:           "dbTask",
            database:       "db-name",
            types:          ["read"]
        }
    ]
}

// check using a Discriminated Union compiles successful
function usePolymorphType (right: Right_Union) {
    switch (right.type) {
        case "db":
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

