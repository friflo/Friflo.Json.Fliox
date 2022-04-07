// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.GraphQL;

namespace Friflo.Json.Fliox.Hub.GraphQL.Lab
{
    public static class TestAPI
    {
        internal static readonly List<GqlType> Types = new List<GqlType>
        {
            new GqlScalar { name = "String" },
            new GqlScalar { name = "Int"    },
            //
            new GqlObject { name = "Query",
                fields = new List<GqlField> {
                    new GqlField { name = "articles",
                        args = new List<GqlInputValue> {
                            Gql.InputValue ("filter",   new GqlScalar { name = "String" }),
                            Gql.InputValue ("limit",    new GqlScalar  { name = "Int" })
                        },
                        type = Gql.List("TestType", true, true)
                    },
                    new GqlField { name = "articlesById",
                        args = new List<GqlInputValue> {
                            Gql.InputValue ("ids",      Gql.List("String", true, true))
                        },
                        type = Gql.List("TestType", true, false)
                    }
                }
            },
            new GqlObject { name = "TestType",
                fields = new List<GqlField> {
                    new GqlField { name = "field1", type = new GqlScalar{ name = "String" } }
                }
            },
        };
    }
}