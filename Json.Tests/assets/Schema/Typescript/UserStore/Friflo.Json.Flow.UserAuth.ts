import { Right } from "./Friflo.Json.Flow.Auth.Rights"

export class Role {
    id:     string;
    rights: Right[];
}

export class UserCredential {
    id:       string;
    passHash: string;
    token:    string;
}

