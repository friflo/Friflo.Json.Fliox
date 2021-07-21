import { Right_Union } from "../UserStore/Friflo.Json.Flow.Auth.Rights"
import { Role } from "../UserStore/Friflo.Json.Flow.UserAuth"

// check assignment with using a type compiles successful
var exampleRole: Role = {
    id: "some-id",
    rights: [
        {
            type:           "allow",
            grant:          true,
            description:    "allow description"
        },
        {
            type:           "database",
            containers:     { "Article": { operations:["read", "query", "update"], subscribeChanges: ["update"] }}
        },
        {
            type:           "message",
            names:          ["test-mess*"]
        },
        {
            type:           "subscribeMessage",
            names:          ["test-sub*"]
        },
        { 
            type:           "predicate",
            names:          ["TestPredicate"]
        },
        {
            type:           "task",
            types:          ["read"]
        }
    ]
}

// check using a Discriminated Union compiles successful
function usePolymorphType (right: Right_Union) {
    switch (right.type) {
        case "allow":
            var grant: boolean = right.grant;
            break;
        case "message":
            var names: string[] = right.names;
            break;
        case "predicate":
            var names: string[] = right.names;
            break;
    }
}
