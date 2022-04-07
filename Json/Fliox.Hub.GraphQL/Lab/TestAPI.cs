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
                    new GqlField { name = "articles_read",
                        args = new List<GqlInputValue> {
                            new GqlInputValue { name = "ids",
                                type = new GqlNonNull { 
                                    ofType = new GqlList {
                                        ofType = new GqlScalar{ name = "String" }
                                    }
                                }
                            }
                        },
                        type = new GqlList {
                            ofType = new GqlScalar{ name = "TestType" }
                        }
                    },
                    new GqlField { name = "articles_query",
                        args = new List<GqlInputValue> {
                            new GqlInputValue { name = "filter",
                                type = new GqlList {
                                    ofType = new GqlScalar{ name = "String" }
                                }
                            },
                            new GqlInputValue { name = "limit",
                                type = new GqlScalar { name = "Int"}
                            }
                        },
                        type = new GqlList {
                            ofType = new GqlScalar{ name = "TestType" }
                        }
                    }
                }
            },
            new GqlObject { name = "TestType",
                fields = new List<GqlField> {
                    new GqlField { name = "field1",
                        type = new GqlScalar{ name = "String" }
                    }
                }
            },
        };
    }
}