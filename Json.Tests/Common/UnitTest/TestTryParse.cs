using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest
{


    
    class TestTryParse
    {
        // [c# - How do I pass a ref struct argument to a MethodInfo if the struct has a ReadOnlySpan field - Stack Overflow]
        // https://stackoverflow.com/questions/63126581/how-do-i-pass-a-ref-struct-argument-to-a-methodinfo-if-the-struct-has-a-readonly
        /*
        public readonly ref struct Bob
        {
            public Bob(ReadOnlySpan<byte> myProperty) => MyProperty = myProperty;    

            public ReadOnlySpan<byte> MyProperty { get; }
        }

        delegate byte myDelegate(Bob asd);
        
        public static byte DoSomething(Bob bob) => bob.MyProperty[1]; // return something from the span ¯\_(ツ)_/¯
        
        [Test]
        public void Sample() {
            var bytes = new byte[] {1, 2, 3};
            var span = bytes.AsSpan();
            var bob = new Bob(span);
            
            var method = typeof(TestTryParse).GetMethod("DoSomething");
            var parameter = Expression.Parameter(typeof(Bob), "b");
            var call = Expression.Call(method, parameter);
            var expression = Expression.Lambda<myDelegate>(call, parameter);
            var func = expression.Compile();
            var result = func(bob);
        } */

        [Test]
        public void TryParse() {
            double.TryParse("123", NumberStyles.Float, NumberFormatInfo.InvariantInfo, out double result2);
            // var span = "123".AsSpan();
#if !UNITY_5_3_OR_NEWER
            char[] charBuf = new char[30];
            charBuf[0] = '1';
            charBuf[1] = (char)0;
            
            ReadOnlySpan<char> span = new ReadOnlySpan<char>(charBuf); 
            int.TryParse(span, out int result1);
            double.TryParse(span, out double result3);
#endif
        }

        [Test]
        public void TryParseInvoke() {
            /*
            // static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out double result)
            Type[] types = { typeof(ReadOnlySpan<char>), typeof(NumberStyles), typeof(NumberFormatInfo), typeof(double).MakeByRefType() };
            ParameterModifier[] modifiers = {new ParameterModifier()};
            
            MethodInfo dynMethod = typeof(double).GetMethod(nameof(double.TryParse), BindingFlags.NonPublic | BindingFlags.Static, null, types, null);
            
            
            
            var dblDesult = new double();
            ReadOnlySpan<char> readOnlySpan = "123".AsSpan();
            // object[] parameters = { "123".AsSpan(), NumberStyles.Float, NumberFormatInfo.InvariantInfo, dblDesult };
            // dynMethod.Invoke(null, parameters);
            */
        }
    }
}