using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Tests.Common.Examples.Burst
{
    public class SerializerMonolith : LeakTestsFixture
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
            using (var serial = new Local<JsonSerializer>())
            {
                ref var s = ref serial.value;
                s.InitSerializer();

                s.ObjectStart();
                    s.MemberStr ("firstName",   buddy.firstName);
                    s.MemberLng ("age",         buddy.age);
                    s.MemberArrayStart ("hobbies", true);
                    for (int n = 0; n < buddy.hobbies.Count; n++) {
                        s.ObjectStart();
                        s.MemberStr ("name", buddy.hobbies[n].name);
                        s.ObjectEnd();
                    }
                    s.ArrayEnd();
                s.ObjectEnd();

                var expect = @"{""firstName"":""John"",""age"":24,""hobbies"":[{""name"":""Gaming""},{""name"":""STAR WARS""}]}";
                AreEqual(expect, s.json.AsString());
            }
        }
    }
}
