// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Obj.Reflect;
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
            EnsureSymbol(nameof(Generator.typeMappers));
            EnsureSymbol(nameof(Generator.fileExt));
            
            EnsureSymbol(nameof(EmitType.type));

            EnsureSymbol(nameof(Package.imports));
            EnsureSymbol(nameof(Package.header));
            EnsureSymbol(nameof(Package.footer));
            EnsureSymbol(nameof(Package.emitTypes));
            
            EnsureSymbol(nameof(TypeContext.generator));
            EnsureSymbol(nameof(TypeContext.imports));
            EnsureSymbol(nameof(TypeContext.owner));
            
            EnsureSymbol(nameof(PolyType.type));
            EnsureSymbol(nameof(PolyType.name));
            
            EnsureSymbol(nameof(InstanceFactory.discriminator));
            EnsureSymbol(nameof(InstanceFactory.polyTypes));
            
            EnsureSymbol(nameof(PropField.name));
            EnsureSymbol(nameof(PropField.jsonName));
            EnsureSymbol(nameof(PropField.fieldType));
            
            EnsureSymbol(nameof(TypeMapper.InstanceFactory));
            EnsureSymbol(nameof(TypeMapper.Discriminant));
            EnsureSymbol(nameof(TypeMapper.type));
            EnsureSymbol(nameof(TypeMapper.isNullable));
            EnsureSymbol(nameof(TypeMapper.nullableUnderlyingType));
            EnsureSymbol(nameof(TypeMapper.isNullable));
            EnsureSymbol(nameof(TypeMapper.IsComplex));
            EnsureSymbol(nameof(TypeMapper.IsArray));
            EnsureSymbol(nameof(TypeMapper.propFields));
            EnsureSymbol(nameof(TypeMapper.GetElementMapper));
            EnsureSymbol(nameof(TypeMapper.GetEnumValues));
            EnsureSymbol(nameof(TypeMapper.GetUnderlyingMapper));
            EnsureSymbol(nameof(TypeMapper.GetTypeSemantic));
        }
    }
}