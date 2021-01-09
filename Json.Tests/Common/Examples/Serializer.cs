using System.Collections.Generic;
using Friflo.Json.Burst;
using NUnit.Framework;
using static NUnit.Framework.Assert;
#pragma warning disable 618

// ReSharper disable InconsistentNaming

namespace Friflo.Json.Tests.Common.Examples
{
    public class Serializer
    {
        static Buddy CreateBuddy() {
            Buddy buddy = new Buddy();
            buddy.firstName = "John";
            buddy.age = 24;
            buddy.hobbies = new List<Hobby>();
            buddy.hobbies.Add(new Hobby{ name = "Gaming"});
            buddy.hobbies.Add(new Hobby{ name = "STAR WARS"});
            return buddy;
        }
        
        public class Buddy {
            public  string              firstName;
            public  int                 age;
            public  List<Hobby>     hobbies;
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
            }
        }

        private static void WriteBuddy(ref JsonSerializer s, ref Buddy buddy) {
            s.ObjectStart();
            s.MemberString  ("firstName",   buddy.firstName);
            s.MemberLong    ("age",         buddy.age);
            s.MemberArrayStart("hobbies");
            for (int n = 0; n < buddy.hobbies.Count; n++) 
                WriteHobby(ref s, buddy.hobbies[n]);
            s.ArrayEnd();
            s.ObjectEnd();
        }
        
        private static void WriteHobby(ref JsonSerializer s, Hobby buddy) {
            s.ObjectStart();
            s.MemberString("name", buddy.name);
            s.ObjectEnd();
        }
    }
}
