using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming

// JSON_BURST_TAG
using Str32 = System.String;
using Str128 = System.String;


namespace Friflo.Json.Tests.Common.Examples.Burst
{
    public class SerializerBurst : LeakTestsFixture
    {
        static Buddy CreateBuddy() {
            Buddy buddy;
            buddy.firstName = "John";
            buddy.age = 24;
            buddy.hobbies = new ValueList<Hobby>(2, AllocType.Persistent);
            buddy.hobbies.Add(new Hobby{ name = "Gaming"});
            buddy.hobbies.Add(new Hobby{ name = "STAR WARS"});
            return buddy;
        }
        
        public struct Buddy : IDisposable {
            public  Str128              firstName;
            public  int                 age;
            public  ValueList<Hobby>    hobbies;

            public void Dispose() { // only required for Unity/JSON_BURST
                hobbies.Dispose();
            }
        }
    
        public struct Hobby {
            public Str128   name;
        }

        // Using a struct containing JSON key names enables using them by ref to avoid memcpy
        public struct Keys {
            public Str32    firstName;
            public Str32    age;
            public Str32    hobbies;
            public Str32    name;

            public Keys(Default _) {
                firstName   = "firstName";
                age         = "age";
                hobbies     = "hobbies";
                name        = "name";
            }
        }
        
        [Test]
        public void WriteJson() {
            Keys    k = new Keys(Default.Constructor);
            using (var serial = new Local<Utf8JsonWriter>())
            using (var buddy = new Local<Buddy>(CreateBuddy()))
            {
                ref var s = ref serial.value;
                s.InitSerializer();
                WriteBuddy(ref s, in k, in buddy.value);

                var expect = @"{""firstName"":""John"",""age"":24,""hobbies"":[{""name"":""Gaming""},{""name"":""STAR WARS""}]}";
                AreEqual(expect, s.json.AsString());
            }
        }

        private static void WriteBuddy(ref Utf8JsonWriter s, in Keys k, in Buddy buddy) {
            s.ObjectStart();
            s.MemberStr (k.firstName,   buddy.firstName);
            s.MemberLng (k.age,         buddy.age);
            s.MemberArrayStart(k.hobbies, true);
            for (int n = 0; n < buddy.hobbies.Count; n++) 
                WriteHobby(ref s, in k, in buddy.hobbies.ElementAt(n));
            s.ArrayEnd();
            s.ObjectEnd();
        }
        
        private static void WriteHobby(ref Utf8JsonWriter s, in Keys k, in Hobby buddy) {
            s.ObjectStart();
            s.MemberStr(k.name, buddy.name);
            s.ObjectEnd();
        }
    }
}
