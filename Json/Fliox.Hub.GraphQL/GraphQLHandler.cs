// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;
using GraphQLParser;
using GraphQLParser.AST;


#pragma warning disable CS0649
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    public class GraphQLHandler: IRequestHandler
    {
        private const   string  GraphQLRoute = "/graphql";
        
        public bool IsMatch(RequestContext context) {
            if (context.method != "POST")
                return false;
            return context.route == GraphQLRoute;
        }

        public async Task HandleRequest(RequestContext context) {
            var body    = await JsonValue.ReadToEndAsync(context.body).ConfigureAwait(false);
            var pool    = context.Pool;
            GraphQLPost postBody;
            using (var pooled = pool.ObjectMapper.Get()) {
                var reader  = pooled.instance.reader;
                postBody    = reader.Read<GraphQLPost>(body);
            }
            var query       = Parser.Parse(postBody.query);
            switch (postBody.operationName) {
                case "IntrospectionQuery":
                    IntrospectionQuery(context, query);
                    return;
                default:
                    context.WriteError("Invalid operation", postBody.operationName, 400);
                    return;
            }
        }
        
        // ReSharper disable once UnusedParameter.Local
        private static void IntrospectionQuery (RequestContext context, GraphQLDocument query) {
            // var queryString = query.Source.ToString();
            // Console.WriteLine("-------------------------------- query --------------------------------");
            // Console.WriteLine(queryString);
            
            var types       = new List<GqlType> {
                new GqlScalar { name = "Boolean" },
                new GqlScalar { name = "String"  },
                new GqlScalar { name = "ID"  },
                new GqlObject { name = "Query",
                    fields = new List<GqlField> {
                        new GqlField { name = "_entities",
                            args = new List<GqlArg> {
                                new GqlArg { name = "representations",
                                    type = new GqlNonNull {
                                        ofType = new GqlList {
                                            ofType = new GqlNonNull {
                                                ofType = new GqlScalar{ name = "_Any" }
                                            }
                                        }
                                    }
                                }
                            },
                            type = new GqlNonNull {
                                ofType = new GqlList {
                                    ofType = new GqlUnion { name = "_Entity" }
                                }
                            }
                        },
                        new GqlField { name = "_service",
                            type = new GqlNonNull {
                                ofType = new GqlObject{ name = "_Service",
                                    fields = new List<GqlField>()
                                }
                            }
                        } 
                    }
                },
                new GqlUnion  { name = "_Entity" }, // possibleTypes?
                new GqlScalar { name = "_Any" },
                new GqlObject { name = "_Service",
                    fields = new List<GqlField> {
                        new GqlField { name = "sdl",
                            type = new GqlScalar{ name = "String" }
                        }
                    }
                },
                new GqlObject { name = "__Schema",
                    fields = new List<GqlField> {
                        new GqlField { name = "description",
                            type = new GqlScalar{ name = "String" }
                        },
                        new GqlField { name = "types",
                            type = new GqlNonNull {
                                ofType = new GqlList {
                                    ofType = new GqlNonNull {
                                        ofType = new GqlObject { name = "__Type" }
                                    }
                                }
                            }
                        },
                        new GqlField { name = "queryType",
                            type = new GqlNonNull {
                                ofType = new GqlObject { name = "__Type" }
                            }
                        },
                        new GqlField { name = "mutationType",
                            type = new GqlNonNull {
                                ofType = new GqlObject { name = "__Type" }
                            }
                        },
                        new GqlField { name = "subscriptionType",
                            type = new GqlNonNull {
                                ofType = new GqlObject { name = "__Type" }
                            }
                        },
                        new GqlField { name = "directives",
                            type = new GqlNonNull {
                                ofType = new GqlList {
                                    ofType = new GqlNonNull {
                                        ofType = new GqlObject { name = "__Directive" }
                                    }
                                }
                            }
                        },
                    }
                },
                new GqlObject { name = "__Type",
                    fields = new List<GqlField> {
                        new GqlField { name = "kind",
                            args = new List<GqlArg>(),
                            type = new GqlNonNull {
                                ofType = new GqlEnum { name = "__TypeKind" }
                            }
                        },
                        new GqlField { name = "name",
                            args = new List<GqlArg>(),
                            type = new GqlScalar { name = "String" }
                        },
                        new GqlField { name = "description",
                            args = new List<GqlArg>(),
                            type = new GqlScalar { name = "String" }
                        },
                        new GqlField { name = "specifiedByUrl",
                            args = new List<GqlArg>(),
                            type = new GqlScalar { name = "String" }
                        },
                        new GqlField { name = "fields",
                            args = new List<GqlArg> {
                                new GqlArg {
                                    name = "includeDeprecated",
                                    type = new GqlScalar() { name = "Boolean" } 
                                }
                            },
                            type = new GqlList {
                                ofType = new GqlNonNull {
                                    ofType = new GqlObject { name = "__Field" }
                                }
                            }
                        },
                        new GqlField { name = "interfaces",
                            args = new List<GqlArg>(),
                            type = new GqlList {
                                ofType = new GqlNonNull {
                                    ofType = new GqlObject { name = "__Type"}
                                }
                            }
                        },
                        new GqlField { name = "possibleTypes",
                            args = new List<GqlArg>(),
                            type = new GqlList {
                                ofType = new GqlNonNull {
                                    ofType = new GqlObject { name = "__Type"}
                                }
                            }
                        },
                        new GqlField { name = "enumValues",
                            args = new List<GqlArg> {
                                new GqlArg { name = "includeDeprecated",
                                    type = new GqlScalar { name = "Boolean" }
                                }
                            },
                            type = new GqlList {
                                ofType = new GqlNonNull {
                                    ofType = new GqlObject { name = "__EnumValue"}
                                }
                            }
                        },
                        new GqlField { name = "inputFields",
                            args = new List<GqlArg> {
                                new GqlArg { name = "includeDeprecated",
                                    type = new GqlScalar { name = "Boolean" }
                                }
                            },
                            type = new GqlList {
                                ofType = new GqlNonNull {
                                    ofType = new GqlObject { name = "__InputValue"}
                                }
                            }
                        },
                        new GqlField { name = "ofType",
                            args = new List<GqlArg>(),
                            type = new GqlObject { name = "__Type" }
                        },
                    }
                },
                new GqlEnum { name = "__TypeKind",
                    enumValues = new List<GqlEnumValue> {
                        new GqlEnumValue { name = "SCALAR"       },
                        new GqlEnumValue { name = "OBJECT"       },
                        new GqlEnumValue { name = "INTERFACE"    },
                        new GqlEnumValue { name = "UNION"        },
                        new GqlEnumValue { name = "ENUM"         },
                        new GqlEnumValue { name = "INPUT_OBJECT" },
                        new GqlEnumValue { name = "LIST"         },
                        new GqlEnumValue { name = "NON_NULL"     },
                    } 
                },
                new GqlObject { name = "__Field",
                    fields = new List<GqlField> {
                        new GqlField { name = "name",
                            args = new List<GqlArg>(),
                            type = new GqlNonNull {
                                ofType = new GqlScalar { name = "String" }
                            }
                        },
                        new GqlField { name = "description",
                            args = new List<GqlArg>(),
                            type = new GqlScalar { name = "String" }
                        },
                        new GqlField { name = "args",
                            args = new List<GqlArg> {
                                new GqlArg { name = "includeDeprecated",
                                    type = new GqlScalar { name = "Boolean" }
                                }
                            },
                            type = new GqlNonNull {
                                ofType = new GqlList {
                                    ofType = new GqlNonNull {
                                        ofType = new GqlObject { name = "__InputValue" }
                                    }
                                }
                            }
                        },
                        new GqlField { name = "type",
                            args = new List<GqlArg>(),
                            type = new GqlNonNull {
                                ofType = new GqlObject { name = "__Type" }
                            }
                        },
                        new GqlField { name = "isDeprecated",
                            args = new List<GqlArg>(),
                            type = new GqlNonNull {
                                ofType = new GqlScalar { name = "Boolean" }
                            }
                        },
                        new GqlField { name = "deprecationReason",
                            args = new List<GqlArg>(),
                            type = new GqlScalar { name = "String" }
                        },
                    }
                }
            };
            
            var schema = new GqlSchema {
                queryType   = new GqlQueryType { name = "Query" },
                types       = types,
                directives  = new List<GqlDirective>()
            };
            var response = new GqlResponse {
                data = new GqlData {
                    schema = schema
                }
            };
            var pool        = context.Pool;
            using (var pooled = pool.ObjectMapper.Get()) {
                var writer              = pooled.instance.writer;
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                var responseBody        = new JsonValue(writer.WriteAsArray(response));
                context.Write(responseBody, responseBody.Length, "application/json", 200);
                Console.WriteLine(responseBody.AsString());
            }
        }

        internal class GraphQLPost
        {
            public  string                      query;
            public  string                      operationName;
            public  Dictionary<string,string>   variables;
        }
    }
}