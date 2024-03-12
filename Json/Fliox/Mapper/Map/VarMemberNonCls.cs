using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map
{
// NON_CLS - whole file
public partial struct Var
{
   
    // --- integer
    private sealed class MemberSInt8<T> : Member {
        private     readonly    Func  <T, sbyte>    getter;
        private     readonly    Action<T, sbyte>    setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.SInt8);
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
        internal    MemberSInt8(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,sbyte>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,sbyte>(mi);
        }
    }
    
    private sealed class MemberUInt16<T> : Member {
        private     readonly    Func  <T, ushort>   getter;
        private     readonly    Action<T, ushort>   setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.UInt16);
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
        internal    MemberUInt16(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,ushort>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,ushort>(mi);
        }
    }
    
    private sealed class MemberUInt32<T> : Member {
        private     readonly    Func  <T, uint>     getter;
        private     readonly    Action<T, uint>     setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.UInt32);
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
        internal    MemberUInt32(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,uint>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,uint>(mi);
        }
    }
    
    private sealed class MemberUInt64<T> : Member {
        private     readonly    Func  <T, ulong>    getter;
        private     readonly    Action<T, ulong>    setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.UInt64);
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
        internal    MemberUInt64(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,ulong>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,ulong>(mi);
        }
    }
    
    // --- integer nullable
    private sealed class MemberSInt8Null<T> : Member {
        private     readonly    Func  <T, sbyte?>   getter;
        private     readonly    Action<T, sbyte?>   setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.SInt8Null);
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
        internal    MemberSInt8Null(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,sbyte?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,sbyte?>(mi);
        }
    }
    
    private sealed class MemberUInt16Null<T> : Member {
        private     readonly    Func  <T, ushort?>  getter;
        private     readonly    Action<T, ushort?>  setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.UInt16Null);
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
        internal    MemberUInt16Null(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,ushort?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,ushort?>(mi);
        }
    }
    
    private sealed class MemberUInt32Null<T> : Member {
        private     readonly    Func  <T, uint?>    getter;
        private     readonly    Action<T, uint?>    setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.UInt32Null);
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
        internal    MemberUInt32Null(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,uint?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,uint?>(mi);
        }
    }
    
    private sealed class MemberUInt64Null<T> : Member {
        private     readonly    Func  <T, ulong?>    getter;
        private     readonly    Action<T, ulong?>    setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.UInt64Null);
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
        internal    MemberUInt64Null(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,ulong?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,ulong?>(mi);
        }
    }
}
}