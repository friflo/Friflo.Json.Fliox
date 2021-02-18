using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
    using Str128 = Unity.Collections.FixedString128;
#else
    using Str32 = System.String;
    using Str128 = System.String;
#endif

namespace Friflo.Json.Tests.Common.Examples.Burst
{
    public class ParserBurst
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
            using (var parser = new Local<JsonParser>())
            using (var buddy = new Local<Buddy>())
            {
                ref var p = ref parser.instance;
                ref var b = ref buddy.instance;
                b.hobbies = new ValueList<Hobby>(0, AllocType.Persistent);
                p.InitParser(json);
                p.NextEvent(); // ObjectStart
                ReadBuddy(ref p, in k, ref b);

                if (p.error.ErrSet)
                    Fail(p.error.msg.ToString());
                AreEqual(JsonEvent.EOF, p.NextEvent()); // Important to ensure absence of application errors
                AreEqual("John",        b.firstName);
                AreEqual(24,            b.age);
                AreEqual(2,             b.hobbies.Count);
                AreEqual("Gaming",      b.hobbies.array[0].name);
                AreEqual("STAR WARS",   b.hobbies.array[1].name);
            }
        }
        
        private static void ReadBuddy(ref JsonParser p, in Keys k, ref Buddy buddy) {
            var i = p.GetObjectIterator();
            while (p.NextObjectMember(ref i)) {
                if      (p.UseMemberStr (ref i, in k.firstName))    { buddy.firstName = p.value.ToString(); }
                else if (p.UseMemberNum (ref i, in k.age))          { buddy.age = p.ValueAsInt(out _); }
                else if (p.UseMemberArr (ref i, in k.hobbies))      { ReadHobbyList(ref p, in k, ref buddy.hobbies); }
            }
        }
        
        private static void ReadHobbyList(ref JsonParser p, in Keys k, ref ValueList<Hobby> hobbyList) {
            var i = p.GetArrayIterator();
            while (i.NextArrayElement(ref p)) {
                if (p.UseElementObj(ref i)) {        
                    var hobby = new Hobby();
                    ReadHobby(ref p, in k, ref hobby);
                    hobbyList.Add(hobby);
                }
            }
        }
        
        private static void ReadHobby(ref JsonParser p, in Keys k, ref Hobby hobby) {
            var i = p.GetObjectIterator();
            while (p.NextObjectMember(ref i)) {
                if (p.UseMemberStr(ref i, in k.name))  { hobby.name = p.value.ToStr128(); }
            }
        }
    }
}