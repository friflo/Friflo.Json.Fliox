import { Role } from "../UserStore/Friflo.Json.Flow.UserAuth"

var xxx: Role = {
    id: "some-id",
    rights: [
        {
            type: "allow",
            // grant: true,
            description: "allow description"
        },
        {
            type: "database",
            // containers: { },
            description: "test"
        }
    ]
}
