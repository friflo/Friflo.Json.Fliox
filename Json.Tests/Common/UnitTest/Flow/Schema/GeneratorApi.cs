// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.Utils;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public class GeneratorApi
    {
        // ReSharper disable once UnusedParameter.Local
        private static void EnsureSymbol(string _) {}
        
        // ReSharper disable once UnusedMember.Local
        private static void EnsureApiAccess() {
            EnsureSymbol(nameof(Generator.files));
            EnsureSymbol(nameof(Generator.packages));
            EnsureSymbol(nameof(Generator.types));
            EnsureSymbol(nameof(Generator.fileExt));
            
            EnsureSymbol(nameof(EmitType.type));

            EnsureSymbol(nameof(Package.imports));
            EnsureSymbol(nameof(Package.header));
            EnsureSymbol(nameof(Package.footer));
            EnsureSymbol(nameof(Package.emitTypes));
            
            EnsureSymbol(nameof(TypeContext.generator));
            EnsureSymbol(nameof(TypeContext.imports));
            EnsureSymbol(nameof(TypeContext.owner));
        }
    }
}