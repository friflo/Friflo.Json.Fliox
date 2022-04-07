// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Json.Fliox.Schema.GraphQL
{
    public static class Gql
    {
        public static GqlType List (string name, bool required, bool itemsRequired) {
            GqlType itemType = new GqlScalar{ name = name };
            if (itemsRequired) {
                itemType = new GqlNonNull { ofType = itemType };
            }
            GqlType listType = new GqlList { ofType = itemType };
            if (required) {
                return new GqlNonNull { ofType = listType };
            }
            return listType;
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
        
    }
}