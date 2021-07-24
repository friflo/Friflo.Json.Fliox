// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Flow.Schema.Utils
{
    public interface ITyp {
        string  Name         { get; }
        string  Namespace    { get; }
        ITyp    BaseType     { get; }
    }
    
    public class NativeType : ITyp
    {
        public   readonly   Type    native;
        internal            ITyp    baseType;
        
        public              string  Name      => native.Name;
        public              string  Namespace => native.Namespace;
        public              ITyp    BaseType  => baseType;
        
        public NativeType (Type type) {
            this.native     = type;
        }

        public override bool Equals(object obj) {
            if (obj == null)
                throw new NullReferenceException();
            var other = (NativeType)obj;
            return native == other.native;
        }

        public override int GetHashCode() {
            return (native != null ? native.GetHashCode() : 0);
        }
    }
}