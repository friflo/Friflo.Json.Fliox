using System;
using System.Reflection.Emit;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    struct TestFields
    {
        public int MaxValue;

        public int MaxValueProperty
        {
            get { return MaxValue; }
            set { MaxValue = value; }
        }
    };
    
    delegate void RefAction<T1, T2>(ref T1 arg1, T2 arg2);

    public class TestEmit
    {
        // samples from: [c# - Can I set a value on a struct through reflection without boxing? - Stack Overflow]
        // https://stackoverflow.com/questions/9927590/can-i-set-a-value-on-a-struct-through-reflection-without-boxing
        // 
        [Test]
        public void SetField()
        {
            var fieldInfo = typeof(TestFields).GetField("MaxValue");

            var dynamicMethod = new DynamicMethod(String.Empty, typeof(void), new Type[] { fieldInfo.ReflectedType.MakeByRefType(), fieldInfo.FieldType }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, fieldInfo);
            ilGenerator.Emit(OpCodes.Ret);
            var fieldSetter = (RefAction<TestFields, int>)dynamicMethod.CreateDelegate(typeof(RefAction<TestFields, int>));

            var fields = new TestFields { MaxValue = 1234 };
            
            fieldSetter(ref fields, 90);
            Console.WriteLine(fields.MaxValue);
        }
        
        [Test]
        public void SetProperty()
        {
            var propertyInfo = typeof(TestFields).GetProperty("MaxValueProperty");
            var propertySetter = (RefAction<TestFields, int>)Delegate.CreateDelegate(typeof(RefAction<TestFields, int>), propertyInfo.GetSetMethod());

            var fields = new TestFields { MaxValue = 1234 };
            
            propertySetter(ref fields, 5678);
            Console.WriteLine(fields.MaxValue);
        }

    }
}