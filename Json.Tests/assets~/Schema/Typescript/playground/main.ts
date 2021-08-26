import { testEntityIdStore } from "./TestEntityIdStore";
import { testJsonSchema } from "./TestJsonFlowSchema";
import { testPocStore } from "./TestPocStore";
import { testSync } from "./TestSync";
import { testUserStore } from "./TestUserStore";
import { validateSchemas } from "./validate-json-schema";

validateSchemas()

testPocStore();
testSync()
testUserStore();
testEntityIdStore();
testJsonSchema();