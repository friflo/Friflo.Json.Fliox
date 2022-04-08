import fs from 'fs';
import { DocumentNode, GraphQLError, parse, SourceLocation } from 'graphql';
import { validate } from 'graphql/validation';
import { validateSDL } from 'graphql/validation/validate';


function readSchema(path: string) : string {
    return fs.readFileSync(path, "utf8");    
}

function validateSchema(path: string) {
    console.log("  validate: ", path);
    const schema:   string                  = readSchema(path);
    const document: DocumentNode            = parse(schema);
    const errors:   readonly GraphQLError[] = validateSDL(document);

    if (errors.length == 0)
        return;

    const base = `Json.Tests/assets~/Schema/Typescript/${path}`;

    for (var error of errors) {
        const loc   = error.locations ? error.locations[0] : { line: 0, column: 0}
        const msg   = `${base}(${loc.line},${loc.column}): error - ${error.message}`
        console.error(msg);
    }
    console.log();
}

export function validateGraphQLSchemas() {
    validateSchema("../GraphQL/ClusterStore/schema.graphql");
//  validateSchema("../GraphQL/MonitorStore/schema.graphql"); todo
    validateSchema("../GraphQL/PocStore/schema.graphql");
    validateSchema("../GraphQL/UserStore/schema.graphql");
}