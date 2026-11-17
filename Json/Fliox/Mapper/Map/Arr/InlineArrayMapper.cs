// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
#if NET8_0_OR_GREATER
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Arr
{
    /// <summary>
    /// Maps a struct annotated with <see cref="InlineArrayAttribute"/> (.NET 8) as a JSON array of its elements.<br/>
    /// Without it the struct is mapped as an object without members: it is written as <c>{}</c> and its elements are lost.
    /// </summary>
    internal sealed class InlineArrayMatcher : ITypeMatcher {
        public static readonly InlineArrayMatcher Instance = new InlineArrayMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (!type.IsValueType)
                return null;
            var attribute = type.GetCustomAttribute<InlineArrayAttribute>();
            if (attribute == null)
                return null;
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fields.Length != 1)
                return null;
            var elementType = fields[0].FieldType;
            object[] constructorParams = { config, type, elementType, attribute.Length };
            var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(InlineArrayMapper<,>), new[] { type, elementType }, constructorParams);
            return (TypeMapper) newInstance;
        }
    }
    
    /// <summary>
    /// An inline array of N elements is written as a JSON array of N elements. A read replaces the block: a shorter array leaves the
    /// remaining elements default, a longer array is an error because the surplus elements would be lost, and <c>null</c> reads as
    /// the default block.
    /// </summary>
    internal sealed class InlineArrayMapper<TBlock, TElm> : CollectionMapper<TBlock, TElm>
        where TBlock : struct
    {
        private readonly int length;
        
        public override string  DataTypeName()              => $"{typeof(TBlock).Name}[{length}]";
        public override bool    IsNull(ref TBlock value)    => false;
        public override int     Count(object array)         => length;

        public InlineArrayMapper(StoreConfig config, Type type, Type elementType, int length) :
            base(config, type, elementType, 1, typeof(string), null, false, true)
        {
            this.length = length;
        }
        
        private Span<TElm> Elements(ref TBlock block) => MemoryMarshal.CreateSpan(ref Unsafe.As<TBlock, TElm>(ref block), length);

        public override void Write(ref Writer writer, TBlock slot) {
            int startLevel = writer.IncLevel();
            var elements = Elements(ref slot);
            writer.WriteArrayBegin();
            for (int n = 0; n < elements.Length; n++) {
                writer.WriteDelimiter(n);
                
                var element = elements[n];
                if (elementType.IsNull(ref element)) {
                    writer.AppendNull();
                } else {
                    elementType.Write(ref writer, element);
                    writer.FlushFilledBuffer();
                }
            }
            writer.WriteArrayEnd();
            writer.DecLevel(startLevel);
        }

        public override TBlock Read(ref Reader reader, TBlock slot, out bool success) {
            if (reader.parser.Event == JsonEvent.ValueNull) {
                success = true;
                return default;
            }
            if (!reader.StartArray(this, out success))
                return default;
            
            slot = default;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ValueNull:
                        if (index >= length)
                            return reader.ErrorMsg<TBlock>($"JSON array has more elements than the inline array holds ({length}). type: ", typeof(TBlock).Name, out success);
                        TElm element = default;
                        element = reader.ReadElement(elementType, ref element, out success);
                        if (!success)
                            return default;
                        Elements(ref slot)[index] = element;
                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        success = true;
                        return slot;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return reader.ErrorMsg<TBlock>("unexpected state: ", ev, out success);
                }
            }
        }
        
        public override void Copy(TBlock src, ref TBlock dst) {
            dst = src;
            // elements which are reference types or contain them need a deep copy
            var srcElements = Elements(ref src);
            var dstElements = Elements(ref dst);
            for (int n = 0; n < srcElements.Length; n++) {
                elementType.Copy(srcElements[n], ref dstElements[n]);
            }
        }
    }
}
#endif
