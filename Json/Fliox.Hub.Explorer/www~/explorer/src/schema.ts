import { FieldType, JsonType, JSONSchema }  from "../../../../../Json.Tests/assets~/Schema/Typescript/JSONSchema/Friflo.Json.Fliox.Schema.JSON";
import { DbSchema }                         from "../../../../../Json.Tests/assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster";

declare module "../../../../../Json.Tests/assets~/Schema/Typescript/ClusterStore/Friflo.Json.Fliox.Hub.DB.Cluster" {
    interface DbSchema {
        _rootSchema:        JsonType;
        _containerSchemas : { [key: string] : JsonType };        
    }
}

declare module "../../../../../Json.Tests/assets~/Schema/Typescript/JSONSchema/Friflo.Json.Fliox.Schema.JSON" {
    interface JsonType {
        _typeName:      string;
        _namespace:     string;
        _resolvedDef:   JsonType;
    }

    interface JSONSchema {
        oneOf?:         any[],
        _resolvedDef:   JsonType;
    }

    interface FieldType {
        _resolvedDef:   JsonType;
    }
}

export type MonacoSchema = {
    readonly uri: string;
    /**
     * A list of glob patterns that describe for which file URIs the JSON schema will be used.
     * '*' and '**' wildcards are supported. Exclusion patterns start with '!'.
     * For example '*.schema.json', 'package.json', '!foo*.schema.json', 'foo/**\/BADRESP.json'.
     * A match succeeds when there is at least one pattern matching and last matching pattern does not start with '!'.
     */
             fileMatch?: string[];
    /**
     * The schema for the given URI.
     * NOTE !!!
     * Since monaco-editor 0.34.0-dev.20220401
     * "$ref" properties used by monaco.languages.json.jsonDefaults.setDiagnosticsOptions({schemas: MonacoSchema[]})
     * must not use relative uri's.                     E.g. { "$ref": "./foo-schema.json#/definitions/Main" }
     * Since 0.34.0 uri's are required to be absolute.  E.g. { "$ref": "http://myserver/foo-schema.json#/definitions/Main", }
     */
    readonly schema?: JSONSchema;

    _resolvedDef?: JSONSchema
}


// ----------------------------------------------- Schema -----------------------------------------------
export class Schema
{
    public static createEntitySchemas (databaseSchemas: { [key: string]: DbSchema}, dbSchemas: DbSchema[]) : {[key: string]: MonacoSchema } {
        /* for (const dbSchema of dbSchemas) {
            const jsonSchemas = dbSchema.jsonSchemas as { [key: string] : JSONSchema};
            delete jsonSchemas["openapi.json"];
        } */
        const schemaMap: { [key: string]: MonacoSchema } = {};
        for (const dbSchema of dbSchemas) {
            const jsonSchemas       = dbSchema.jsonSchemas as { [key: string] : JSONSchema};
            const database          = dbSchema.id;
            const containersByType  = {} as { [key: string] : string[] };
            const rootSchema        = jsonSchemas[dbSchema.schemaPath].definitions[dbSchema.schemaName];
            dbSchema._rootSchema    = rootSchema;
            const containers        = rootSchema.properties;
            for (const containerName in containers) {
                const container     = containers[containerName];
                const containerType = container.additionalProperties.$ref;
                if (!containersByType[containerType]) { containersByType[containerType] = []; }
                containersByType[containerType].push(containerName);
            }
            databaseSchemas[database]  = dbSchema;
            dbSchema._containerSchemas = {};

            // add all schemas and their definitions to schemaMap and map them to an uri like:
            //   http://main_db/Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json
            //   http://main_db/Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json#/definitions/PocStore
            for (const schemaPath in jsonSchemas) {
                const schema      = jsonSchemas[schemaPath];
                const uri         = "http://" + database + "/" + schemaPath;
                const schemaEntry: MonacoSchema = {
                    uri:            uri,
                    schema:         schema,
                    fileMatch:      [], // can have multiple in case schema is used by multiple editor models
                    _resolvedDef:   schema // not part of monaco > DiagnosticsOptions.schemas
                };
                const namespace     = schemaPath.substring(0, schemaPath.length - ".json".length);
                schemaMap[uri]      = schemaEntry;
                const definitions   = schema.definitions;
                const baseRefType   = schema.$ref ? schema.$ref.substring('#/definitions/'.length) : undefined;
                for (const definitionName in definitions) {
                    const definition        = definitions[definitionName];
                    definition._typeName    = definitionName;
                    definition._namespace   = namespace;
                    if (definitionName == baseRefType) {
                        definition._namespace = namespace.substring(0, namespace.length - definitionName.length - 1);
                    }
                    // console.log("---", definition._namespace, definitionName);
                    const path          = "/" + schemaPath + "#/definitions/" + definitionName;
                    const schemaId      = "." + path;
                    const uri           = "http://" + database + path;
                    let schemaRef : JSONSchema = { $ref: uri, _resolvedDef: null };
                    const containers    = containersByType[schemaId];
                    if (containers) {
                        for (const container of containers) {
                            dbSchema._containerSchemas[container] = definition;
                        }
                        // entityEditor type can either be its entity type or an array using this type
                        schemaRef = { "oneOf": [schemaRef, { type: "array", items: schemaRef } ], _resolvedDef: null };
                    }
                    // add reference for definitionName pointing to definition in current schemaPath
                    const definitionEntry: MonacoSchema = {
                        uri:            uri,
                        schema:         schemaRef,
                        fileMatch:      [], // can have multiple in case schema is used by multiple editor models
                        _resolvedDef:   definition // not part of monaco > DiagnosticsOptions.schemas
                    };
                    schemaMap[uri] = definitionEntry;
                }
            }
            Schema.resolveRefs(jsonSchemas);
            Schema.addFileMatcher(database, dbSchema, schemaMap);
        }
        return schemaMap;
    }

    private static resolveRefs(jsonSchemas: { [key: string] : JSONSchema }) {
        for (const schemaPath in jsonSchemas) {
            // if (schemaPath == "Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Order.json") debugger;
            const schema      = jsonSchemas[schemaPath];
            Schema.resolveNodeRefs(jsonSchemas, schema, schema);
        }
    }

    private static resolveNodeRefs(jsonSchemas: { [key: string] : JSONSchema }, schema: JSONSchema, node: JSONSchema) {
        const nodeType = typeof node;
        if (nodeType != "object")
            return;
        if (Array.isArray(node))
            return;
        const ref = node.$ref;
        if (ref) {
            if (ref[0] == "#") {
                const localName     = ref.substring("#/definitions/".length);
                node._resolvedDef   = schema.definitions[localName];
            } else {
                const localNamePos  = ref.indexOf ("#");
                const schemaPath    = ref.substring(2, localNamePos); // start after './'
                const localName     = ref.substring(localNamePos + "#/definitions/".length);
                const globalSchema  = jsonSchemas[schemaPath];
                node._resolvedDef   = globalSchema.definitions[localName];
            }
        }
        for (const propertyName in node) {
            if (propertyName == "_resolvedDef")
                continue;
            // if (propertyName == "dateTimeNull") debugger;
            const property  = (node as any)[propertyName] as FieldType;
            const fieldType = Schema.getFieldType(property);
            this.resolveNodeRefs(jsonSchemas, schema, fieldType.type as JSONSchema); // todo fix cast            
        }
    }

    public static getFieldType(fieldType: FieldType) : { type: FieldType, isNullable: boolean } {
        const oneOf     = fieldType.oneOf;
        if (!oneOf)
            return { type: fieldType, isNullable: false };
        let isNullable              = false;
        let oneOfType: FieldType    = null;
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
    private static addFileMatcher(database: string, dbSchema: DbSchema, schemaMap: { [key: string]: MonacoSchema }) {
        const jsonSchemas     = dbSchema.jsonSchemas as { [key: string] : JSONSchema};
        const schemaName      = dbSchema.schemaName;
        const schemaPath      = dbSchema.schemaPath;
        const jsonSchema      = jsonSchemas[schemaPath];
        const dbType          = jsonSchema.definitions[schemaName];
        const containers      = dbType.properties;
        for (const containerName in containers) {
            const container     = containers[containerName];
            const containerType = Schema.getResolvedType(container.additionalProperties, schemaPath);
            const uri       = "http://" + database + containerType.$ref.substring(1);
            const schema    = schemaMap[uri];
            const url       = `entity://${database}.${containerName.toLocaleLowerCase()}.json`;
            schema.fileMatch.push(url); // requires a lower case string
        }
        const commands        = dbType.commands;
        for (const commandName in commands) {
            const command   = commands[commandName];
            // assign file matcher for command param
            const paramType = Schema.replaceLocalRefsClone(database, command.param, schemaPath);
            Schema.addCommandArgument("message-param", database, commandName, paramType, schemaMap);

            // assign file matcher for command result
            const resultType   = Schema.replaceLocalRefsClone(database, command.result, schemaPath);
            Schema.addCommandArgument("message-result", database, commandName, resultType, schemaMap);
        }
        const messages        = dbType.messages;
        for (const messageName in messages) {
            const message   = messages[messageName];
            // assign file matcher for command param
            const paramType = Schema.replaceLocalRefsClone(database, message.param, schemaPath);
            Schema.addCommandArgument("message-param", database, messageName, paramType, schemaMap);
            // note: messages have no result -> no return type
        }
    }

    // a command argument is either the command param or command result
    private static addCommandArgument (
        argumentName:   string,
        database:       string,
        command:        string,
        type:           FieldType,
        schemaMap:      { [key: string]: MonacoSchema }
    ) : void
    {
        const url = `${argumentName}://${database}.${command.toLocaleLowerCase()}.json`;
        /* if (type.$ref) {
            const uri       = "http://" + database + type.$ref.substring(1);
            const schema    = schemaMap[uri];
            schema.fileMatch.push(url); // requires a lower case string
            return;
        } */
        if (type == null) {
            type = { type: "null", _resolvedDef: null};
        }
        // create a new monaco schema with an uri that is never referenced
        // - created uri is unique and descriptive
        // - created uri allows resolving relative "$ref" types
        const uri = "http://" + database + "/" + command + "#/" + argumentName;
        const schema: MonacoSchema = {
            uri:        uri,
            schema:     type,
            fileMatch:  [url]
        };
        schemaMap[uri] = schema;
    }

    private static getResolvedType (type: FieldType, schemaPath: string) : FieldType {
        const $ref = type.$ref;
        if (!$ref)
            return type;
        if ($ref[0] != "#")
            return type;
        return { $ref: "./" + schemaPath + $ref, _resolvedDef: null };
    }

    private static replaceLocalRefsClone (database: string, type: FieldType, schemaPath: string) : FieldType {
        if (!type) {
            return null;
        }
        const clone = JSON.parse(JSON.stringify(type)) as FieldType;
        Schema.replaceLocalRefs(database, clone, schemaPath);
        return clone;
    }

    /** $ref uri's must be absolute. See {@link MonacoSchema.schema} */
    private static replaceLocalRefs (database: string, node: any, schemaPath: string) : void {
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