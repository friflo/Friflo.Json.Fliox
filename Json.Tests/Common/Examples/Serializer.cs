using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming

namespace Friflo.Json.Tests.Common.Examples
{
    public class Serializer
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
            public  string              firstName;
            public  int                 age;
            public  ValueList<Hobby>    hobbies;

            public void Dispose() { // only required for Unity/JSON_BURST
                hobbies.Dispose();
            }
        }
    
        public struct Hobby {
            public string   name;
        }

        [Test]
        public void WriteJson() {
            Buddy buddy = CreateBuddy();
            var s = new JsonSerializer();
            s.InitSerializer();
            try {
                WriteBuddy(ref s, ref buddy);

                var expect = @"{""firstName"":""John"",""age"":24,""hobbies"":[{""name"":""Gaming""},{""name"":""STAR WARS""}]}";
                AreEqual(expect, s.dst.ToString());
            }
            finally {
                // only required for Unity/JSON_BURST
                s.Dispose();
                buddy.Dispose();
            }
        }

        private static void WriteBuddy(ref JsonSerializer s, ref Buddy buddy) {
            s.ObjectStart();
            s.MemberString  ("firstName",   buddy.firstName);
            s.MemberLong    ("age",         buddy.age);
            s.MemberArrayStart("hobbies");
            for (int n = 0; n < buddy.hobbies.Length; n++) 
                WriteHobby(ref s, ref buddy.hobbies.ElementAt(n));
            s.ArrayEnd();
            s.ObjectEnd();
        }
        
        private static void WriteHobby(ref JsonSerializer s, ref Hobby buddy) {
            s.ObjectStart();
            s.MemberString("name", buddy.name);
            s.ObjectEnd();
        }
    }
}
