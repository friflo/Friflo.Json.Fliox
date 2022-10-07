// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Friflo.Json.Fliox.Mapper.Map.Object.Reflect
{
    public readonly struct Var
    {
        public override bool    Equals(object obj)  => throw new InvalidOperationException("not implemented intentionally");
        public override int     GetHashCode()       => throw new InvalidOperationException("not implemented intentionally");

        private   readonly  VarType type;
        internal  readonly  object  obj;
        internal  readonly  long    lng;
        internal  readonly  double  dbl;   // can merge with lng using BitConverter.DoubleToInt64Bits()
        
        public  bool    IsNull                  =>  type.IsNull(this);
        public  bool    NotNull                 => !type.IsNull(this);
        // --- reference
        public  object  Object      { get { AssertType(VarTypeObject.Instance); return obj;           } } 
        public  string  String      { get { AssertType(VarTypeString.Instance); return (string)obj;   } }
        
        // --- primitives
        public  bool    Bool        { get { AssertType(VarTypeBool.Instance); return lng != 0;        } }
        public  char    Char        { get { AssertType(VarTypeChar.Instance); return (char) lng;      } }

        public  byte    Int8        { get { AssertType(VarTypeLong.Instance); return (byte) lng;      } }
        public  short   Int16       { get { AssertType(VarTypeLong.Instance); return (short)lng;      } }
        public  int     Int32       { get { AssertType(VarTypeLong.Instance); return (int)  lng;      } }
        public  long    Int64       { get { AssertType(VarTypeLong.Instance); return        lng;      } }
        
        public  float   Flt32       { get { AssertType(VarTypeDbl.Instance);  return (float)dbl;      } }
        public  double  Flt64       { get { AssertType(VarTypeDbl.Instance);  return        dbl;      } }
        
        // --- nullable
        public  bool?   BoolNull    { get { AssertType(VarTypeNullableBool.Instance); return obj != null ? lng != 0     : default; } }
        public  char?   CharNull    { get { AssertType(VarTypeNullableChar.Instance); return obj != null ? (char?)  lng : default; } }

        public  byte?   Int8Null    { get { AssertType(VarTypeNullableLong.Instance); return obj != null ? (byte?)  lng : default; } }
        public  short?  Int16Null   { get { AssertType(VarTypeNullableLong.Instance); return obj != null ? (short?) lng : default; } }
        public  int?    Int32Null   { get { AssertType(VarTypeNullableLong.Instance); return obj != null ? (int?)   lng : default; } }
        public  long?   Int64Null   { get { AssertType(VarTypeNullableLong.Instance); return obj != null ? (long?)  lng : default; } }
        
        public  float?  Flt32Null   { get { AssertType(VarTypeNullableDbl.Instance);  return obj != null ? (float?) dbl : default; } }
        public  double? Flt64Null   { get { AssertType(VarTypeNullableDbl.Instance);  return obj != null ? (double?)dbl : default; } }

        
        public  override  string  ToString()              =>  type.AsString(this);

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
        public Var (object value) {
            type    = VarTypeObject.Instance;
            obj     = value;
            lng     = 0;
            dbl     = 0;
        }
        
        public Var (string value) {
            type    = VarTypeString.Instance;
            obj     = value;
            lng     = 0;
            dbl     = 0;
        }
        
        // --- long (int64) ---
        public Var (long value) {
            type    = VarTypeLong.Instance;
            obj     = HasValue;
            lng     = value;
            dbl     = 0;
        }

        public Var (long? value) {
            type    = VarTypeNullableLong.Instance;
            obj     = value.HasValue ? HasValue : null;
            lng     = value ?? 0L;
            dbl     = 0;
        }
        
        // --- double (64 bit) ---
        public Var (double value) {
            type    = VarTypeDbl.Instance;
            obj     = HasValue;
            lng     = 0;
            dbl     = value;
        }

        public Var (double? value) {
            type    = VarTypeNullableDbl.Instance;
            obj     = value.HasValue ? HasValue : null;
            lng     = 0;
            dbl     = value ?? 0L;
        }
        
        // --- bool ---
        public Var (bool value) {
            type    = VarTypeBool.Instance;
            obj     = HasValue;
            lng     = value ? 1 : 0;
            dbl     = 0;
        }

        public Var (bool? value) {
            type    = VarTypeNullableBool.Instance;
            obj     = value.HasValue ? HasValue : null;
            lng     = value.HasValue ? value.Value ? 1 : 0 : 0;
            dbl     = 0;
        }
        
        // --- char ---
        public Var (char value) {
            type    = VarTypeChar.Instance;
            obj     = HasValue;
            lng     = value;
            dbl     = 0;
        }

        public Var (char? value) {
            type    = VarTypeNullableChar.Instance;
            obj     = value.HasValue ? HasValue : null;
            lng     = value ?? 0;
            dbl     = 0;
        }
        
        private static readonly object HasValue = "HasValue";
    }
}
