using System;
using System.Reflection;

namespace Friflo.Json.Fliox.Mapper.Map
{
public partial struct Var
{
    internal abstract class Member {
        internal    abstract    Var     GetVar (object obj);
        internal    abstract    void    SetVar (object obj, in Var value);
    }
    
    private static Func<T, TValue>      CreateGet<T,TValue>(MethodInfo method) {
        return (Func<T, TValue>)  Delegate.CreateDelegate  (typeof(Func<T, TValue>), method);
    }
    private static Action<T, TValue>    CreateSet<T,TValue>(MethodInfo method) {
        return (Action<T, TValue>)Delegate.CreateDelegate  (typeof(Action<T, TValue>), method);
    }


    // --- integer
    private class MemberInt8<T> : Member {
        private     readonly    Func  <T, byte>     getter;
        private     readonly    Action<T, byte>     setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int8);
    
        internal    MemberInt8(PropertyInfo mi) {
            getter = CreateGet<T,byte>(mi.GetGetMethod(true));
            setter = CreateSet<T,byte>(mi.GetSetMethod(true));
        }
    }
    
    private class MemberInt16<T> : Member {
        private     readonly    Func  <T, short>    getter;
        private     readonly    Action<T, short>    setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int16);
    
        internal    MemberInt16(PropertyInfo mi) {
            getter = CreateGet<T,short>(mi.GetGetMethod(true));
            setter = CreateSet<T,short>(mi.GetSetMethod(true));
        }
    }
    
    private class MemberInt32<T> : Member {
        private     readonly    Func  <T, int>      getter;
        private     readonly    Action<T, int>      setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int32);
    
        internal    MemberInt32(PropertyInfo mi) {
            getter = CreateGet<T,int>(mi.GetGetMethod(true));
            setter = CreateSet<T,int>(mi.GetSetMethod(true));
        }
    }
    
    private class MemberInt64<T> : Member {
        private     readonly    Func  <T, long>     getter;
        private     readonly    Action<T, long>     setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int64);
    
        internal    MemberInt64(PropertyInfo mi) {
            getter = CreateGet<T,long>(mi.GetGetMethod(true));
            setter = CreateSet<T,long>(mi.GetSetMethod(true));
        }
    }
    
    // --- integer nullable
    private class MemberInt8Null<T> : Member {
        private     readonly    Func  <T, byte?>    getter;
        private     readonly    Action<T, byte?>    setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int8Null);
    
        internal    MemberInt8Null(PropertyInfo mi) {
            getter = CreateGet<T,byte?>(mi.GetGetMethod(true));
            setter = CreateSet<T,byte?>(mi.GetSetMethod(true));
        }
    }
    
    private class MemberInt16Null<T> : Member {
        private     readonly    Func  <T, short?>   getter;
        private     readonly    Action<T, short?>   setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int16Null);
    
        internal    MemberInt16Null(PropertyInfo mi) {
            getter = CreateGet<T,short?>(mi.GetGetMethod(true));
            setter = CreateSet<T,short?>(mi.GetSetMethod(true));
        }
    }
    
    private class MemberInt32Null<T> : Member {
        private     readonly    Func  <T, int?>     getter;
        private     readonly    Action<T, int?>     setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int32Null);
    
        internal    MemberInt32Null(PropertyInfo mi) {
            getter = CreateGet<T,int?>(mi.GetGetMethod(true));
            setter = CreateSet<T,int?>(mi.GetSetMethod(true));
        }
    }
    
    private class MemberInt64Null<T> : Member {
        private     readonly    Func  <T, long?>    getter;
        private     readonly    Action<T, long?>    setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int64Null);
    
        internal    MemberInt64Null(PropertyInfo mi) {
            getter = CreateGet<T,long?>(mi.GetGetMethod(true));
            setter = CreateSet<T,long?>(mi.GetSetMethod(true));
        }
    }
    
    // --- float (32 bit) ---
    private class MemberFlt<T> : Member {
        private     readonly    Func  <T, float>    getter;
        private     readonly    Action<T, float>    setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Flt32);
    
        internal    MemberFlt(PropertyInfo mi) {
            getter = CreateGet<T,float>(mi.GetGetMethod(true));
            setter = CreateSet<T,float>(mi.GetSetMethod(true));
        }
    }
    
    private class MemberFltNull<T> : Member {
        private     readonly    Func  <T, float?>   getter;
        private     readonly    Action<T, float?>   setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Flt32Null);
    
        internal    MemberFltNull(PropertyInfo mi) {
            getter = CreateGet<T,float?>(mi.GetGetMethod(true));
            setter = CreateSet<T,float?>(mi.GetSetMethod(true));
        }
    }
    
    // --- double (64 bit) ---
    private class MemberDbl<T> : Member {
        private     readonly    Func  <T, double>   getter;
        private     readonly    Action<T, double>   setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Flt64);
    
        internal    MemberDbl(PropertyInfo mi) {
            getter = CreateGet<T,double>(mi.GetGetMethod(true));
            setter = CreateSet<T,double>(mi.GetSetMethod(true));
        }
    }
    
    private class MemberDblNull<T> : Member {
        private     readonly    Func  <T, double?>  getter;
        private     readonly    Action<T, double?>  setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Flt64Null);
    
        internal    MemberDblNull(PropertyInfo mi) {
            getter = CreateGet<T,double?>(mi.GetGetMethod(true));
            setter = CreateSet<T,double?>(mi.GetSetMethod(true));
        }
    }
    
    // --- bool ---
    private class MemberBool<T> : Member {
        private     readonly    Func  <T, bool>     getter;
        private     readonly    Action<T, bool>     setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Bool);
    
        internal    MemberBool(PropertyInfo mi) {
            getter = CreateGet<T,bool>(mi.GetGetMethod(true));
            setter = CreateSet<T,bool>(mi.GetSetMethod(true));
        }
    }
    
    private class MemberBoolNull<T> : Member {
        private     readonly    Func  <T, bool?>  getter;
        private     readonly    Action<T, bool?>  setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.BoolNull);
    
        internal    MemberBoolNull(PropertyInfo mi) {
            getter = CreateGet<T,bool?>(mi.GetGetMethod(true));
            setter = CreateSet<T,bool?>(mi.GetSetMethod(true));
        }
    }
    
    // --- char ---
    private class MemberChar<T> : Member {
        private     readonly    Func  <T, char>     getter;
        private     readonly    Action<T, char>     setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Char);
    
        internal    MemberChar(PropertyInfo mi) {
            getter = CreateGet<T,char>(mi.GetGetMethod(true));
            setter = CreateSet<T,char>(mi.GetSetMethod(true));
        }
    }
    
    private class MemberCharNull<T> : Member {
        private     readonly    Func  <T, char?>  getter;
        private     readonly    Action<T, char?>  setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.CharNull);
    
        internal    MemberCharNull(PropertyInfo mi) {
            getter = CreateGet<T,char?>(mi.GetGetMethod(true));
            setter = CreateSet<T,char?>(mi.GetSetMethod(true));
        }
    }
}
}