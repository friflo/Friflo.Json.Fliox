using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
    using Str128 = Unity.Collections.FixedString128;
#else
    using Str32 = System.String;
    using Str128 = System.String;
#endif

namespace Friflo.Json.Tests.Common.Examples.Burst
{
    public class SerializerBurst
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
            using (var serial = new Local<JsonSerializer>())
            using (var buddy = new Local<Buddy>(CreateBuddy()))
            {
                ref var s = ref serial.value;
                s.InitSerializer();
                WriteBuddy(ref s, in k, in buddy.value);

                var expect = @"{""firstName"":""John"",""age"":24,""hobbies"":[{""name"":""Gaming""},{""name"":""STAR WARS""}]}";
                AreEqual(expect, s.json.ToString());
            }
        }

        private static void WriteBuddy(ref JsonSerializer s, in Keys k, in Buddy buddy) {
            s.ObjectStart();
            s.MemberStr (in k.firstName,   in buddy.firstName);
            s.MemberLng (in k.age,         buddy.age);
            s.MemberArrayStart(in k.hobbies, true);
            for (int n = 0; n < buddy.hobbies.Count; n++) 
                WriteHobby(ref s, in k, in buddy.hobbies.ElementAt(n));
            s.ArrayEnd();
            s.ObjectEnd();
        }
        
        private static void WriteHobby(ref JsonSerializer s, in Keys k, in Hobby buddy) {
            s.ObjectStart();
            s.MemberStr(in k.name, in buddy.name);
            s.ObjectEnd();
        }
    }
}
