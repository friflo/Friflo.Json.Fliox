using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;
// ReSharper disable InconsistentNaming
#pragma warning disable 618

namespace Friflo.Json.Tests.Common.Examples.Burst
{
    public class Parser : LeakTestsFixture
    {
        // Note: new properties can be added to the JSON anywhere without changing compatibility
        static readonly string jsonString = @"
{
    ""firstName"":  ""John"",
    ""age"":        24,
    ""hobbies"":    [
        {""name"":  ""Gaming"" },
        {""name"":  ""STAR WARS""}
    ],
    ""unknownMember"": { ""anotherUnknown"": 42}
}";
        public class Buddy {
            public  string       firstName;
            public  int          age;
            public  List<Hobby>  hobbies = new List<Hobby>();
        }
    
        public struct Hobby {
            public string   name;
        }

        /// <summary>
        /// The following JSON reader is split into multiple Read...() methods each having only one while loop to support:
        /// - Read...() methods can be reused enabling the DRY principle
        /// - Read...() methods can be unit tested
        /// - enhance readability
        /// - enhance maintainability
        /// - enables the possibility to create readable code via a code generator
        ///
        /// A weak example is shown at <see cref="ParserMonolith"/> doing exactly the same processing. 
        /// </summary>
        [Test]
        public void ReadJson() {
            Buddy buddy = new Buddy();
            
            using (var json = new Bytes(jsonString))
            using (var parser = new Local<JsonParser>())
            {
                ref var p = ref parser.instance;
                p.InitParser(json);
                p.NextEvent(); // ObjectStart
                p.IsRootObject(out JObj i);
                ReadBuddy(ref p, ref i, ref buddy);

                if (p.error.ErrSet)
                    Fail(p.error.msg.ToString());
                AreEqual(JsonEvent.EOF, p.NextEvent()); // Important to ensure absence of application errors
                AreEqual("John",        buddy.firstName);
                AreEqual(24,            buddy.age);
                AreEqual(2,             buddy.hobbies.Count);
                AreEqual("Gaming",      buddy.hobbies[0].name);
                AreEqual("STAR WARS",   buddy.hobbies[1].name);
            }
        }
        
        private static void ReadBuddy(ref JsonParser p, ref JObj i, ref Buddy buddy) {
            while (i.NextObjectMember(ref p)) {
                if      (i.UseMemberStr (ref p, "firstName"))               { buddy.firstName = p.value.ToString(); }
                else if (i.UseMemberNum (ref p, "age"))                     { buddy.age = p.ValueAsInt(out _); }
                else if (i.UseMemberArr (ref p, "hobbies", out JArr arr))   { ReadHobbyList(ref p, ref arr, ref buddy.hobbies); }
            }
        }
        
        private static void ReadHobbyList(ref JsonParser p, ref JArr arr, ref List<Hobby> hobbyList) {
            while (arr.NextArrayElement(ref p)) {
                if (arr.UseElementObj(ref p, out JObj obj)) {
                    var hobby = new Hobby();
                    ReadHobby(ref p, ref obj, ref hobby);
                    hobbyList.Add(hobby);
                }
            }
        }
        
        private static void ReadHobby(ref JsonParser p, ref JObj i, ref Hobby hobby) {
            while (i.NextObjectMember(ref p)) {
                if (i.UseMemberStr (ref p, "name"))  { hobby.name = p.value.ToString(); }
            }
        }
    }
}