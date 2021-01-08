using System.Collections.Generic;
using Friflo.Json.Burst;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.Examples
{
    public class Serializer
    {
        static Buddy CreateBuddy() {
            Buddy buddy = new Buddy();
            buddy.firstName = "John";
            buddy.age = 24;
            buddy.hobbies.Add(new Hobby{ name = "Gaming"});
            buddy.hobbies.Add(new Hobby{ name = "STAR WARS"});
            return buddy;
        }
        
        public class Buddy {
            public  string       firstName;
            public  int          age;
            public  List<Hobby>  hobbies = new List<Hobby>();
        }
    
        public struct Hobby {
            public string   name;
        }

        [Test]
        public void WriteJson() {
            Buddy buddy = CreateBuddy();
            var s = new JsonSerializer();
            s.InitSerializer();
            
            s.ObjectStart();
                s.MemberString  ("firstName",   buddy.firstName);
                s.MemberLong    ("age",         buddy.age);
                s.MemberArrayKey("hobbies");
                s.ArrayStart();
                for (int n = 0; n < buddy.hobbies.Count; n++) {
                    s.ObjectStart();
                    s.MemberString  ("name", buddy.hobbies[n].name);
                    s.ObjectEnd();
                }
                s.ArrayEnd();
            s.ObjectEnd();

            var expect = @"{""firstName"":""John"",""age"":24,""hobbies"":[{""name"":""Gaming""},{""name"":""STAR WARS""}]}";
            AreEqual(expect, s.dst.ToString());
        }
    }
}
