import Ajv, { ValidateFunction } from "ajv"
import fs from 'fs';


function run() {
    const ajv = new Ajv({allErrors: true}) // options can be passed, e.g. {allErrors: true}
    // runTest(ajv);

    // var jsonSchemaString: string = fs.readFileSync("../JSON/json-schema.org/schema.json", "utf8");
    // var jsonSchema = JSON.parse(jsonSchemaString);


    const validate = ajv.getSchema("http://json-schema.org/draft-07/schema#") as ValidateFunction;

    const schema = {
        type: "object",
        properties: {
          foo: {type: "integer"},
          bar: {type: "string"}
        },
        required: ["foo"],
        additionalProperties: false,
    }  

    const valid = validate(schema);
    if (!valid)
        console.log(validate.errors)    
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



