// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Object.Reflect
{
    // --- fields
    internal sealed class MemberField : Var.Member {
        private  readonly   VarType     varType;
        private  readonly   FieldInfo   field;
        
        internal MemberField(VarType varType, FieldInfo field) {
            this.varType    = varType;
            this.field      = field;
        }
        
        public    override    Var     GetVar (object obj) {
            // if (useDirect) return field.GetValueDirect(__makeref(obj));
            var value = field.GetValue(obj);
            return varType.FromObject(value);
        }
        
        public      override    void    SetVar (object obj, in Var value) {
            var valueObject = varType.ToObject(value);
            // if (useDirect) { field.SetValueDirect(__makeref(obj), value); return; }
            field.SetValue(obj, valueObject); // todo use Expression - but not for Unity
        }
        
        public      override    void    Copy   (object from, object to) {
            var value = field.GetValue(from);
            field.SetValue(to, value);
        }
    }
    
    // --- properties
    internal sealed class MemberProperty : Var.Member {
        private  readonly   VarType                 varType;
        private  readonly   Func<object, object>    getter;
        private  readonly   Action<object, object>  setter;
        
        internal MemberProperty(VarType varType, MemberInfo mi) {
            this.varType    = varType;
            getter          = DelegateUtils.CreateMemberGetter<object,object>(mi);
            setter          = DelegateUtils.CreateMemberSetter<object,object>(mi);
        }
        
        public    override    Var     GetVar (object obj) {
            var value = getter(obj); // return new Var(getMethod.Invoke(obj, null));
            return varType.FromObject(value);
        }
        
        public    override    void    SetVar (object obj, in Var value) {
            var valueObject = varType.ToObject(value);
            setter(obj, valueObject);
        }
        
        public    override    void    Copy   (object from, object to) => throw new NotImplementedException();
    }
}