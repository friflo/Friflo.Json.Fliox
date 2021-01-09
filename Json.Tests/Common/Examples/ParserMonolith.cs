using System.Collections.Generic;
using Friflo.Json.Burst;
using NUnit.Framework;

using static NUnit.Framework.Assert;
// ReSharper disable InconsistentNaming
#pragma warning disable 618

namespace Friflo.Json.Tests.Common.Examples
{
    public class ParserMonolith
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
        /// Demonstrating an anti pattern having multiple nested while loops is not recommended.
        ///
        /// Overall fewer lines of code than <see cref="Parser"/> but lacks readability and is harder to maintain.
        /// The sample was introduced to show the fact which may happen when evolving a JSON reader over time.   
        /// </summary>
        [Test]
        public void ReadJson() {
            Buddy buddy = new Buddy();
            
            JsonParser p = new JsonParser();
            Bytes json = new Bytes(jsonString);
            try {
                p.InitParser(json);
                p.NextEvent(); // ObjectStart
                while (p.NextObjectMember()) {
                    if      (p.UseMemberStr("firstName"))   { buddy.firstName = p.value.ToString(); }
                    else if (p.UseMemberNum("age"))         { buddy.age = p.ValueAsInt(out _); }
                    else if (p.UseMemberArr("hobbies")) {
                        while (p.NextArrayElement()) {
                            if (p.UseElementObj()) {
                                var hobby = new Hobby();
                                while (p.NextObjectMember()) {
                                    if (p.UseMemberStr("name")) { hobby.name = p.value.ToString(); }
                                }
                                buddy.hobbies.Add(hobby);
                            }
                        }
                    }
                }
                AreEqual(JsonEvent.EOF, p.NextEvent());
                if (p.error.ErrSet)
                    Fail(p.error.msg.ToString());
                AreEqual("John",        buddy.firstName);
                AreEqual(24,            buddy.age);
                AreEqual("Gaming",      buddy.hobbies[0].name);
                AreEqual("STAR WARS",   buddy.hobbies[1].name);
            }
            finally {
                // only required for Unity/JSON_BURST
                json.Dispose(); 
                p.Dispose();
            }
        }
    }
}