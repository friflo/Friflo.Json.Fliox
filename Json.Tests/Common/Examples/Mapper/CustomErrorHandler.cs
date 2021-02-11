using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.Examples.Mapper
{
    public class CustomErrorHandler : IErrorHandler {
        
        public void HandleError(int pos, ref Bytes message) {
            throw new Exception(message.ToString());
        }
        
        [Test]
        public void Run() {

            using (TypeStore typeStore = new TypeStore())
            using (JsonReader read = new JsonReader(typeStore, new CustomErrorHandler()))
            using (var invalid = new Bytes("invalid"))
            {
                var e = Throws<Exception>(() => read.ReadObject(invalid, typeof(string)));
                AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: i path: '(root)' at position: 1", e.Message);
            }
        }
    }
    
}

