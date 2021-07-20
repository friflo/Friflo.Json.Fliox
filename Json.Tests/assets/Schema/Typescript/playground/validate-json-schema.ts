import Ajv from "ajv"

const ajv = new Ajv({allErrors: true}) // options can be passed, e.g. {allErrors: true}

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
  foo: "ssss",
  bar: "abc"
}

const validate = ajv.compile(schema)
const valid = validate(data)
if (!valid) console.log(validate.errors)
