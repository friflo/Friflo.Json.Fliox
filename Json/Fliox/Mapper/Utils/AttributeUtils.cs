// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Reflection;

namespace Friflo.Json.Fliox.Mapper.Utils
{
    public static class AttributeUtils {
                
        public static void Property(IEnumerable<CustomAttributeData> attributes, out string name) {
            name        = null;
            foreach (var attr in attributes) {
                if (attr.AttributeType != typeof(SerializeAttribute))
                    continue;
                var arguments   = attr.ConstructorArguments;
                name = arguments.Count < 1 ? null : (string)arguments[0].Value;
            }
        }
        
        public static string CommandName(IEnumerable<CustomAttributeData> attributes) {
            foreach (var attr in attributes) {
                if (attr.AttributeType != typeof(DatabaseCommandAttribute))
                    continue;
                var arguments   = attr.ConstructorArguments;
                return arguments.Count < 1 ? null : (string)arguments[0].Value;
            }
            return null;
        }
    }
}