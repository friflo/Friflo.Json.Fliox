// ----------------------------------------------- Schema -----------------------------------------------
export class Schema {
    static createEntitySchemas(databaseSchemas, dbSchemas) {
        const schemaMap = {};
        for (const dbSchema of dbSchemas) {
            const jsonSchemas = dbSchema.jsonSchemas;
            const database = dbSchema.id;
            const containerRefs = {};
            const rootSchema = jsonSchemas[dbSchema.schemaPath].definitions[dbSchema.schemaName];
            dbSchema._rootSchema = rootSchema;
            const containers = rootSchema.properties;
            for (const containerName in containers) {
                const container = containers[containerName];
                containerRefs[container.additionalProperties.$ref] = containerName;
            }
            databaseSchemas[database] = dbSchema;
            dbSchema._containerSchemas = {};
            // add all schemas and their definitions to schemaMap and map them to an uri like:
            //   http://main_db/Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json
            //   http://main_db/Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json#/definitions/PocStore
            for (const schemaPath in jsonSchemas) {
                const schema = jsonSchemas[schemaPath];
                const uri = "http://" + database + "/" + schemaPath;
                const schemaEntry = {
                    uri: uri,
                    schema: schema,
                    fileMatch: [],
                    _resolvedDef: schema // not part of monaco > DiagnosticsOptions.schemas
                };
                const namespace = schemaPath.substring(0, schemaPath.length - ".json".length);
                schemaMap[uri] = schemaEntry;
                const definitions = schema.definitions;
                const baseRefType = schema.$ref ? schema.$ref.substring('#/definitions/'.length) : undefined;
                for (const definitionName in definitions) {
                    const definition = definitions[definitionName];
                    definition._typeName = definitionName;
                    definition._namespace = namespace;
                    if (definitionName == baseRefType) {
                        definition._namespace = namespace.substring(0, namespace.length - definitionName.length - 1);
                    }
                    // console.log("---", definition._namespace, definitionName);
                    const path = "/" + schemaPath + "#/definitions/" + definitionName;
                    const schemaId = "." + path;
                    const uri = "http://" + database + path;
                    const containerName = containerRefs[schemaId];
                    let schemaRef = { $ref: schemaId };
                    if (containerName) {
                        dbSchema._containerSchemas[containerName] = definition;
                        // entityEditor type can either be its entity type or an array using this type
                        schemaRef = { "oneOf": [schemaRef, { type: "array", items: schemaRef }] };
                    }
                    // add reference for definitionName pointing to definition in current schemaPath
                    const definitionEntry = {
                        uri: uri,
                        schema: schemaRef,
                        fileMatch: [],
                        _resolvedDef: definition // not part of monaco > DiagnosticsOptions.schemas
                    };
                    schemaMap[uri] = definitionEntry;
                }
            }
            Schema.resolveRefs(jsonSchemas);
            Schema.addFileMatcher(database, dbSchema, schemaMap);
        }
        return schemaMap;
    }
    static resolveRefs(jsonSchemas) {
        for (const schemaPath in jsonSchemas) {
            // if (schemaPath == "Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Order.json") debugger;
            const schema = jsonSchemas[schemaPath];
            Schema.resolveNodeRefs(jsonSchemas, schema, schema);
        }
    }
    static resolveNodeRefs(jsonSchemas, schema, node) {
        const nodeType = typeof node;
        if (nodeType != "object")
            return;
        if (Array.isArray(node))
            return;
        const ref = node.$ref;
        if (ref) {
            if (ref[0] == "#") {
                const localName = ref.substring("#/definitions/".length);
                node._resolvedDef = schema.definitions[localName];
            }
            else {
                const localNamePos = ref.indexOf("#");
                const schemaPath = ref.substring(2, localNamePos); // start after './'
                const localName = ref.substring(localNamePos + "#/definitions/".length);
                const globalSchema = jsonSchemas[schemaPath];
                node._resolvedDef = globalSchema.definitions[localName];
            }
        }
        for (const propertyName in node) {
            if (propertyName == "_resolvedDef")
                continue;
            // if (propertyName == "dateTimeNull") debugger;
            const property = node[propertyName];
            const fieldType = Schema.getFieldType(property);
            this.resolveNodeRefs(jsonSchemas, schema, fieldType.type); // todo fix cast            
        }
    }
    static getFieldType(fieldType) {
        const oneOf = fieldType.oneOf;
        if (!oneOf)
            return { type: fieldType, isNullable: false };
        let isNullable = false;
        let oneOfType = null;
        for (const item of oneOf) {
            if (item.type == "null") {
                isNullable = true;
                continue;
            }
            oneOfType = item;
        }
        return { type: oneOfType, isNullable };
    }
    // add a "fileMatch" property to all container entity type schemas used for editor validation
    static addFileMatcher(database, dbSchema, schemaMap) {
        const jsonSchemas = dbSchema.jsonSchemas;
        const schemaName = dbSchema.schemaName;
        const schemaPath = dbSchema.schemaPath;
        const jsonSchema = jsonSchemas[schemaPath];
        const dbType = jsonSchema.definitions[schemaName];
        const containers = dbType.properties;
        for (const containerName in containers) {
            const container = containers[containerName];
            const containerType = Schema.getResolvedType(container.additionalProperties, schemaPath);
            const uri = "http://" + database + containerType.$ref.substring(1);
            const schema = schemaMap[uri];
            const url = `entity://${database}.${containerName.toLocaleLowerCase()}.json`;
            schema.fileMatch.push(url); // requires a lower case string
        }
        const commandType = jsonSchema.definitions[schemaName];
        const commands = commandType.commands;
        for (const commandName in commands) {
            const command = commands[commandName];
            // assign file matcher for command param
            const paramType = Schema.getResolvedType(command.param, schemaPath);
            let url = `command-param://${database}.${commandName.toLocaleLowerCase()}.json`;
            if (paramType.$ref) {
                const uri = "http://" + database + paramType.$ref.substring(1);
                const schema = schemaMap[uri];
                schema.fileMatch.push(url); // requires a lower case string
            }
            else {
                // uri is never referenced - create an arbitrary unique uri
                const uri = "http://" + database + "/command/param" + commandName;
                const schema = {
                    uri: uri,
                    schema: paramType,
                    fileMatch: [url]
                };
                schemaMap[uri] = schema;
            }
            // assign file matcher for command result
            const resultType = Schema.getResolvedType(command.result, schemaPath);
            url = `command-result://${database}.${commandName.toLocaleLowerCase()}.json`;
            if (resultType.$ref) {
                const uri = "http://" + database + resultType.$ref.substring(1);
                const schema = schemaMap[uri];
                schema.fileMatch.push(url); // requires a lower case string
            }
            else {
                // uri is never referenced - create an arbitrary unique uri
                const uri = "http://" + database + "/command/result" + commandName;
                const schema = {
                    uri: uri,
                    schema: resultType,
                    fileMatch: [url]
                };
                schemaMap[uri] = schema;
            }
        }
    }
    static getResolvedType(type, schemaPath) {
        const $ref = type.$ref;
        if (!$ref)
            return type;
        if ($ref[0] != "#")
            return type;
        return { $ref: "./" + schemaPath + $ref };
    }
}
//# sourceMappingURL=schema.js.map