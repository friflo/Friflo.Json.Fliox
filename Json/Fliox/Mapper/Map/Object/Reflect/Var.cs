// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Mapper.Map.Object.Reflect
{
    internal readonly struct Var
    {
        public override bool    Equals(object obj)  => throw new InvalidOperationException("not implemented intentionally");
        public override int     GetHashCode()       => throw new InvalidOperationException("not implemented intentionally");

        internal  readonly  VarType type;
        internal  readonly  object  obj;
        internal  readonly  long    lng;
        internal  readonly  double  dbl;   // can merge with lng using BitConverter.DoubleToInt64Bits()
        
        internal            bool    IsEqual(in Var other)   =>  type.AreEqual(this, other);
        internal            bool    IsNull                  =>  type.IsNull(this);
        internal            bool    NotNull                 => !type.IsNull(this);
        internal            string  AsString()              =>  type.AsString(this);
        
        public    override  string  ToString()              =>  AsString();

        public static bool operator == (in Var val1, in Var val2) =>  val1.type.AreEqual(val1, val2);
        public static bool operator != (in Var val1, in Var val2) => !val1.type.AreEqual(val1, val2);
        
        // --- object ---
        internal Var (object value) {
            type    = VarTypeObject.Instance;
            obj     = value;
            lng     = 0;
            dbl     = 0;
        }
        
        internal Var (string value) {
            type    = VarTypeString.Instance;
            obj     = value;
            lng     = 0;
            dbl     = 0;
        }
        
        // --- long (int64) ---
        internal Var (long value) {
            type    = VarTypeLong.Instance;
            obj     = HasValue;
            lng     = value;
            dbl     = 0;
        }

        internal Var (long? value) {
            type    = VarTypeNullableLong.Instance;
            obj     = value.HasValue ? HasValue : null;
            lng     = value ?? 0L;
            dbl     = 0;
        }
        
        // --- double (64 bit) ---
        internal Var (double value) {
            type    = VarTypeDbl.Instance;
            obj     = HasValue;
            lng     = 0;
            dbl     = value;
        }

        internal Var (double? value) {
            type    = VarTypeNullableDbl.Instance;
            obj     = value.HasValue ? HasValue : null;
            lng     = 0;
            dbl     = value ?? 0L;
        }
        
        private static readonly object HasValue = "HasValue";
    }
    
    


    
    
}