using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

namespace Friflo.Json.Tests.Common.Examples
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
            public  Str32               firstName;
            public  int                 age;
            public  ValueList<Hobby>    hobbies;

            public void Dispose() {
                hobbies.Dispose();
            }
        }
    
        public struct Hobby {
            public Str32   name;
        }

        [Test]
        public void WriteJson() {
            Buddy buddy = CreateBuddy();
            var s = new JsonSerializer();
            s.InitSerializer();
            try {
                s.ObjectStart();
                    s.MemberString  ("firstName",   buddy.firstName);
                    s.MemberLong    ("age",         buddy.age);
                    s.MemberArrayStart("hobbies");
                    for (int n = 0; n < buddy.hobbies.Length; n++) {
                        s.ObjectStart();
                        s.MemberString  ("name", buddy.hobbies.array[n].name);
                        s.ObjectEnd();
                    }
                    s.ArrayEnd();
                s.ObjectEnd();

                var expect = @"{""firstName"":""John"",""age"":24,""hobbies"":[{""name"":""Gaming""},{""name"":""STAR WARS""}]}";
                AreEqual(expect, s.dst.ToString());
            }
            finally {
                // only required for Unity/JSON_BURST
                s.Dispose();
                buddy.Dispose();
            }
        }
    }
}
