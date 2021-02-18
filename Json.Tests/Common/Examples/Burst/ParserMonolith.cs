using System.Collections.Generic;
using Friflo.Json.Burst;
using NUnit.Framework;
using static NUnit.Framework.Assert;
// ReSharper disable InconsistentNaming
#pragma warning disable 618

namespace Friflo.Json.Tests.Common.Examples.Burst
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
            using (var parser = new Local<JsonParser>())
            using (var json = new Bytes(jsonString))
            {
                ref var p = ref parser.instance;
                p.InitParser(json);
                p.NextEvent(); // ObjectStart
                var i1 = p.GetObjectIterator();
                while (p.NextObjectMember(ref i1)) {
                    if      (p.UseMemberStr (ref i1, "firstName"))   { buddy.firstName = p.value.ToString(); }
                    else if (p.UseMemberNum (ref i1, "age"))         { buddy.age = p.ValueAsInt(out _); }
                    else if (p.UseMemberArr (ref i1, "hobbies")) {
                        var i2 = p.GetArrayIterator();
                        while (i2.NextArrayElement(ref p)) {
                            if (p.UseElementObj(ref i2)) {
                                var hobby = new Hobby();
                                var i3 = p.GetObjectIterator();
                                while (p.NextObjectMember(ref i3)) {
                                    if (p.UseMemberStr (ref i3, "name")) { hobby.name = p.value.ToString(); }
                                }
                                buddy.hobbies.Add(hobby);
                            }
                        }
                    }
                }
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
    }
}