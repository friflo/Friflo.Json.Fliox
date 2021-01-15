// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Managed.Types
{
    public interface ITypeResolver
    {
        StubType CreateStubType(Type type);
    }
}