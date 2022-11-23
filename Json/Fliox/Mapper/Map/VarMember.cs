using System;
using System.Reflection;

namespace Friflo.Json.Fliox.Mapper.Map
{
public partial struct Var
{
    internal readonly struct MemberMethods
    {
        internal readonly  MethodInfo  getter;
        internal readonly  MethodInfo  setter;
        
        internal MemberMethods (MethodInfo getter, MethodInfo setter) {
            this.getter = getter;
            this.setter = setter;
        }
    }

    public abstract class Member {
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
    
        internal    MemberInt8(MemberMethods mm) {
            getter = CreateGet<T,byte>(mm.getter);
            setter = CreateSet<T,byte>(mm.setter);
        }
    }
    
    private class MemberInt16<T> : Member {
        private     readonly    Func  <T, short>    getter;
        private     readonly    Action<T, short>    setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int16);
    
        internal    MemberInt16(MemberMethods mm) {
            getter = CreateGet<T,short>(mm.getter);
            setter = CreateSet<T,short>(mm.setter);
        }
    }
    
    private class MemberInt32<T> : Member {
        private     readonly    Func  <T, int>      getter;
        private     readonly    Action<T, int>      setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int32);
    
        internal    MemberInt32(MemberMethods mm) {
            getter = CreateGet<T,int>(mm.getter);
            setter = CreateSet<T,int>(mm.setter);
        }
    }
    
    private class MemberInt64<T> : Member {
        private     readonly    Func  <T, long>     getter;
        private     readonly    Action<T, long>     setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int64);
    
        internal    MemberInt64(MemberMethods mm) {
            getter = CreateGet<T,long>(mm.getter);
            setter = CreateSet<T,long>(mm.setter);
        }
    }
    
    // --- integer nullable
    private class MemberInt8Null<T> : Member {
        private     readonly    Func  <T, byte?>    getter;
        private     readonly    Action<T, byte?>    setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int8Null);
    
        internal    MemberInt8Null(MemberMethods mm) {
            getter = CreateGet<T,byte?>(mm.getter);
            setter = CreateSet<T,byte?>(mm.setter);
        }
    }
    
    private class MemberInt16Null<T> : Member {
        private     readonly    Func  <T, short?>   getter;
        private     readonly    Action<T, short?>   setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int16Null);
    
        internal    MemberInt16Null(MemberMethods mm) {
            getter = CreateGet<T,short?>(mm.getter);
            setter = CreateSet<T,short?>(mm.setter);
        }
    }
    
    private class MemberInt32Null<T> : Member {
        private     readonly    Func  <T, int?>     getter;
        private     readonly    Action<T, int?>     setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int32Null);
    
        internal    MemberInt32Null(MemberMethods mm) {
            getter = CreateGet<T,int?>(mm.getter);
            setter = CreateSet<T,int?>(mm.setter);
        }
    }
    
    private class MemberInt64Null<T> : Member {
        private     readonly    Func  <T, long?>    getter;
        private     readonly    Action<T, long?>    setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Int64Null);
    
        internal    MemberInt64Null(MemberMethods mm) {
            getter = CreateGet<T,long?>(mm.getter);
            setter = CreateSet<T,long?>(mm.setter);
        }
    }
    
    // --- float (32 bit) ---
    private class MemberFlt<T> : Member {
        private     readonly    Func  <T, float>    getter;
        private     readonly    Action<T, float>    setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Flt32);
    
        internal    MemberFlt(MemberMethods mm) {
            getter = CreateGet<T,float>(mm.getter);
            setter = CreateSet<T,float>(mm.setter);
        }
    }
    
    private class MemberFltNull<T> : Member {
        private     readonly    Func  <T, float?>   getter;
        private     readonly    Action<T, float?>   setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Flt32Null);
    
        internal    MemberFltNull(MemberMethods mm) {
            getter = CreateGet<T,float?>(mm.getter);
            setter = CreateSet<T,float?>(mm.setter);
        }
    }
    
    // --- double (64 bit) ---
    private class MemberDbl<T> : Member {
        private     readonly    Func  <T, double>   getter;
        private     readonly    Action<T, double>   setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Flt64);
    
        internal    MemberDbl(MemberMethods mm) {
            getter = CreateGet<T,double>(mm.getter);
            setter = CreateSet<T,double>(mm.setter);
        }
    }
    
    private class MemberDblNull<T> : Member {
        private     readonly    Func  <T, double?>  getter;
        private     readonly    Action<T, double?>  setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Flt64Null);
    
        internal    MemberDblNull(MemberMethods mm) {
            getter = CreateGet<T,double?>(mm.getter);
            setter = CreateSet<T,double?>(mm.setter);
        }
    }
    
    // --- bool ---
    private class MemberBool<T> : Member {
        private     readonly    Func  <T, bool>     getter;
        private     readonly    Action<T, bool>     setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Bool);
    
        internal    MemberBool(MemberMethods mm) {
            getter = CreateGet<T,bool>(mm.getter);
            setter = CreateSet<T,bool>(mm.setter);
        }
    }
    
    private class MemberBoolNull<T> : Member {
        private     readonly    Func  <T, bool?>    getter;
        private     readonly    Action<T, bool?>    setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.BoolNull);
    
        internal    MemberBoolNull(MemberMethods mm) {
            getter = CreateGet<T,bool?>(mm.getter);
            setter = CreateSet<T,bool?>(mm.setter);
        }
    }
    
    // --- char ---
    private class MemberChar<T> : Member {
        private     readonly    Func  <T, char>     getter;
        private     readonly    Action<T, char>     setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.Char);
    
        internal    MemberChar(MemberMethods mm) {
            getter = CreateGet<T,char>(mm.getter);
            setter = CreateSet<T,char>(mm.setter);
        }
    }
    
    private class MemberCharNull<T> : Member {
        private     readonly    Func  <T, char?>    getter;
        private     readonly    Action<T, char?>    setter;
        internal    override    Var                 GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void                SetVar (object obj, in Var value)   => setter((T)obj, value.CharNull);
    
        internal    MemberCharNull(MemberMethods mm) {
            getter = CreateGet<T,char?>(mm.getter);
            setter = CreateSet<T,char?>(mm.setter);
        }
    }
}
}