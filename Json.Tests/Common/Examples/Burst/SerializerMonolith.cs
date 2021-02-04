using System.Collections.Generic;
using Friflo.Json.Burst;
using NUnit.Framework;
using static NUnit.Framework.Assert;
// ReSharper disable InconsistentNaming
#pragma warning disable 618

namespace Friflo.Json.Tests.Common.Examples.Burst
{
    public class SerializerMonolith
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

        /// <summary>
        /// Demonstrating an anti pattern having multiple nested loops is not recommended.
        ///
        /// Overall fewer lines of code than <see cref="Serializer"/> but lacks readability and is harder to maintain.
        /// The sample was introduced to show the fact which may happen when evolving a JSON reader over time.   
        /// </summary>
        [Test]
        public void WriteJson() {
            Buddy buddy = CreateBuddy();
            var s = new JsonSerializer();
            s.InitSerializer();
            try {
                s.ObjectStart();
                    s.MemberStr ("firstName",   buddy.firstName);
                    s.MemberLng ("age",         buddy.age);
                    s.MemberArrayStart ("hobbies");
                    for (int n = 0; n < buddy.hobbies.Count; n++) {
                        s.ObjectStart();
                        s.MemberStr ("name", buddy.hobbies[n].name);
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
            }
        }
    }
}
