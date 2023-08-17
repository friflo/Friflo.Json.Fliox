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
        public      abstract    void    Copy   (object from, object to);
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));

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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));

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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));

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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));

    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
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
        public      override    void                Copy   (object from, object to)     => setter((T)to, getter((T)from));
    
        internal    MemberDateTimeNull(MemberInfo mi) {
            getter = DelegateUtils.CreateMemberGetter<T,DateTime?>(mi);
            setter = DelegateUtils.CreateMemberSetter<T,DateTime?>(mi);
        }
    }
}
}