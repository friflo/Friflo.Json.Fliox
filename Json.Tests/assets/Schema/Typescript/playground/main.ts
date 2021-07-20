import { Role } from "../UserStore/Friflo.Json.Flow.UserAuth"

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
            containers:     { "Article": { operations:["read", "query", "update"], subscribeChanges: ["update"] }},
            description:    "test"
        },
        {
            type:           "message",
            description:    "some text",
            names:          ["test-mess*"]
        },
        {
            type:           "subscribeMessage",
            description:    "some text",
            names:          ["test-sub*"]
        },
        { 
            type:           "predicate",
            description:    "some text",
            names:          ["TestPredicate"]
        },
        {
            type:           "task",
            description:    "some text",
            types:          ["read"]
        }
    ]
}
