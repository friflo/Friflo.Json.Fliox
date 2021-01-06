using System.Collections.Generic;
using Friflo.Json.Burst;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.Examples
{
    public class ParseJsonExample
    {
        static readonly string jsonString = @"
{
    ""firstName"":  ""John"",
    ""age"":        24,
    ""hobbies"":    [
        {""name"":  ""Gaming"" },
        {""name"":  ""STAR WARS""}
    ]
}";
        
        [Test]
        public void ParseJson() {
            Buddy buddy = new Buddy();
            
            JsonParser p = new JsonParser();
            Bytes json = new Bytes(jsonString); 
            p.InitParser(json);
            p.NextEvent(); // expect JsonEvent.ObjectStart

            var readRoot = new ReadObject();
            while (readRoot.NextEvent(ref p)) {                     // descend to root object 
                if      (readRoot.UseStr(ref p, "firstName")) {
                    buddy.name = p.value.ToString();
                }
                else if (readRoot.UseNum(ref p, "age")) {
                    buddy.age = p.ValueAsInt(out _);
                }
                else if (readRoot.UseArr(ref p, "hobbies")) {       // descend to hobbies array
                    var readHobbies = new ReadArray();
                    while (readHobbies.NextEvent(ref p)) {          // iterate array elements
                        if (readHobbies.UseObj(ref p)) {        
                            var hobby = new Hobby();
                            var readHobby = new ReadObject();
                            while (readHobby.NextEvent(ref p)) {    // descend to hobby object 
                                if (readHobby.UseStr(ref p, "name")) {
                                    hobby.name = p.value.ToString();
                                }
                            }
                            buddy.hobbies.Add(hobby);
                        }
                    }
                }
            }
            AreEqual(JsonEvent.EOF, p.NextEvent());
            if (p.error.ErrSet)
                Fail(p.error.msg.ToString());
            AreEqual("John",        buddy.name);
            AreEqual(24,            buddy.age);
            AreEqual("Gaming",      buddy.hobbies[0].name);
            AreEqual("STAR WARS",   buddy.hobbies[1].name);
        }
    }

    public class Buddy
    {
        public          string       name;
        public          int          age;
        public readonly List<Hobby>  hobbies = new List<Hobby>();
    }
    
    public struct Hobby
    {
        public string   name;
        public int      passion;
    }
}