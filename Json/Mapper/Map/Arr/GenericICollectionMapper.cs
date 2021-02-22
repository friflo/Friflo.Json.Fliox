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
    public class GenericICollectionMatcher : ITypeMatcher {
        public static readonly GenericICollectionMatcher Instance = new GenericICollectionMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(ICollection<>) );
            if (args == null)
                return null;
            Type elementType = args[0];
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            if (constructor == null) {
                if (type.GetGenericTypeDefinition() == typeof(ICollection<>))
                    constructor = ReflectUtils.GetDefaultConstructor(typeof(List<>).MakeGenericType(elementType));
                else
                    throw new NotSupportedException("not default constructor for type: " + type);
            }
            object[] constructorParams = {config, type, elementType, constructor};
            // new GenericICollectionMapper<ICollection<TElm>,TElm>  (config, type, elementType, constructor);
            var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(GenericICollectionMapper<,>), new[] {type, elementType}, constructorParams);
            return (TypeMapper) newInstance;
        }        
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class GenericICollectionMapper<TCol, TElm> : CollectionMapper<TCol, TElm> where TCol : ICollection<TElm>
    {
        public override string DataTypeName() { return "ICollection"; }
        
        public GenericICollectionMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor) {
        }

        public override void Write(ref Writer writer, TCol slot) {
            int startLevel = writer.IncLevel();
            var list = slot;
            writer.bytes.AppendChar('[');
            
            int n = 0;
            foreach (var currentItem in list) {
                var item = currentItem; // capture to use by ref
                writer.WriteDelimiter(n++);
                
                if (!elementType.IsNull(ref item)) {
                    writer.WriteElement(elementType, ref item);
                    writer.FlushFilledBuffer();
                } else
                    writer.AppendNull();
            }
            writer.WriteArrayEnd();
            writer.DecLevel(startLevel);
        }
        

        public override TCol Read(ref Reader reader, TCol slot, out bool success) {
            if (!reader.StartArray(this, out success))
                return default;
            
            var list = slot;
            if (list == null)
                list = (TCol) CreateInstance();
            else
                list.Clear();

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
                        elemVar = reader.ReadElement(elementType, ref elemVar, out success);
                        if (!success)
                            return default;
                        list.Add(elemVar);
                        break;
                    case JsonEvent.ValueNull:
                        if (!reader.IsElementNullable(this, elementType, out success))
                            return default;
                        list.Add(default);
                        break;
                    case JsonEvent.ArrayEnd:
                        success = true;
                        return list;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        ReadUtils.ErrorMsg<List<TElm>>(ref reader, "unexpected state: ", ev, out success);
                        return default;
                }
            }
        }
    }
}
