import { testEntityIdStore } from "./TestEntityIdStore";
import { testJsonSchema } from "./TestJsonFlowSchema";
import { testPocStore } from "./TestPocStore";
import { testSync } from "./TestSync";
import { testUserStore } from "./TestUserStore";
import { validateSchemas } from "./validate-json-schema";
import { validateGraphQLSchemas } from "./validate-graphql-schema";

console.log("------------------------------------ validate GraphQL schemas ------------------------------------");
validateGraphQLSchemas()
console.log();

console.log("------------------------------------ validate JSON Schemas ------------------------------------");
validateSchemas()
console.log();

console.log("------------------------------------ Typescript playground ------------------------------------");
testPocStore();
testSync()
testUserStore();
testEntityIdStore();
testJsonSchema();