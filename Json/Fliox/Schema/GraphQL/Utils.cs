// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Json.Fliox.Schema.GraphQL
{
    public static class Gql
    {
        public static GqlType List (GqlType itemType, bool required, bool itemsRequired) {
            if (itemsRequired) {
                itemType = new GqlNonNull { ofType = itemType };
            }
            GqlType listType = new GqlList { ofType = itemType };
            if (required) {
                return new GqlNonNull { ofType = listType };
            }
            return listType;
        }
        
        public static GqlScalar Scalar (string name) {
            return new GqlScalar { name = name };
        }
        
        public static GqlScalar ScalarInput (string name) {
            return new GqlScalar { name = name + "Input",  };
        }
        
        public static GqlType Type (GqlType type, bool required) {
            if (required)
                return new GqlNonNull { ofType = type };
            return type;
        }
        
        public static GqlInputValue InputValue (string name, GqlType type, bool required = false) {
            if (required) {
                type = new GqlNonNull { ofType = type };
            }
            return new GqlInputValue { name = name, type = type };
        }
        
        public static GqlType String    () =>   new GqlScalar { name = "String"   };
        public static GqlType Int       () =>   new GqlScalar { name = "Int"      };
        public static GqlType Float     () =>   new GqlScalar { name = "Float"    };
        public static GqlType Boolean   () =>   new GqlScalar { name = "Boolean"  };
        public static GqlType Any       () =>   new GqlScalar { name = "Any"      };
        
    }
}