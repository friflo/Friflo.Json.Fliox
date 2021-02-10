// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public class GenericIReadOnlyCollectionMatcher : ITypeMatcher {
        public static readonly GenericIReadOnlyCollectionMatcher Instance = new GenericIReadOnlyCollectionMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(IReadOnlyCollection<>) );
            if (args != null) {
                Type elementType = args[0];
                ConstructorInfo constructor = ReflectUtils.GetCopyConstructor(type);
                if (constructor == null) {
                    throw new NotSupportedException("no copy constructor for type: " + type);
                }
                object[] constructorParams = {config, type, elementType, constructor};
                // new GenericIReadOnlyCollectionMapper<IReadOnlyCollection<TElm>,TElm>  (config, type, elementType, constructor);
                var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(GenericIReadOnlyCollectionMapper<,>), new[] {type, elementType}, constructorParams);
                return (TypeMapper) newInstance;
            }
            return null;
        }        
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class GenericIReadOnlyCollectionMapper<TCol, TElm> : CollectionMapper<TCol, TElm> where TCol : IReadOnlyCollection<TElm>
    {
        public override string DataTypeName() { return "IReadOnlyCollection"; }
        
        public GenericIReadOnlyCollectionMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor) {
        }

        public override void Write(JsonWriter writer, TCol slot) {
            int startLevel = WriteUtils.IncLevel(writer);
            var col = slot;
            writer.bytes.AppendChar('[');
            
            int n = 0;
            IEnumerator<TElm> iter = col.GetEnumerator();
            if (type.GetGenericTypeDefinition() == typeof(ConcurrentStack<>))
                iter = col.Reverse().GetEnumerator();
            while (iter.MoveNext()) {
                var item = iter.Current; // capture to use by ref
                if (n++ > 0)
                    writer.bytes.AppendChar(',');
                
                if (!elementType.IsNull(ref item)) {
                    ObjectUtils.Write(writer, elementType, ref item);
                } else
                    WriteUtils.AppendNull(writer);
            }
            iter.Dispose();
            writer.bytes.AppendChar(']');
            WriteUtils.DecLevel(writer, startLevel);
        }
        

        public override TCol Read(JsonReader reader, TCol slot, out bool success) {
            if (!ArrayUtils.StartArray(reader, this, out success))
                return default;
            
            List<TElm> list = new List<TElm>();
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        TElm elemVar;
                        elemVar = default;
                        elemVar = ObjectUtils.Read(reader, elementType, ref elemVar, out success);
                        if (!success)
                            return default;
                        list.Add(elemVar);
                        break;
                    case JsonEvent.ValueNull:
                        if (!ArrayUtils.IsNullable(reader, this, elementType, out success))
                            return default;
                        list.Add(default);
                        break;
                    case JsonEvent.ArrayEnd:
                        success = true;
                        TCol col = (TCol)ReflectUtils.CreateInstanceCopy(constructor, list);
                        return col;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        ReadUtils.ErrorMsg<List<TElm>>(reader, "unexpected state: ", ev, out success);
                        return default;
                }
            }
        }
    }
}
