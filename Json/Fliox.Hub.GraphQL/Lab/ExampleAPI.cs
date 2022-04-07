// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.GraphQL;

namespace Friflo.Json.Fliox.Hub.GraphQL.Lab
{
    public static class ExampleAPI
    {
        internal static readonly List<GqlType> Types = new List<GqlType>
        {
            new GqlScalar { name = "Boolean" },
            new GqlScalar { name = "String"  },
            new GqlScalar { name = "ID"  },
            new GqlObject { name = "Query",
                fields = new List<GqlField> {
                    new GqlField { name = "_entities",
                        args = new List<GqlInputValue> {
                            new GqlInputValue { name = "representations",
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
            new GqlUnion { name = "_Entity",
                possibleTypes = new List<GqlType> ()
            },
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
                        type = new GqlObject { name = "__Type" }
                    },
                    new GqlField { name = "subscriptionType",
                        type =  new GqlObject { name = "__Type" }
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
                        type = new GqlNonNull {
                            ofType = new GqlEnum { name = "__TypeKind" }
                        }
                    },
                    new GqlField { name = "name",
                        type = new GqlScalar { name = "String" }
                    },
                    new GqlField { name = "description",
                        type = new GqlScalar { name = "String" }
                    },
                    new GqlField { name = "specifiedByUrl",
                        type = new GqlScalar { name = "String" }
                    },
                    new GqlField { name = "fields",
                        args = new List<GqlInputValue> {
                            new GqlInputValue { name = "includeDeprecated",
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
                        type = new GqlList {
                            ofType = new GqlNonNull {
                                ofType = new GqlObject { name = "__Type"}
                            }
                        }
                    },
                    new GqlField { name = "possibleTypes",
                        type = new GqlList {
                            ofType = new GqlNonNull {
                                ofType = new GqlObject { name = "__Type"}
                            }
                        }
                    },
                    new GqlField { name = "enumValues",
                        args = new List<GqlInputValue> {
                            new GqlInputValue { name = "includeDeprecated",
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
                        args = new List<GqlInputValue> {
                            new GqlInputValue { name = "includeDeprecated",
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
                        type = new GqlObject { name = "__Type" }
                    },
                }
            },
            new GqlEnum   { name = "__TypeKind",
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
                        type = new GqlNonNull {
                            ofType = new GqlScalar { name = "String" }
                        }
                    },
                    new GqlField { name = "description",
                        type = new GqlScalar { name = "String" }
                    },
                    new GqlField { name = "args",
                        args = new List<GqlInputValue> {
                            new GqlInputValue { name = "includeDeprecated",
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
                        type = new GqlNonNull {
                            ofType = new GqlObject { name = "__Type" }
                        }
                    },
                    new GqlField { name = "isDeprecated",
                        type = new GqlNonNull {
                            ofType = new GqlScalar { name = "Boolean" }
                        }
                    },
                    new GqlField { name = "deprecationReason",
                        type = new GqlScalar { name = "String" }
                    },
                }
            },
            new GqlObject { name = "__InputValue",
                fields = new List<GqlField> {
                    new GqlField { name = "name",
                        type = new GqlNonNull {
                            ofType = new GqlScalar { name = "String" }
                        }
                    },
                    new GqlField { name = "description",
                        type = new GqlScalar { name = "String" }
                    },
                    new GqlField { name = "type",
                        type = new GqlNonNull {
                            ofType = new GqlObject { name = "__Type" }
                        }
                    },
                    new GqlField { name = "defaultValue",
                        type = new GqlScalar { name = "String" }
                    },
                    new GqlField { name = "isDeprecated",
                        type = new GqlNonNull {
                            ofType = new GqlScalar { name = "Boolean" }
                        }
                    },
                    new GqlField { name = "deprecationReason",
                        type = new GqlScalar { name = "String" }
                    },
                }
            },
            new GqlObject { name = "__EnumValue",
                fields = new List<GqlField> {
                    new GqlField { name = "name",
                        type = new GqlNonNull {
                            ofType = new GqlScalar { name = "String" }
                        }
                    },
                    new GqlField { name = "description",
                        type = new GqlScalar { name = "String" }
                    },
                    new GqlField { name = "isDeprecated",
                        type = new GqlNonNull {
                            ofType = new GqlScalar { name = "Boolean" }
                        }
                    },
                    new GqlField { name = "deprecationReason",
                        type = new GqlScalar { name = "String" }
                    },
                }
            },
            new GqlObject { name = "__Directive",
                fields = new List<GqlField> {
                    new GqlField { name = "name",
                        type = new GqlNonNull {
                            ofType = new GqlScalar { name = "String" }
                        }
                    },
                    new GqlField { name = "description",
                        type = new GqlScalar { name = "String" }
                    },
                    new GqlField { name = "isRepeatable",
                        type = new GqlNonNull {
                            ofType = new GqlScalar { name = "Boolean" }
                        }
                    },
                    new GqlField { name = "locations",
                        type = new GqlNonNull {
                            ofType = new GqlList {
                                ofType = new GqlNonNull {
                                    ofType = new GqlEnum { name = "__DirectiveLocation" }
                                }
                            }
                        }
                    },
                    new GqlField { name = "args",
                        args = new List<GqlInputValue> {
                            new GqlInputValue { name = "includeDeprecated",
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
                }
            },
            new GqlEnum { name = "__DirectiveLocation",
                enumValues = new List<GqlEnumValue> {
                    new GqlEnumValue { name = "QUERY"                  },
                    new GqlEnumValue { name = "MUTATION"               },
                    new GqlEnumValue { name = "SUBSCRIPTION"           },
                    new GqlEnumValue { name = "FIELD"                  },
                    new GqlEnumValue { name = "FRAGMENT_DEFINITION"    },
                    new GqlEnumValue { name = "FRAGMENT_SPREAD"        },
                    new GqlEnumValue { name = "INLINE_FRAGMENT"        },
                    new GqlEnumValue { name = "VARIABLE_DEFINITION"    },
                    new GqlEnumValue { name = "SCHEMA"                 },
                    new GqlEnumValue { name = "SCALAR"                 },
                    new GqlEnumValue { name = "OBJECT"                 },
                    new GqlEnumValue { name = "FIELD_DEFINITION"       },
                    new GqlEnumValue { name = "ARGUMENT_DEFINITION"    },
                    new GqlEnumValue { name = "INTERFACE"              },
                    new GqlEnumValue { name = "UNION"                  },
                    new GqlEnumValue { name = "ENUM"                   },
                    new GqlEnumValue { name = "ENUM_VALUE"             },
                    new GqlEnumValue { name = "INPUT_OBJECT"           },
                    new GqlEnumValue { name = "INPUT_FIELD_DEFINITION" },
                }
            }
            
        };
    }
}