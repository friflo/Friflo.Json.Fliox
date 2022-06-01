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
                if (attr.AttributeType != typeof(SerializeMemberAttribute))
                    continue;
                if (attr.NamedArguments == null)
                    continue;
                foreach (var args in attr.NamedArguments) {
                    switch (args.MemberName) {
                        case nameof(SerializeMemberAttribute.Name):
                            if (args.TypedValue.Value != null)
                                name = args.TypedValue.Value as string;
                            break;
                    }
                }
            }
        }
        
        public static string CommandName(IEnumerable<CustomAttributeData> attributes) {
            foreach (var attr in attributes) {
                if (attr.AttributeType != typeof(DatabaseCommandAttribute))
                    continue;
                if (attr.NamedArguments == null)
                    continue;
                foreach (var args in attr.NamedArguments) {
                    switch (args.MemberName) {
                        case nameof(SerializeMemberAttribute.Name):
                            if (args.TypedValue.Value != null)
                                return args.TypedValue.Value as string;
                            break;
                    }
                }
            }
            return null;
        }
    }
}