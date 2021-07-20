import { Role } from "../UserStore/Friflo.Json.Flow.UserAuth"

var exampleRole: Role = {
    id: "some-id",
    rights: [
        {
            type: "allow",
            grant: true,
            description: "allow description"
        },
        {
            type: "database",
            containers: { "Article": { operations:["read", "query", "update"], subscribeChanges: ["update"] }},
            description: "test"
        }
    ]
}
