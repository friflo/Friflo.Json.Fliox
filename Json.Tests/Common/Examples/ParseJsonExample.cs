using System;
using Friflo.Json.Burst;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.Examples
{
    public class ParseJsonExample
    {
        [Test]
        public void ParseJson() {
            string jsonString = @"
{
    ""name"": ""John"",
    ""age"":  24
}";
            String name = "";
            int age = 0;
            
            JsonParser p = new JsonParser();
            Bytes json = new Bytes(jsonString); 
            p.InitParser(json);
            if (p.NextEvent() == JsonEvent.ObjectStart) {
                ReadObject read = new ReadObject();
                while (read.NextEvent(ref p)) {
                    if      (read.UseStr(ref p, "name")) {
                        name = p.value.ToString();
                    }
                    else if (read.UseNum(ref p, "age")) {
                        age = p.ValueAsInt(out _);
                    }
                }
            } else {
                Fail("Expect: ObjectStart");
            }

            AreEqual("John",    name.ToString());
            AreEqual(24,        age);
        }
    }
}