// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.GraphQL;

namespace Friflo.Json.Fliox.Hub.GraphQL.Lab
{
    public static class TestAPI
    {
        internal static readonly List<GqlType> Types = new List<GqlType>
        {
            Gql.String(),
            Gql.Int(),
            //
            new GqlObject { name = "Query",
                fields = new List<GqlField> {
                    new GqlField { name = "articles",
                        args = new List<GqlInputValue> {
                            Gql.InputValue ("filter",   Gql.String()),
                            Gql.InputValue ("limit",    Gql.Int())
                        },
                        type = Gql.List(Gql.Scalar("TestType"), true, true)
                    },
                    new GqlField { name = "articlesById",
                        args = new List<GqlInputValue> {
                            Gql.InputValue ("ids",      Gql.List(Gql.String(), true, true))
                        },
                        type = Gql.List(Gql.Scalar("TestType"), true, false)
                    }
                }
            },
            new GqlObject { name = "TestType",
                fields = new List<GqlField> {
                    new GqlField { name = "field1", type = Gql.String() }
                }
            },
        };
        
        internal static JsonValue CreateTestSchema(Pool pool) {
            var types       = Types;
            var gqlSchema   = new GqlSchema {
                queryType   = new GqlType { name = "Query" },
                types       = types,
                directives  = new List<GqlDirective>()
            };
            return ModelUtils.CreateSchemaResponse(pool, gqlSchema);
        }
    }
}