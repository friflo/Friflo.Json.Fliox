using System;
using Friflo.Json.Mapper.Map.Val;

#if UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{
    class DoubleFieldMapper : DoubleMapper  { public DoubleFieldMapper  (Type type) : base(type) { } }
    class FloatFieldMapper  : FloatMapper   { public FloatFieldMapper   (Type type) : base(type) { } }
    class LongFieldMapper   : LongMapper    { public LongFieldMapper    (Type type) : base(type) { } }
    class IntFieldMapper    : IntMapper     { public IntFieldMapper     (Type type) : base(type) { } }
    class ShortFieldMapper  : ShortMapper   { public ShortFieldMapper   (Type type) : base(type) { } }
    class ByteFieldMapper   : ByteMapper    { public ByteFieldMapper    (Type type) : base(type) { } }
    class BoolFieldMapper   : BoolMapper    { public BoolFieldMapper    (Type type) : base(type) { } }

    public          class ClassMirror { }
    public abstract class ClassLayout { }
}

#endif

