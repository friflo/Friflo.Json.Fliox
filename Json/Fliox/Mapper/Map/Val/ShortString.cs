// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    internal sealed class ShortStringMatcher : ITypeMatcher {
        public static readonly ShortStringMatcher Instance = new ShortStringMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(ShortString))
                return null;
            return new ShortStringMapper (config, type);
        }
    }
    
    internal sealed class ShortStringMapper : TypeMapper<ShortString>
    {
        public override string  DataTypeName()                  => "ShortString";
        public override bool    IsNull(ref ShortString value)   => value.IsNull();

        public ShortStringMapper(StoreConfig config, Type type) : base (config, type, true, false) { }
        
        public override bool IsNullVar(in Var value) {
            var key = (ShortString)value.TryGetObject();
            return key.IsNull();
        }

        public override void Write(ref Writer writer, ShortString value) {
            if (!value.IsNull()) {
                writer.WriteShortString(value);
            } else {
                writer.AppendNull();
            }
        }

        public override ShortString Read(ref Reader reader, ShortString value, out bool success) {
            ref var parser = ref reader.parser;
            var ev = parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    success = true;
                    return new ShortString();
                case JsonEvent.ValueString:
                    success = true;
                    return new ShortString(parser.value, value.str);
                default:
                    return reader.ErrorMsg<ShortString>("Expect string as ShortString. ", ev, out success);
            }
        }
    }
}