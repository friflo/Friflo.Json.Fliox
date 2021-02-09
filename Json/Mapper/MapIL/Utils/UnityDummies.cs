// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_5_3_OR_NEWER

using System;
using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.Mapper.MapIL.Val
{
    class DoubleFieldMapper : DoubleMapper  { public DoubleFieldMapper  (StoreConfig config, Type type) : base(config, type) { } }
    class FloatFieldMapper  : FloatMapper   { public FloatFieldMapper   (StoreConfig config, Type type) : base(config, type) { } }
    class LongFieldMapper   : LongMapper    { public LongFieldMapper    (StoreConfig config, Type type) : base(config, type) { } }
    class IntFieldMapper    : IntMapper     { public IntFieldMapper     (StoreConfig config, Type type) : base(config, type) { } }
    class ShortFieldMapper  : ShortMapper   { public ShortFieldMapper   (StoreConfig config, Type type) : base(config, type) { } }
    class ByteFieldMapper   : ByteMapper    { public ByteFieldMapper    (StoreConfig config, Type type) : base(config, type) { } }
    class BoolFieldMapper   : BoolMapper    { public BoolFieldMapper    (StoreConfig config, Type type) : base(config, type) { } }
    //
    class NullableDoubleFieldMapper : NullableDoubleMapper  { public NullableDoubleFieldMapper  (StoreConfig config, Type type) : base(config, type) { } }
    class NullableFloatFieldMapper  : NullableFloatMapper   { public NullableFloatFieldMapper   (StoreConfig config, Type type) : base(config, type) { } }
    class NullableLongFieldMapper   : NullableLongMapper    { public NullableLongFieldMapper    (StoreConfig config, Type type) : base(config, type) { } }
    class NullableIntFieldMapper    : NullableIntMapper     { public NullableIntFieldMapper     (StoreConfig config, Type type) : base(config, type) { } }
    class NullableShortFieldMapper  : NullableShortMapper   { public NullableShortFieldMapper   (StoreConfig config, Type type) : base(config, type) { } }
    class NullableByteFieldMapper   : NullableByteMapper    { public NullableByteFieldMapper    (StoreConfig config, Type type) : base(config, type) { } }
    class NullableBoolFieldMapper   : NullableBoolMapper    { public NullableBoolFieldMapper    (StoreConfig config, Type type) : base(config, type) { } }
}

namespace Friflo.Json.Mapper.MapIL.Obj
{
    public          class ClassMirror { }
    public abstract class ClassLayout { }
}

#endif

