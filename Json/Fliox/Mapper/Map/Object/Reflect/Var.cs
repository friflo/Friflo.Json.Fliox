// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

namespace Friflo.Json.Fliox.Mapper.Map.Object.Reflect
{
    public readonly partial struct Var
    {
        public override bool    Equals(object obj)  => throw new InvalidOperationException("not implemented intentionally");
        public override int     GetHashCode()       => throw new InvalidOperationException("not implemented intentionally");

        // --- Note! All fields must be private to ensure using only the type checked properties 
        [Browse(Never)] private   readonly  VarType type;
                        private   readonly  object  obj;
                        private   readonly  long    lng;
                        private   readonly  double  dbl;   // can merge with lng using BitConverter.DoubleToInt64Bits()
        
                        public  bool    IsNull      =>  type.IsNull(this);
        [Browse(Never)] public  bool    NotNull     => !type.IsNull(this);
        // --- reference
        [Browse(Never)] public  object  Object      { get { AssertType(VarTypeObject.Instance); return obj;           } } 
        [Browse(Never)] public  string  String      { get { AssertType(VarTypeString.Instance); return (string)obj;   } }
        
        // --- primitives
        [Browse(Never)] public  bool    Bool        { get { AssertType(VarTypeBool.Instance);   return lng != 0;        } }
        [Browse(Never)] public  char    Char        { get { AssertType(VarTypeChar.Instance);   return (char) lng;      } }

        [Browse(Never)] public  byte    Int8        { get { AssertType(VarTypeInt8.Instance);   return (byte) lng;      } }
        [Browse(Never)] public  short   Int16       { get { AssertType(VarTypeInt16.Instance);  return (short)lng;      } }
        [Browse(Never)] public  int     Int32       { get { AssertType(VarTypeInt32.Instance);  return (int)  lng;      } }
        [Browse(Never)] public  long    Int64       { get { AssertType(VarTypeInt64.Instance);  return        lng;      } }
        
        [Browse(Never)] public  float   Flt32       { get { AssertType(VarTypeFlt.Instance);    return (float)dbl;      } }
        [Browse(Never)] public  double  Flt64       { get { AssertType(VarTypeDbl.Instance);    return        dbl;      } }
        
        // --- nullable
        [Browse(Never)] public  bool?   BoolNull    { get { AssertType(VarTypeNullableBool.Instance);  return obj != null ? lng != 0 : (bool?)null; } }
        [Browse(Never)] public  char?   CharNull    { get { AssertType(VarTypeNullableChar.Instance);  return obj != null ? (char?)  lng : null; } }

        [Browse(Never)] public  byte?   Int8Null    { get { AssertType(VarTypeNullableInt8.Instance);  return obj != null ? (byte?)  lng : null; } }
        [Browse(Never)] public  short?  Int16Null   { get { AssertType(VarTypeNullableInt16.Instance); return obj != null ? (short?) lng : null; } }
        [Browse(Never)] public  int?    Int32Null   { get { AssertType(VarTypeNullableInt32.Instance); return obj != null ? (int?)   lng : null; } }
        [Browse(Never)] public  long?   Int64Null   { get { AssertType(VarTypeNullableInt64.Instance); return obj != null ? (long?)  lng : null; } }
        
        [Browse(Never)] public  float?  Flt32Null   { get { AssertType(VarTypeNullableFlt.Instance);   return obj != null ? (float?) dbl : null; } }
        [Browse(Never)] public  double? Flt64Null   { get { AssertType(VarTypeNullableDbl.Instance);   return obj != null ? (double?)dbl : null; } }

        
        public  override  string  ToString() =>  $"{{{type}}} {type.AsString(this)}";

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
        public Var (object value)  { type = VarTypeObject.Instance; obj = value; lng = 0; dbl = 0; }
        public Var (string value)  { type = VarTypeString.Instance; obj = value; lng = 0; dbl = 0; }
        
        // --- primitives
        public Var (char    value) { type = VarTypeChar.Instance;   obj = HasValue; lng = value; dbl = 0; }
        
        public Var (byte    value) { type = VarTypeInt8.Instance;   obj = HasValue; lng = value; dbl = 0; }
        public Var (short   value) { type = VarTypeInt16.Instance;  obj = HasValue; lng = value; dbl = 0; }
        public Var (int     value) { type = VarTypeInt32.Instance;  obj = HasValue; lng = value; dbl = 0; }
        public Var (long    value) { type = VarTypeInt64.Instance;  obj = HasValue; lng = value; dbl = 0; }

        public Var (float   value) { type = VarTypeFlt.Instance;    obj = HasValue; lng = 0; dbl = value; }
        public Var (double  value) { type = VarTypeDbl.Instance;    obj = HasValue; lng = 0; dbl = value; }

        // --- nullable primitives
        public Var (char? value)   { type = VarTypeNullableChar.Instance;  obj = value.HasValue ? HasValue : null; lng = value ?? 0; dbl = 0; }
        
        public Var (byte?   value) { type = VarTypeNullableInt8.Instance;  obj = value.HasValue ? HasValue : null; lng = value ?? 0; dbl = 0; }
        public Var (short?  value) { type = VarTypeNullableInt16.Instance; obj = value.HasValue ? HasValue : null; lng = value ?? 0; dbl = 0; }
        public Var (int?    value) { type = VarTypeNullableInt32.Instance; obj = value.HasValue ? HasValue : null; lng = value ?? 0; dbl = 0; }
        public Var (long?   value) { type = VarTypeNullableInt64.Instance; obj = value.HasValue ? HasValue : null; lng = value ?? 0; dbl = 0; }

        public Var (float?  value) { type = VarTypeNullableFlt.Instance;   obj = value.HasValue ? HasValue : null; lng = 0; dbl = value ?? 0; }
        public Var (double? value) { type = VarTypeNullableDbl.Instance;   obj = value.HasValue ? HasValue : null; lng = 0; dbl = value ?? 0; }
        

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

        private static readonly object HasValue = "HasValue";
    }
}
