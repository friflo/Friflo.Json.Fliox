import Ajv, { ValidateFunction } from "ajv"
import fs from 'fs';
import path from 'path';


export function validateSchemas() {
    const ajv = new Ajv({allErrors: true}) // options can be passed, e.g. {allErrors: true}
    // runTest(ajv);

    const validate = ajv.getSchema("http://json-schema.org/draft-07/schema#") as ValidateFunction;

    const userStoreFiles        = getFiles("../JSON/UserStore/");
    const pocStoreFiles         = getFiles("../JSON/PocStore/");
    const entityIdStoreFiles    = getFiles("../JSON/EntityIdStore/");
    const jsonFlowSchemaFiles   = getFiles("../JSON/JsonFlowSchema/");

    const schemas : string[] = userStoreFiles.concat(pocStoreFiles, entityIdStoreFiles, jsonFlowSchemaFiles);

    for (var path of schemas) {
        console.log("validate: ", path);
        var json: string = fs.readFileSync(path, "utf8");
        var jsonSchema = JSON.parse(json);

        const valid = validate(jsonSchema);
        if (!valid)
            console.log(validate.errors)    
    }
}

function getFiles(folder: string) : string[] {
    var fileNames = fs.readdirSync(folder);
    fileNames = fileNames.filter(name => path.extname(name) === ".json");
    return fileNames.map(name => folder + name);
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




