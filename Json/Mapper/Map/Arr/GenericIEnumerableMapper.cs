// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public class GenericIEnumerableMatcher : ITypeMatcher {
        public static readonly GenericIEnumerableMatcher Instance = new GenericIEnumerableMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(IEnumerable<>) );
            if (args != null) {
                Type elementType = args[0];
                ConstructorInfo constructor = null;
                // ReSharper disable once ExpressionIsAlwaysNull
                object[] constructorParams = {config, type, elementType, constructor};
                // new GenericIEnumerableMapper<IEnumerable<TElm>,TElm>  (config, type, elementType, constructor);
                var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(GenericIEnumerableMapper<,>), new[] {type, elementType}, constructorParams);
                return (TypeMapper) newInstance;
            }
            return null;
        }        
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class GenericIEnumerableMapper<TCol, TElm> : CollectionMapper<TCol, TElm> where TCol : IEnumerable<TElm>
    {
        public override string DataTypeName() { return "IEnumerable"; }
        
        public GenericIEnumerableMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor) {
        }

        public override void Write(JsonWriter writer, TCol slot) {
            int startLevel = WriteUtils.IncLevel(writer);
            var enumerable = slot;
            writer.bytes.AppendChar('[');

            int n = 0;
            foreach (var currentItem in enumerable) {
                var item = currentItem; // capture to use by ref
                if (n++ > 0)
                    writer.bytes.AppendChar(',');
                
                if (!elementType.IsNull(ref item)) {
                    ObjectUtils.Write(writer, elementType, ref item);
                } else
                    WriteUtils.AppendNull(writer);
            }
            writer.bytes.AppendChar(']');
            WriteUtils.DecLevel(writer, startLevel);
        }

        public override TCol Read(ref Reader reader, TCol slot, out bool success) {
            throw new InvalidOperationException("IEnumerable<> cannot be used for Read(). type: " + type);
        }
    }
}