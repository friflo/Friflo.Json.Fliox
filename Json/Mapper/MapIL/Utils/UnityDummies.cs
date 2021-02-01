// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_5_3_OR_NEWER

using System;
using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.Mapper.MapIL.Val
{
    class DoubleFieldMapper : DoubleMapper  { public DoubleFieldMapper  (Type type) : base(type) { } }
    class FloatFieldMapper  : FloatMapper   { public FloatFieldMapper   (Type type) : base(type) { } }
    class LongFieldMapper   : LongMapper    { public LongFieldMapper    (Type type) : base(type) { } }
    class IntFieldMapper    : IntMapper     { public IntFieldMapper     (Type type) : base(type) { } }
    class ShortFieldMapper  : ShortMapper   { public ShortFieldMapper   (Type type) : base(type) { } }
    class ByteFieldMapper   : ByteMapper    { public ByteFieldMapper    (Type type) : base(type) { } }
    class BoolFieldMapper   : BoolMapper    { public BoolFieldMapper    (Type type) : base(type) { } }
    //
    class NullableDoubleFieldMapper : NullableDoubleMapper  { public NullableDoubleFieldMapper  (Type type) : base(type) { } }
    class NullableFloatFieldMapper  : NullableFloatMapper   { public NullableFloatFieldMapper   (Type type) : base(type) { } }
    class NullableLongFieldMapper   : NullableLongMapper    { public NullableLongFieldMapper    (Type type) : base(type) { } }
    class NullableIntFieldMapper    : NullableIntMapper     { public NullableIntFieldMapper     (Type type) : base(type) { } }
    class NullableShortFieldMapper  : NullableShortMapper   { public NullableShortFieldMapper   (Type type) : base(type) { } }
    class NullableByteFieldMapper   : NullableByteMapper    { public NullableByteFieldMapper    (Type type) : base(type) { } }
    class NullableBoolFieldMapper   : NullableBoolMapper    { public NullableBoolFieldMapper    (Type type) : base(type) { } }
}

namespace Friflo.Json.Mapper.MapIL.Obj
{
    public          class ClassMirror { }
    public abstract class ClassLayout { }
}

#endif

