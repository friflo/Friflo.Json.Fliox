// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using static System.Diagnostics.DebuggerBrowsableState;
using static System.BitConverter;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;
namespace Friflo.Json.Fliox.Mapper.Map
{
    /// <summary>
    /// <see cref="Var"/> is used to prevent boxing of primitives types when: <br/>
    /// - serializing primitive JSON values like: 123, "abc", true or false <br/>
    /// - accessing primitive class fields or properties like: bool, byte, short, int, long, char, float and double. <br/> 
    /// </summary>
    public readonly partial struct Var
    {
        public override bool    Equals(object obj)  => throw new InvalidOperationException("not implemented intentionally");
        public override int     GetHashCode()       => throw new InvalidOperationException("not implemented intentionally");

        // --- Note! All fields must be private to ensure using only the type checked properties 
        private   readonly      VarType type;
        private   readonly      object  obj;
        private   readonly      long    lng;            // holds also the value of floating point value Dbl
                        
        private                 double  Dbl             => Int64BitsToDouble(lng);
                        
        internal                object  TryGetObject()  =>  type.TryGetObject(this);
        internal                object  ToObject()      =>  type.ToObject(this);
        
        internal  new           Type    GetType()       =>  type.GetType(this);
        
                        public  bool    IsNull          =>  type.IsNull(this);
        [Browse(Never)] public  bool    NotNull         => !type.IsNull(this);
        // --- reference
        [Browse(Never)] public  object  Object      { get { AssertType(TypeObject.Instance); return obj;           } } 
        [Browse(Never)] public  string  String      { get { AssertType(TypeString.Instance); return (string)obj;   } }
        
        // --- primitives
        [Browse(Never)] public  bool    Bool        { get { AssertType(TypeBool.Instance);   return lng != 0;        } }
        [Browse(Never)] public  char    Char        { get { AssertType(TypeChar.Instance);   return (char) lng;      } }

        [Browse(Never)] public  byte    Int8        { get { AssertType(TypeInt8.Instance);   return (byte) lng;      } }
        [Browse(Never)] public  short   Int16       { get { AssertType(TypeInt16.Instance);  return (short)lng;      } }
        [Browse(Never)] public  int     Int32       { get { AssertType(TypeInt32.Instance);  return (int)  lng;      } }
        [Browse(Never)] public  long    Int64       { get { AssertType(TypeInt64.Instance);  return        lng;      } }
        
        [Browse(Never)] public  float   Flt32       { get { AssertType(TypeFlt.Instance);    return (float)Dbl;      } }
        [Browse(Never)] public  double  Flt64       { get { AssertType(TypeDbl.Instance);    return        Dbl;      } }
        
        // --- nullable
        [Browse(Never)] public  bool?   BoolNull    { get { AssertType(TypeNullableBool.Instance);  return obj != null ? lng != 0 : (bool?)null; } }
        [Browse(Never)] public  char?   CharNull    { get { AssertType(TypeNullableChar.Instance);  return obj != null ? (char?)  lng : null; } }

        [Browse(Never)] public  byte?   Int8Null    { get { AssertType(TypeNullableInt8.Instance);  return obj != null ? (byte?)  lng : null; } }
        [Browse(Never)] public  short?  Int16Null   { get { AssertType(TypeNullableInt16.Instance); return obj != null ? (short?) lng : null; } }
        [Browse(Never)] public  int?    Int32Null   { get { AssertType(TypeNullableInt32.Instance); return obj != null ? (int?)   lng : null; } }
        [Browse(Never)] public  long?   Int64Null   { get { AssertType(TypeNullableInt64.Instance); return obj != null ? (long?)  lng : null; } }
        
        [Browse(Never)] public  float?  Flt32Null   { get { AssertType(TypeNullableFlt.Instance);   return obj != null ? (float?) Dbl : null; } }
        [Browse(Never)] public  double? Flt64Null   { get { AssertType(TypeNullableDbl.Instance);   return obj != null ? (double?)Dbl : null; } }

        public              string  AsString() =>  type.AsString(this);
        public  override    string  ToString() =>  $"{{{type}}} {type.AsString(this)}";

        public              bool    IsEqual(in Var other) {
            AssertSameType(this, other);
            return type.AreEqual(this, other);
        }
        
        public static bool operator == (in Var val1, in Var val2) {
            AssertSameType(val1, val2);
            return val1.type.AreEqual(val1, val2);
        }

        public static bool operator != (in Var val1, in Var val2) {
            AssertSameType(val1, val2);
            return !val1.type.AreEqual(val1, val2);
        }

        [Conditional("DEBUG")]
        private static void AssertSameType(in Var val1, in Var val2) {
            if (val1.type == val2.type)
                return;
            throw new InvalidOperationException($"incompatible type - val1: {val1.type.Name}, val2: {val2.type.Name}");
        }
        
        [Conditional("DEBUG")]
        private void AssertType(VarType expect) {
            if (type == expect)
                return;
            throw new InvalidOperationException($"Expect type: {expect}, was: {type}");
        }
        
        // --- object ---
        public Var (object value)  { type = TypeObject.Instance; obj = value; lng = 0; }
        public Var (string value)  { type = TypeString.Instance; obj = value; lng = 0; }
        
        // --- primitives
        public Var (char    value) { type = TypeChar.Instance;   obj = HasValue; lng = value; }
        
        public Var (byte    value) { type = TypeInt8.Instance;   obj = HasValue; lng = value; }
        public Var (short   value) { type = TypeInt16.Instance;  obj = HasValue; lng = value; }
        public Var (int     value) { type = TypeInt32.Instance;  obj = HasValue; lng = value; }
        public Var (long    value) { type = TypeInt64.Instance;  obj = HasValue; lng = value; }

        public Var (float   value) { type = TypeFlt.Instance;    obj = HasValue; lng = DoubleToInt64Bits (value);  }
        public Var (double  value) { type = TypeDbl.Instance;    obj = HasValue; lng = DoubleToInt64Bits (value); }

        // --- nullable primitives
        public Var (char? value)   { type = TypeNullableChar.Instance;  obj = value.HasValue ? HasValue : null; lng = value ?? 0; }
        
        public Var (byte?   value) { type = TypeNullableInt8.Instance;  obj = value.HasValue ? HasValue : null; lng = value ?? 0; }
        public Var (short?  value) { type = TypeNullableInt16.Instance; obj = value.HasValue ? HasValue : null; lng = value ?? 0; }
        public Var (int?    value) { type = TypeNullableInt32.Instance; obj = value.HasValue ? HasValue : null; lng = value ?? 0; }
        public Var (long?   value) { type = TypeNullableInt64.Instance; obj = value.HasValue ? HasValue : null; lng = value ?? 0; }

        public Var (float?  value) { type = TypeNullableFlt.Instance;   obj = value.HasValue ? HasValue : null; lng = value.HasValue ? DoubleToInt64Bits (value.Value) : 0; }
        public Var (double? value) { type = TypeNullableDbl.Instance;   obj = value.HasValue ? HasValue : null; lng = value.HasValue ? DoubleToInt64Bits (value.Value) : 0; }
        

        // --- bool ---
        public Var (bool value) {
            type    = TypeBool.Instance;
            obj     = HasValue;
            lng     = value ? 1 : 0;
        }

        public Var (bool? value) {
            type    = TypeNullableBool.Instance;
            obj     = value.HasValue ? HasValue : null;
            lng     = value.HasValue ? value.Value ? 1 : 0 : 0;
        }
        
        private static readonly object HasValue = "HasValue";
    }
}
