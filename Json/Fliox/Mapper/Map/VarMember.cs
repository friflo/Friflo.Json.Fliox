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

    private class MemberInt32<T> : Member {
        private     readonly    Func  <T, int>  getter;
        private     readonly    Action<T, int>  setter;
        internal    override    Var             GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void            SetVar (object obj, in Var value)   => setter((T)obj, value.Int32);
    
        internal    MemberInt32(PropertyInfo mi) {
            getter = CreateGet<T,int>(mi.GetGetMethod(true));
            setter = CreateSet<T,int>(mi.GetSetMethod(true));
        }
    }
    
    private class MemberInt64<T> : Member {
        private     readonly    Func  <T, long>  getter;
        private     readonly    Action<T, long>  setter;
        internal    override    Var             GetVar (object obj)                 => new Var(getter((T)obj));
        internal    override    void            SetVar (object obj, in Var value)   => setter((T)obj, value.Int64);
    
        internal    MemberInt64(PropertyInfo mi) {
            getter = CreateGet<T,long>(mi.GetGetMethod(true));
            setter = CreateSet<T,long>(mi.GetSetMethod(true));
        }
    }
}
}