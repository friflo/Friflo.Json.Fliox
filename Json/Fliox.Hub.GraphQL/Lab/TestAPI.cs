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
            new GqlObject { name = "Query",
                fields = new List<GqlField> {
                    new GqlField { name = "test",
                        args = new List<GqlInputValue> {
                            new GqlInputValue { name = "parameter",
                                type = new GqlScalar{ name = "String" }
                            }
                        },
                        type = new GqlList {
                            ofType = new GqlScalar{ name = "String" }
                        }
                    }
                }
            },
            new GqlScalar { name = "String"  },
        };
    }
}