using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Tests.Common.Examples.Burst
{
    public class Serializer : LeakTestsFixture
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

        /// <summary>
        /// The following JSON writer is split into multiple Write...() methods to apply the principles mentioned in <see cref="Parser"/>
        /// A weak counterpart example is shown at <see cref="SerializerMonolith"/> doing exactly the same processing. 
        /// </summary>
        [Test]
        public void WriteJson() {
            Buddy buddy = CreateBuddy();
            using (var serial = new Local<JsonSerializer>())
            {
                ref var s = ref serial.value;
                s.InitSerializer();
                WriteBuddy(ref s, buddy);

                var expect = @"{""firstName"":""John"",""age"":24,""hobbies"":[{""name"":""Gaming""},{""name"":""STAR WARS""}]}";
                AreEqual(expect, s.json.ToString());
            }
        }

        private static void WriteBuddy(ref JsonSerializer s, Buddy buddy) {
            s.ObjectStart();
            s.MemberStr ("firstName",   buddy.firstName);
            s.MemberLng ("age",         buddy.age);
            s.MemberArrayStart ("hobbies", true);
            for (int n = 0; n < buddy.hobbies.Count; n++) 
                WriteHobby(ref s, buddy.hobbies[n]);
            s.ArrayEnd();
            s.ObjectEnd();
        }
        
        private static void WriteHobby(ref JsonSerializer s, Hobby buddy) {
            s.ObjectStart();
            s.MemberStr ("name", buddy.name);
            s.ObjectEnd();
        }
    }
}
