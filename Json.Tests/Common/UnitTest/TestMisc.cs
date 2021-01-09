using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest
{
    public class TestMisc
    {
        [Test]
        public void TestUtf8() {
            Bytes src = CommonUtils.FromFile ("assets/EuroSign.txt");
            String str = src.ToString();
            AreEqual("€", str);

            Bytes dst = new Bytes(0);
            dst.FromString("€");
            IsTrue(src.IsEqualBytes(dst));
            dst.Dispose();
            src.Dispose();
        }
        
        [Test]
        public void TestStringIsEqual() {
            Bytes bytes = new Bytes("€");
            AreEqual(3, bytes.Len); // UTF-8 length of € is 3
            String eur = "€";
            AreEqual(eur, bytes.ToString());
            IsTrue(bytes.IsEqualString(eur));
            bytes.Dispose();
            //
            Bytes xyz = new Bytes("xyz");
            String abc = "abc";
            AreEqual(abc.Length, xyz.Len); // Ensure both have same UTF-8 length (both ASCII)
            AreNotEqual(abc, xyz.ToString());
            IsFalse(xyz.IsEqualString(abc));
            xyz.Dispose();          
        }
        
        [Test]
        public void TestUtf8Compare() {
            using (var empty        = new Bytes(""))    //              (0 byte)
            using (var a            = new Bytes("a"))   //  a U+0061    (1 byte)
            using (var ab           = new Bytes("ab"))  //              (2 bytes)
            using (var copyright    = new Bytes("©"))   //  © U+00A9    (2 bytes)  
            using (var euro         = new Bytes("€"))   //  € U+20AC    (3 bytes)
            using (var smiley       = new Bytes("😎"))   //  😎 U+1F60E   (4 bytes)
            {
                IsTrue (Utf8Utils.IsStringEqualUtf8("", empty));
                IsTrue (Utf8Utils.IsStringEqualUtf8("a", a));

                IsFalse(Utf8Utils.IsStringEqualUtf8("a",  ab));
                IsFalse(Utf8Utils.IsStringEqualUtf8("ab", a));

                IsTrue (Utf8Utils.IsStringEqualUtf8("©", copyright));
                IsTrue (Utf8Utils.IsStringEqualUtf8("€", euro));
                IsTrue (Utf8Utils.IsStringEqualUtf8("😎", smiley));
            }
        }

        [Test]
        public void TestBurstStringInterpolation() {
            using (Bytes bytes = new Bytes(128)) {
                int val = 42;
                int val2 = 43;
                char a = 'a';
                bytes.AppendStr32($"With Bytes {val} {val2} {a}");
                AreEqual("With Bytes 42 43 a", $"{bytes.ToStr32()}");

                var withValues = bytes.ToStr32();
                String32 str32 = new String32("World");
                String128 err = new String128($"Hello {str32.value} {withValues}");
                AreEqual("Hello World With Bytes 42 43 a", err.value);
            }
        }
    }
    
    struct Struct
    {
        public int val;
    }
    
    public class TestStructBehavior
    {
        [Test]
        public void TestStructAssignment() {
            Struct var1 = new Struct();
            Struct var2 = var1; // copy as value;
            ref Struct ref1 = ref var1; 
            var1.val = 11;
            AreEqual(0, var2.val); // copy still unchanged
            AreEqual(11, ref1.val); // reference reflect changes
            
            modifyParam(var1);  // method parameter is copied as value, original value stay unchanged
            AreEqual(11, ref1.val);
            
            modifyRefParam(ref var1);
            AreEqual(12, ref1.val); // method parameter is given as reference value, original value is changed
        }

        // ReSharper disable once UnusedParameter.Local
        private void modifyParam(Struct param) {
            param.val = 12;
        }
        
        private void modifyRefParam(ref Struct param) {
            param.val = 12;
        }
        
        // in parameter is passed as reference (ref readonly) - it must not be changed
        // using in parameter degrade performance:
        // [c# 7.2 - Why would one ever use the "in" parameter modifier in C#? - Stack Overflow] https://stackoverflow.com/questions/52820372/why-would-one-ever-use-the-in-parameter-modifier-in-c
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void passByReadOnlyRef(in Struct param) {
            // param.val = 12;  // error CS8332: Cannot assign to a member of variable 'in Struct' because it is a readonly variable
        }
    }
}