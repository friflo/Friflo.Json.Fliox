// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;
using static Friflo.Json.Fliox.Mapper.Map.Var.Static;

// ReSharper disable MergeConditionalExpression
namespace Friflo.Json.Fliox.Mapper.Map
{
    /// <summary>
    /// Use as "union type" struct to store either a long, double or DateTime
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct VarIntern
    {
        [FieldOffset(0)] internal   double      dbl;
        [FieldOffset(0)] internal   long        lng;
        [FieldOffset(0)] internal   DateTime    dt;
    }
    
// NON_CLS
#pragma warning disable 3003  // Warning CS3003 : Type of '...' is not CLS-compliant
#pragma warning disable 3001  // Warning CS3001 : Argument type '...' is not CLS-compliant
    
    
    /// <summary>
    /// <see cref="Var"/> is used to prevent boxing of primitives types when: <br/>
    /// - serializing primitive JSON values like: 123, "abc", true or false <br/>
    /// - accessing primitive class fields or properties like: bool, byte, short, int, long, char, float and double. <br/> 
    /// </summary>
    [CLSCompliant(true)]
    public readonly partial struct Var
    {
        public override bool    Equals(object obj)  => throw new InvalidOperationException("not implemented intentionally");
        public override int     GetHashCode()       => throw new InvalidOperationException("not implemented intentionally");

        // --- Note! All fields must be private to ensure using only the type checked properties 
        private   readonly      VarType     type;   // 8 bytes
        private   readonly      object      obj;    // 8 bytes
        private   readonly      VarIntern   intern; // 8 bytes
                        
        internal                object  TryGetObject()  =>  type.TryGetObject(this);
        internal                object  ToObject()      =>  type.ToObject(this);
        
        internal  new           Type    GetType()       =>  type.GetType(this);
        
                        public  bool    IsNull          =>  type.IsNull(this);
        [Browse(Never)] public  bool    NotNull         => !type.IsNull(this);
        // --- reference
        [Browse(Never)] public  object  Object      { get { AssertType(TypeObject.Instance); return obj;           } } 
        [Browse(Never)] public  string  String      { get { AssertType(TypeString.Instance); return (string)obj;   } }
        
        // --- primitives
        [Browse(Never)] public  bool    Bool        { get { AssertType(TypeBool.Instance);   return intern.lng != 0;        } }
        [Browse(Never)] public  char    Char        { get { AssertType(TypeChar.Instance);   return (char) intern.lng;      } }

        [Browse(Never)] public  byte    Int8        { get { AssertType(TypeInt8.Instance);   return (byte) intern.lng;      } }
        [Browse(Never)] public  short   Int16       { get { AssertType(TypeInt16.Instance);  return (short)intern.lng;      } }
        [Browse(Never)] public  int     Int32       { get { AssertType(TypeInt32.Instance);  return (int)  intern.lng;      } }
        [Browse(Never)] public  long    Int64       { get { AssertType(TypeInt64.Instance);  return        intern.lng;      } }
        
        // --- NON_CLS
        [Browse(Never)] public  sbyte   SInt8       { get { AssertType(TypeSInt8.Instance);  return (sbyte) intern.lng;     } }
        [Browse(Never)] public  ushort  UInt16      { get { AssertType(TypeUInt16.Instance); return (ushort)intern.lng;     } }
        [Browse(Never)] public  uint    UInt32      { get { AssertType(TypeUInt32.Instance); return (uint)  intern.lng;     } }
        [Browse(Never)] public  ulong   UInt64      { get { AssertType(TypeUInt64.Instance); return (ulong) intern.lng;     } }
        
        [Browse(Never)] public  float   Flt32       { get { AssertType(TypeFlt.Instance);    return (float)intern.dbl;      } }
        [Browse(Never)] public  double  Flt64       { get { AssertType(TypeDbl.Instance);    return        intern.dbl;      } }
        
        [Browse(Never)] public  DateTime DateTime   { get { AssertType(TypeDateTime.Instance);return       intern.dt; } }
        
        // --- nullable
        [Browse(Never)] public  bool?   BoolNull    { get { AssertType(TypeNullableBool.Instance);  return obj != null ? intern.lng != 0 : (bool?)null; } }
        [Browse(Never)] public  char?   CharNull    { get { AssertType(TypeNullableChar.Instance);  return obj != null ? (char?)  intern.lng : null; } }

        [Browse(Never)] public  byte?   Int8Null    { get { AssertType(TypeNullableInt8.Instance);  return obj != null ? (byte?)  intern.lng : null; } }
        [Browse(Never)] public  short?  Int16Null   { get { AssertType(TypeNullableInt16.Instance); return obj != null ? (short?) intern.lng : null; } }
        [Browse(Never)] public  int?    Int32Null   { get { AssertType(TypeNullableInt32.Instance); return obj != null ? (int?)   intern.lng : null; } }
        [Browse(Never)] public  long?   Int64Null   { get { AssertType(TypeNullableInt64.Instance); return obj != null ? (long?)  intern.lng : null; } }
        
        // --- NON_CLS
        [Browse(Never)] public  sbyte?  SInt8Null   { get { AssertType(TypeNullableSInt8.Instance);  return obj != null ? (sbyte?)  intern.lng : null; } }
        [Browse(Never)] public  ushort? UInt16Null  { get { AssertType(TypeNullableUInt16.Instance); return obj != null ? (ushort?) intern.lng : null; } }
        [Browse(Never)] public  uint?   UInt32Null  { get { AssertType(TypeNullableUInt32.Instance); return obj != null ? (uint?)   intern.lng : null; } }
        [Browse(Never)] public  ulong?  UInt64Null  { get { AssertType(TypeNullableUInt64.Instance); return obj != null ? (ulong?)  intern.lng : null; } }
        
        [Browse(Never)] public  float?  Flt32Null   { get { AssertType(TypeNullableFlt.Instance);   return obj != null ? (float?) intern.dbl : null; } }
        [Browse(Never)] public  double? Flt64Null   { get { AssertType(TypeNullableDbl.Instance);   return obj != null ? (double?)intern.dbl : null; } }
        [Browse(Never)] public DateTime?DateTimeNull{ get { AssertType(TypeNullableDateTime.Instance);return obj!=null ? (DateTime?)intern.dt : null; } }

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
        public Var (object value)  { type = TypeObject.Instance; obj = value; intern = default; }
        public Var (string value)  { type = TypeString.Instance; obj = value; intern = default; }
        
        // --- primitives
        public Var (char    value) { type = TypeChar.Instance;   obj = HasValue; intern = new VarIntern { lng = value }; }
        
        public Var (byte    value) { type = TypeInt8.Instance;   obj = HasValue; intern = new VarIntern { lng = value }; }
        public Var (short   value) { type = TypeInt16.Instance;  obj = HasValue; intern = new VarIntern { lng = value }; }
        public Var (int     value) { type = TypeInt32.Instance;  obj = HasValue; intern = new VarIntern { lng = value }; }
        public Var (long    value) { type = TypeInt64.Instance;  obj = HasValue; intern = new VarIntern { lng = value }; }
        
        // --- NON_CLS
        public Var (sbyte   value) { type = TypeSInt8.Instance;  obj = HasValue; intern = new VarIntern { lng = value }; }
        public Var (ushort  value) { type = TypeUInt16.Instance; obj = HasValue; intern = new VarIntern { lng = value }; }
        public Var (uint    value) { type = TypeUInt32.Instance; obj = HasValue; intern = new VarIntern { lng = value }; }
        public Var (ulong   value) { type = TypeUInt64.Instance; obj = HasValue; intern = new VarIntern { lng = (long)value }; }

        public Var (float   value) { type = TypeFlt.Instance;    obj = HasValue; intern = new VarIntern { dbl = value }; }
        public Var (double  value) { type = TypeDbl.Instance;    obj = HasValue; intern = new VarIntern { dbl = value }; }
        public Var (DateTime value){ type = TypeDateTime.Instance;obj= HasValue; intern = new VarIntern { dt  = value }; }

        // --- nullable primitives
        public Var (char? value)   { type = TypeNullableChar.Instance;  obj = value.HasValue ? HasValue : null; intern = new VarIntern { lng = value ?? 0 }; }
        
        public Var (byte?   value) { type = TypeNullableInt8.Instance;  obj = value.HasValue ? HasValue : null; intern = new VarIntern { lng = value ?? 0 }; }
        public Var (short?  value) { type = TypeNullableInt16.Instance; obj = value.HasValue ? HasValue : null; intern = new VarIntern { lng = value ?? 0 }; }
        public Var (int?    value) { type = TypeNullableInt32.Instance; obj = value.HasValue ? HasValue : null; intern = new VarIntern { lng = value ?? 0 }; }
        public Var (long?   value) { type = TypeNullableInt64.Instance; obj = value.HasValue ? HasValue : null; intern = new VarIntern { lng = value ?? 0 }; }
        
        // --- NON_CLS
        public Var (sbyte?  value) { type = TypeNullableSInt8.Instance;  obj = value.HasValue ? HasValue : null; intern = new VarIntern { lng = value ?? 0 }; }
        public Var (ushort? value) { type = TypeNullableUInt16.Instance; obj = value.HasValue ? HasValue : null; intern = new VarIntern { lng = value ?? 0 }; }
        public Var (uint?   value) { type = TypeNullableUInt32.Instance; obj = value.HasValue ? HasValue : null; intern = new VarIntern { lng = value ?? 0 }; }
        public Var (ulong?  value) { type = TypeNullableUInt64.Instance; obj = value.HasValue ? HasValue : null; intern = new VarIntern { lng = (long?)value ?? 0 }; }

        public Var (float?  value) { type = TypeNullableFlt.Instance;   obj = value.HasValue ? HasValue : null; intern = new VarIntern { dbl = value.HasValue ? value.Value : 0 }; }
        public Var (double? value) { type = TypeNullableDbl.Instance;   obj = value.HasValue ? HasValue : null; intern = new VarIntern { dbl = value.HasValue ? value.Value : 0 }; }
        public Var (DateTime?value){ type = TypeNullableDateTime.Instance;obj=value.HasValue ? HasValue : null; intern = new VarIntern { dt  = value.HasValue ? value.Value : default };  }

  /*    /// <summary>using instead of <see cref="DateTime.ToBinary"/> which degrade performance by x100</summary>
        internal static long DateTime2Lng(DateTime dateTime) {
            return dateTime.Ticks | (long)dateTime.Kind << DateTimeKindShift;
        }
        
        /// <summary>using instead of <see cref="DateTime.FromBinary"/> which degrade performance by x100</summary>
        internal static DateTime Lng2DateTime(long lng) {
            return new DateTime(lng & DateTimeMaskTicks, (DateTimeKind)((ulong)lng >> DateTimeKindShift));
        }
        
        private const int   DateTimeKindShift = 62;
        private const long  DateTimeMaskTicks = 0x3FFFFFFFFFFFFFFF;

        */
        // --- bool ---
        public Var (bool value) {
            type    = TypeBool.Instance;
            obj     = HasValue;
            intern  = new VarIntern { lng = value ? 1 : 0 };
        }

        public Var (bool? value) {
            type    = TypeNullableBool.Instance;
            obj     = value.HasValue ? HasValue : null;
            intern  = new VarIntern { lng = value.HasValue ? value.Value ? 1 : 0 : 0 };
        }
        
        /// <summary> using a static class prevents noise in form of 'Static members' for class instances in Debugger </summary>
        internal static class Static  {
            internal static readonly object HasValue = "HasValue";
        }
    }
}
