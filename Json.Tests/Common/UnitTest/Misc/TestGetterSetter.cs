using System;
using System.Reflection;
using NUnit.Framework;

// ReSharper disable PossibleNullReferenceException
namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    public struct TestStruct
    {
        public int      value;
    }
    
    public class TestClass {
        public int      intValue;
        public int      Int2 { get; set; }
        public object   objValue;
    }
    
    public static class SampleGen
    {
        public static object    TestClass_objValue_get(TestClass obj)               => obj.objValue;
        public static void      TestClass_objValue_set(TestClass obj, object value) => obj.objValue = value;

        public static int       TestClass_intValue_get(TestClass obj)            => obj.intValue;
        public static void      TestClass_intValue_set(TestClass obj, int value) => obj.intValue = value;
    }
    
    public enum BuildInType {
        Int32,
        Int64,
        Object
    } 
    
    public readonly struct BuildInValue {
        public  readonly    BuildInType type;
        public  readonly    long        longValue;
        public  readonly    object      objValue;

        public BuildInValue (long value, BuildInType type) {
            this.type   = type;
            longValue   = value;
            objValue    = null;
        }
        
        public BuildInValue (object value, BuildInType type) {
            this.type   = type;
            longValue   = 0;
            objValue    = value;
        }
    }

    internal abstract class ObjectMemberAccess<T> {
        public abstract BuildInValue    GetValue (T obj);
        public abstract void            SetValue (T obj, in BuildInValue value);
    }
    
    internal class ObjectMemberAccessInt<T> : ObjectMemberAccess<T> {
        public Func     <T, int>    get;
        public Action   <T, int>    set;
        
        public override BuildInValue GetValue (T obj) => new BuildInValue(get(obj), BuildInType.Int32) ;
        public override void         SetValue (T obj, in BuildInValue value) => set(obj, (int)value.longValue);
    }
    
    internal class ObjectMemberAccessObject<T> : ObjectMemberAccess<T> {
        public Func     <T, object> get;
        public Action   <T, object> set;
        
        public override BuildInValue GetValue (T obj) => new BuildInValue(get(obj), BuildInType.Object) ;
        public override void         SetValue (T obj, in BuildInValue value) => set(obj, value.objValue);
    }
    
    internal static class DelegateUtils {
        public static Action<T, TValue> CreateSetDelegate<T,TValue>(MethodInfo method) {
            return (Action<T, TValue>)Delegate.CreateDelegate  (typeof(Action<T, TValue>), method);
        }
        public static Func<T, TValue>CreateGetDelegate<T,TValue>(MethodInfo method) {
            return (Func<T, TValue>)Delegate.CreateDelegate  (typeof(Func<T, TValue>), method);
        }
    }
    
    public static class TestGetterSetter
    {
        [Test]
        public static void  Test() {
            var testClass = new TestClass { intValue = 123, Int2 = 456 };
            
            var setIntMethod    = typeof(SampleGen).GetMethod(nameof(SampleGen.TestClass_intValue_set));
            var setIntDelegate  = DelegateUtils.CreateSetDelegate<TestClass, int>(setIntMethod);
            
            var getIntMethod    = typeof(SampleGen).GetMethod(nameof(SampleGen.TestClass_intValue_get));
            var getIntDelegate  = DelegateUtils.CreateGetDelegate<TestClass, int>(getIntMethod);
            //
            
            var getInt2Method   = typeof(TestClass).GetProperty(nameof(TestClass.Int2)).GetGetMethod(true);
            var getInt2Delegate = DelegateUtils.CreateGetDelegate<TestClass, int>(getInt2Method);
            
            var setInt2Method   = typeof(TestClass).GetProperty(nameof(TestClass.Int2)).GetSetMethod(true);
            var setInt2Delegate = DelegateUtils.CreateSetDelegate<TestClass, int>(setInt2Method);
            
            //
            var getObjMethod    = typeof(SampleGen).GetMethod(nameof(SampleGen.TestClass_objValue_get));
            var getObjDelegate  = DelegateUtils.CreateGetDelegate<TestClass, object>(getObjMethod);
            
            var setObjMethod    = typeof(SampleGen).GetMethod(nameof(SampleGen.TestClass_objValue_set));
            var setObjDelegate  = DelegateUtils.CreateSetDelegate<TestClass, object>(setObjMethod);
            
            var intAccess       = new ObjectMemberAccessInt<TestClass>       { get = getIntDelegate,  set = setIntDelegate };
            var int2Access      = new ObjectMemberAccessInt<TestClass>       { get = getInt2Delegate, set = setInt2Delegate };
            var objAccess       = new ObjectMemberAccessObject<TestClass>    { get = getObjDelegate,  set = setObjDelegate };

            
            var intField = typeof(TestClass).GetField("intValue");
            var objField = typeof(TestClass).GetField("objValue");

            var start = Mem.GetAllocatedBytes();
            
            var intValue = new BuildInValue(1,BuildInType.Int32);
            for (int n = 0; n < 50; n++) {
                // var value = testClass.intValue;                      // 50_000_000    12 ms, memory             0 bytes
                BuildInValue value = intAccess.GetValue(testClass);     // 50_000_000   110 ms, memory             0 bytes
                // BuildInValue value = int2Access.GetValue(testClass); // 50_000_000   110 ms, memory             0 bytes (Property)
                // intAccess.SetValue(testClass, intValue);             // 50_000_000   110 ms, memory             0 bytes
                // var value = (int)intField.GetValue(testClass);       // 50_000_000  2296 ms, memory 1.200.000.080 bytes
                
                // BuildInValue value = getObj.GetValue(testClass);     // 50_000_000   121 ms, memory             0 bytes
                // var value = objField.GetValue(testClass);            // 50_000_000  1336 ms, memory             0 bytes
            }
            var diff2 = Mem.GetAllocationDiff(start);
            
            Console.WriteLine($"TestStruct: {testClass.intValue} - {diff2}");
        }
    }
}