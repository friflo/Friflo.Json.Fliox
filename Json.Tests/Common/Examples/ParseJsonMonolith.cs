using System.Collections.Generic;
using Friflo.Json.Burst;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.Examples
{
    public class ParseJsonMonolith
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
        /// Overall fewer lines of code than <see cref="ParseJson"/> but lacks readability and is harder to maintain.
        /// The sample was introduced to show the fact which may happen when evolving a JSON reader over time.   
        /// </summary>
        [Test]
        public void ReadJsonMonolith() {
            Buddy buddy = new Buddy();
            
            JsonParser p = new JsonParser();
            Bytes json = new Bytes(jsonString); 
            p.InitParser(json);
            
            var readRoot = new ReadObject();
            while (readRoot.ContinueObject(ref p)) {                         // descend root object
                var readBase = new ReadObject();
                while (readBase.ContinueObject(ref p)) {                     // iterate root object key/values
                    if (readBase.UseStr(ref p, "firstName")) {
                        buddy.firstName = p.value.ToString();
                    }
                    else if (readBase.UseNum(ref p, "age")) {
                        buddy.age = p.ValueAsInt(out _);
                    }
                    else if (readBase.UseArr(ref p, "hobbies")) {        // descend hobbies array
                        var readHobbies = new ReadArray();
                        while (readHobbies.ContinueArray(ref p)) {           // iterate hobbies array elements
                            if (readHobbies.UseObj(ref p)) {             // descend array element
                                var hobby = new Hobby();
                                var readHobby = new ReadObject();
                                while (readHobby.ContinueObject(ref p)) {     // iterate hobby key/values
                                    if (readHobby.UseStr(ref p, "name")) {
                                        hobby.name = p.value.ToString();
                                    }
                                }

                                buddy.hobbies.Add(hobby);
                            }
                        }
                    }
                }
            }
            if (p.error.ErrSet)
                Fail(p.error.msg.ToString());
            AreEqual("John",        buddy.firstName);
            AreEqual(24,            buddy.age);
            AreEqual("Gaming",      buddy.hobbies[0].name);
            AreEqual("STAR WARS",   buddy.hobbies[1].name);
        }
    }
}