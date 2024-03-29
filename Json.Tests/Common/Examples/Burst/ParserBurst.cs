﻿using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// JSON_BURST_TAG
using Str32 = System.String;
using Str128 = System.String;

namespace Friflo.Json.Tests.Common.Examples.Burst
{
    public class ParserBurst : LeakTestsFixture
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
        public struct Buddy : IDisposable {
            public  Str32               firstName;
            public  int                 age;
            public  ValueList<Hobby>    hobbies;
            
            public void Dispose() { // only required for Unity/JSON_BURST
                hobbies.Dispose();
            }
        }
    
        public struct Hobby {
            public Str128 name;
        }
        
        // Using a struct containing JSON key names enables using them by ref to avoid memcpy
        public struct Keys {
            public Str32    firstName;
            public Str32    age;
            public Str32    hobbies;
            public Str32    name;

            public Keys(Default _) {
                firstName   = "firstName";
                age         = "age";
                hobbies     = "hobbies";
                name        = "name";
            }
        }

        /// <summary>
        /// Exactly the same as <see cref="Parser"/> except is uses a struct containing all JSON key names.
        /// This avoids copying all characters of a FixedString32 key name each time a UseMember...() method is called.
        /// It also enhance source code navigation by finding usage of all reader and writer methods using the same key.
        /// </summary>
        [Test]
        public void ReadJson() {
            Keys    k = new Keys(Default.Constructor);
            using (var json = new Bytes(jsonString))
            using (var parser = new Local<Utf8JsonParser>())
            using (var buddy = new Local<Buddy>())
            {
                ref var p = ref parser.value;
                ref var b = ref buddy.value;
                b.hobbies = new ValueList<Hobby>(0, AllocType.Persistent);
                p.InitParser(json);
                p.ExpectRootObject(out JObj i);
                ReadBuddy(ref p, ref i, in k, ref b);

                if (p.error.ErrSet)
                    Fail(p.error.msg.AsString());
                AreEqual(JsonEvent.EOF, p.NextEvent()); // Important to ensure absence of application errors
                AreEqual("John",        b.firstName);
                AreEqual(24,            b.age);
                AreEqual(2,             b.hobbies.Count);
                AreEqual("Gaming",      b.hobbies.array[0].name);
                AreEqual("STAR WARS",   b.hobbies.array[1].name);
            }
        }
        
        private static void ReadBuddy(ref Utf8JsonParser p, ref JObj i, in Keys k, ref Buddy buddy) {
            while (i.NextObjectMember(ref p)) {
                if      (i.UseMemberStr (ref p, in k.firstName))                { buddy.firstName = p.value.AsString(); }
                else if (i.UseMemberNum (ref p, in k.age))                      { buddy.age = p.ValueAsInt(out _); }
                else if (i.UseMemberArr (ref p, in k.hobbies, out JArr arr))    { ReadHobbyList(ref p, ref arr, in k, ref buddy.hobbies); }
            }
        }
        
        private static void ReadHobbyList(ref Utf8JsonParser p, ref JArr i, in Keys k, ref ValueList<Hobby> hobbyList) {
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementObj(ref p, out JObj obj)) {        
                    var hobby = new Hobby();
                    ReadHobby(ref p, ref obj, in k, ref hobby);
                    hobbyList.Add(hobby);
                }
            }
        }
        
        private static void ReadHobby(ref Utf8JsonParser p, ref JObj i, in Keys k, ref Hobby hobby) {
            while (i.NextObjectMember(ref p)) {
                if (i.UseMemberStr(ref p, in k.name))  { hobby.name = p.value.ToStr128(); }
            }
        }
    }
}