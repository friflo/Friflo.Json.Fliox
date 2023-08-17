using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map
{
public partial struct Var
{
    public abstract class Member {
        public      abstract    Var     GetVar (object obj);
        public      abstract    void    SetVar (object obj, in Var value);
    }
    
    // --- object
    private sealed class MemberObject<T> : Member {
        private     readonly    Func  <T, object>   getter;
        private     readonly    Action<T, object>   setter;
        private     readonly    object              defaultValue;
        
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value) {
            if (value.obj != null) {
                setter((T)obj, value.obj);
                return;
            }
            setter((T)obj, defaultValue);
        }

        internal    MemberObject(MemberInfo mi) {
            var type    = mi is FieldInfo field ? field.FieldType : (mi as PropertyInfo).PropertyType;
            if (type.IsValueType) {
                defaultValue = Activator.CreateInstance(type);
            }
            getter = DelegateUtils.CreateMemberGetter<T,object>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,object>(mi);
        }
    }
    
    // --- string
    private sealed class MemberString<T> : Member {
        private     readonly    Func  <T, string>   getter;
        private     readonly    Action<T, string>   setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.String);
    
        internal    MemberString(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,string>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,string>(mi);
        }
    }
    
    // --- integer
    private sealed class MemberInt8<T> : Member {
        private     readonly    Func  <T, byte>     getter;
        private     readonly    Action<T, byte>     setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int8);
    
        internal    MemberInt8(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,byte>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,byte>(mi);
        }
    }
    
    private sealed class MemberInt16<T> : Member {
        private     readonly    Func  <T, short>    getter;
        private     readonly    Action<T, short>    setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int16);
    
        internal    MemberInt16(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,short>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,short>(mi);
        }
    }
    
    private sealed class MemberInt32<T> : Member {
        private     readonly    Func  <T, int>      getter;
        private     readonly    Action<T, int>      setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int32);
    
        internal    MemberInt32(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,int>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,int>(mi);
        }
    }
    
    private sealed class MemberInt64<T> : Member {
        private     readonly    Func  <T, long>     getter;
        private     readonly    Action<T, long>     setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int64);
    
        internal    MemberInt64(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,long>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,long>(mi);
        }
    }
    
    // --- integer nullable
    private sealed class MemberInt8Null<T> : Member {
        private     readonly    Func  <T, byte?>    getter;
        private     readonly    Action<T, byte?>    setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int8Null);
    
        internal    MemberInt8Null(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,byte?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,byte?>(mi);
        }
    }
    
    private sealed class MemberInt16Null<T> : Member {
        private     readonly    Func  <T, short?>   getter;
        private     readonly    Action<T, short?>   setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int16Null);
    
        internal    MemberInt16Null(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,short?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,short?>(mi);
        }
    }
    
    private sealed class MemberInt32Null<T> : Member {
        private     readonly    Func  <T, int?>     getter;
        private     readonly    Action<T, int?>     setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int32Null);
    
        internal    MemberInt32Null(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,int?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,int?>(mi);
        }
    }
    
    private sealed class MemberInt64Null<T> : Member {
        private     readonly    Func  <T, long?>    getter;
        private     readonly    Action<T, long?>    setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int64Null);
    
        internal    MemberInt64Null(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,long?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,long?>(mi);
        }
    }
    
    // --- float (32 bit) ---
    private sealed class MemberFlt<T> : Member {
        private     readonly    Func  <T, float>    getter;
        private     readonly    Action<T, float>    setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Flt32);
    
        internal    MemberFlt(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,float>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,float>(mi);
        }
    }
    
    private sealed class MemberFltNull<T> : Member {
        private     readonly    Func  <T, float?>   getter;
        private     readonly    Action<T, float?>   setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Flt32Null);
    
        internal    MemberFltNull(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,float?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,float?>(mi);
        }
    }
    
    // --- double (64 bit) ---
    private sealed class MemberDbl<T> : Member {
        private     readonly    Func  <T, double>   getter;
        private     readonly    Action<T, double>   setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Flt64);
    
        internal    MemberDbl(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,double>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,double>(mi);
        }
    }
    
    private sealed class MemberDblNull<T> : Member {
        private     readonly    Func  <T, double?>  getter;
        private     readonly    Action<T, double?>  setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Flt64Null);
    
        internal    MemberDblNull(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,double?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,double?>(mi);
        }
    }
    
    // --- bool ---
    private sealed class MemberBool<T> : Member {
        private     readonly    Func  <T, bool>     getter;
        private     readonly    Action<T, bool>     setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Bool);
    
        internal    MemberBool(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,bool>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,bool>(mi);
        }
    }
    
    private sealed class MemberBoolNull<T> : Member {
        private     readonly    Func  <T, bool?>    getter;
        private     readonly    Action<T, bool?>    setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.BoolNull);
    
        internal    MemberBoolNull(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,bool?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,bool?>(mi);
        }
    }
    
    // --- char ---
    private sealed class MemberChar<T> : Member {
        private     readonly    Func  <T, char>     getter;
        private     readonly    Action<T, char>     setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Char);
    
        internal    MemberChar(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,char>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,char>(mi);
        }
    }
    
    private sealed class MemberCharNull<T> : Member {
        private     readonly    Func  <T, char?>    getter;
        private     readonly    Action<T, char?>    setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.CharNull);
    
        internal    MemberCharNull(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,char?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,char?>(mi);
        }
    }
    
    // --- DateTime ---
    private sealed class MemberDateTime<T> : Member {
        private     readonly    Func  <T, DateTime> getter;
        private     readonly    Action<T, DateTime> setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.DateTime);
    
        internal    MemberDateTime(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,DateTime>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,DateTime>(mi);
        }
    }
    
    private sealed class MemberDateTimeNull<T> : Member {
        private     readonly    Func  <T,DateTime?> getter;
        private     readonly    Action<T,DateTime?> setter;
        public      override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        public      override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.DateTimeNull);
    
        internal    MemberDateTimeNull(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,DateTime?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,DateTime?>(mi);
        }
    }
}
}