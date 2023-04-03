// ----------------------------------------------- Schema -----------------------------------------------
export class Schema {
    static createEntitySchemas(databaseSchemas, dbSchemas) {
        /* for (const dbSchema of dbSchemas) {
            const jsonSchemas = dbSchema.jsonSchemas as { [key: string] : JSONSchema};
            delete jsonSchemas["openapi.json"];
        } */
        const schemaMap = {};
        for (const dbSchema of dbSchemas) {
            const jsonSchemas = dbSchema.jsonSchemas;
            const database = dbSchema.id;
            const containersByType = {};
            const rootSchema = jsonSchemas[dbSchema.schemaPath].definitions[dbSchema.schemaName];
            dbSchema._rootSchema = rootSchema;
            const containers = rootSchema.properties;
            for (const containerName in containers) {
                const container = containers[containerName];
                const containerType = container.additionalProperties.$ref;
                if (!containersByType[containerType]) {
                    containersByType[containerType] = [];
                }
                containersByType[containerType].push(containerName);
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
                    let schemaRef = { $ref: uri, _resolvedDef: null };
                    const containers = containersByType[schemaId];
                    if (containers) {
                        for (const container of containers) {
                            dbSchema._containerSchemas[container] = definition;
                        }
                        // entityEditor type can either be its entity type or an array using this type
                        schemaRef = { "oneOf": [schemaRef, { type: "array", items: schemaRef }], _resolvedDef: null };
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
        const commands = dbType.commands;
        for (const commandName in commands) {
            const command = commands[commandName];
            // assign file matcher for command param
            const paramType = Schema.replaceLocalRefsClone(database, command.param, schemaPath);
            Schema.addCommandArgument("message-param", database, commandName, paramType, schemaMap);
            // assign file matcher for command result
            const resultType = Schema.replaceLocalRefsClone(database, command.result, schemaPath);
            Schema.addCommandArgument("message-result", database, commandName, resultType, schemaMap);
        }
        const messages = dbType.messages;
        for (const messageName in messages) {
            const message = messages[messageName];
            // assign file matcher for command param
            const paramType = Schema.replaceLocalRefsClone(database, message.param, schemaPath);
            Schema.addCommandArgument("message-param", database, messageName, paramType, schemaMap);
            // note: messages have no result -> no return type
        }
    }
    // a command argument is either the command param or command result
    static addCommandArgument(argumentName, database, command, type, schemaMap) {
        const url = `${argumentName}://${database}.${command.toLocaleLowerCase()}.json`;
        /* if (type.$ref) {
            const uri       = "http://" + database + type.$ref.substring(1);
            const schema    = schemaMap[uri];
            schema.fileMatch.push(url); // requires a lower case string
            return;
        } */
        if (type == null) {
            type = { type: "null", _resolvedDef: null };
        }
        // create a new monaco schema with an uri that is never referenced
        // - created uri is unique and descriptive
        // - created uri allows resolving relative "$ref" types
        const uri = "http://" + database + "/" + command + "#/" + argumentName;
        const schema = {
            uri: uri,
            schema: type,
            fileMatch: [url]
        };
        schemaMap[uri] = schema;
    }
    static getResolvedType(type, schemaPath) {
        const $ref = type.$ref;
        if (!$ref)
            return type;
        if ($ref[0] != "#")
            return type;
        return { $ref: "./" + schemaPath + $ref, _resolvedDef: null };
    }
    static replaceLocalRefsClone(database, type, schemaPath) {
        if (!type) {
            return null;
        }
        const clone = JSON.parse(JSON.stringify(type));
        Schema.replaceLocalRefs(database, clone, schemaPath);
        return clone;
    }
    /** $ref uri's must be absolute. See {@link MonacoSchema.schema} */
    static replaceLocalRefs(database, node, schemaPath) {
        for (const propertyName in node) {
            const property = node[propertyName];
            switch (typeof property) {
                case "string":
                    if (propertyName == "$ref") {
                        const $ref = property;
                        if ($ref && $ref[0] == "#") {
                            node.$ref = "./" + schemaPath + $ref;
                        }
                        if (node.$ref.startsWith("./")) {
                            node.$ref = `http://${database}/` + node.$ref.substring(2); // replace "./"
                        }
                    }
                    break;
                case "object":
                    if (propertyName.startsWith("_")) {
                        node[propertyName] = null;
                        break;
                    }
                    Schema.replaceLocalRefs(database, property, schemaPath);
                    break;
            }
        }
    }
}
//# sourceMappingURL=schema.js.map