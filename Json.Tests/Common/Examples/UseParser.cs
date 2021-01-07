using System.Collections.Generic;
using Friflo.Json.Burst;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.Examples
{
    public class UseParser
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
        /// - good readability
        /// - good maintainability
        /// - unit testing
        /// - enables the possibility to create readable code via a code generator
        ///
        /// A weak example is shown at <see cref="UseParserMonolith"/> doing exactly the same processing. 
        /// </summary>
        [Test]
        public void ReadJson() {
            Buddy buddy = new Buddy();
            
            JsonParser p = new JsonParser();
            Bytes json = new Bytes(jsonString);
            try {
                p.InitParser(json);

                p.NextEvent();
                ReadBuddy(ref p, ref buddy);

                AreEqual(JsonEvent.EOF, p.NextEvent());
                if (p.error.ErrSet)
                    Fail(p.error.msg.ToString());
                AreEqual("John", buddy.firstName);
                AreEqual(24, buddy.age);
                AreEqual("Gaming", buddy.hobbies[0].name);
                AreEqual("STAR WARS", buddy.hobbies[1].name);
            }
            finally {
                // only required for Unity/JSON_BURST
                json.Dispose();
                p.Dispose();
            }
        }
        
        private static void ReadBuddy(ref JsonParser p, ref Buddy buddy) {
            while (p.NextObjectMember()) {
                if      (p.IsMemberStr ("firstName"))    { buddy.firstName = p.value.ToString(); }
                else if (p.IsMemberNum ("age"))          { buddy.age = p.ValueAsInt(out _); }
                else if (p.IsMemberArr ("hobbies"))      { ReadHobbyList(ref p, ref buddy.hobbies); }
            }
        }
        
        private static void ReadHobbyList(ref JsonParser p, ref List<Hobby> hobbyList) {
            var arr = new ReadArray();
            while (arr.NextElement(ref p)) {
                if (arr.UseObj(ref p)) {        
                    var hobby = new Hobby();
                    ReadHobby(ref p, ref hobby);                // descend array element
                    hobbyList.Add(hobby);
                }
            }
        }
        
        private static void ReadHobby(ref JsonParser p, ref Hobby hobby) {
            while (p.NextObjectMember()) {
                if (p.IsMemberStr("name"))  { hobby.name = p.value.ToString(); }
            }
        }
    }
}