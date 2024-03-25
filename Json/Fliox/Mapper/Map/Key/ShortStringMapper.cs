// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Mapper.Map.Key
{
    internal sealed class ShortStringMapper : KeyMapper<ShortString>
    {
        public override void WriteKey (ref Writer writer, in ShortString key) {
            writer.WriteShortString(key);
        }
        
        public override ShortString ReadKey (ref Reader reader, out bool success) {
            ref var parser = ref reader.parser;
            success = true;
            return new ShortString(parser.key, null);
        }
        
        public override JsonKey     ToJsonKey      (in ShortString key) {
            return new JsonKey(key);
        }
        
        public override ShortString ToKey          (in JsonKey key) {
            return new ShortString(key);
        }
    }
}