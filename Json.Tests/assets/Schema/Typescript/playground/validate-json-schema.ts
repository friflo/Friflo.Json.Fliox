import Ajv, { ValidateFunction } from "ajv"
import fs from 'fs';


function run() {
    const ajv = new Ajv({allErrors: true}) // options can be passed, e.g. {allErrors: true}
    // runTest(ajv);

    const validate = ajv.getSchema("http://json-schema.org/draft-07/schema#") as ValidateFunction;

    const schemas : string[] = [
        "../JSON/UserStore/Friflo.Json.Flow.Auth.Rights.json",
        "../JSON/UserStore/Friflo.Json.Flow.Sync.json",
        "../JSON/UserStore/Friflo.Json.Flow.UserAuth.Role.json",
        "../JSON/UserStore/Friflo.Json.Flow.UserAuth.UserCredential.json",
        "../JSON/UserStore/Friflo.Json.Flow.UserAuth.UserPermission.json",
    ];

    for (var path of schemas) {
        console.log("validate: ", path);
        var json: string = fs.readFileSync(path, "utf8");
        var jsonSchema = JSON.parse(json);

        const valid = validate(jsonSchema);
        if (!valid)
            console.log(validate.errors)    
    }
}

function runTest(ajv: Ajv) {
    
    const schema = {
        type: "object",
        properties: {
          foo: {type: "integer"},
          bar: {type: "string"}
        },
        required: ["foo"],
        additionalProperties: false,
    }      
      
    const data = {
        foo: "should error",
        bar: "abc"
    }

    const validate = ajv.compile(schema)
    const valid = validate(data)
    if (!valid)
        console.log(validate.errors)
}

run();



