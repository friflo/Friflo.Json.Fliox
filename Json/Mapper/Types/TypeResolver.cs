// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.Types
{
    public interface ITypeResolver
    {
        StubType CreateStubType(Type type);
    }
    
    public class TypeResolver : ITypeResolver
    {
        public readonly IJsonMapper[] resolvers;

        public TypeResolver(IJsonMapper[] resolvers) {
            this.resolvers = resolvers;
        }

        public StubType CreateStubType(Type type) {
            for (int n = 0; n < resolvers.Length; n++) {
                StubType stubType = resolvers[n].CreateStubType(type);
                if (stubType != null)
                    return stubType;
            }
            return null;
        }
    }

}