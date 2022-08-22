import fs           from 'fs';
import * as path    from 'path';
import { DocumentNode, GraphQLError, parse, SourceLocation } from 'graphql';
import { validateSDL } from 'graphql/validation/validate';


function readSchema(path: string) : string {
    return fs.readFileSync(path, "utf8");    
}

function parseSchema(schema: string) : { doc?: DocumentNode, error?: GraphQLError} {
    try {
        return { doc: parse(schema) };
    }
    catch (error) {
        return { error: error as GraphQLError }
    }
}

function validateSchema(schemaPath: string) {
    console.log("  validate: ", schemaPath);
    const schema        = readSchema(schemaPath);
    const parseResult   = parseSchema(schema);
    let errors: readonly GraphQLError[];
    if (parseResult.error) {
        errors = [parseResult.error]
    } else {
        errors = validateSDL(parseResult.doc!);
    }
    if (errors.length == 0)
        return;
    const filePath  = `Json.Tests/assets~/Schema/Typescript/${schemaPath}`;
    const base      = path.normalize(filePath).split("\\").join("/");

    for (var error of errors) {
        const loc   = error.locations ? error.locations[0] : { line: 0, column: 0}
        const msg   = `${base}:${loc.line}:${loc.column} - error: ${error.message}`
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