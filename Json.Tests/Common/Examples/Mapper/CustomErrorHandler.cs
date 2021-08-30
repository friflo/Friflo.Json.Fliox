using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;
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
            using (var typeStore    = new TypeStore())
            using (var read         = new ObjectReader(typeStore, new CustomErrorHandler()))
            {
                var e = Throws<Exception>(() => read.ReadObject("invalid", typeof(string)));
                AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: i path: '(root)' at position: 1", e.Message);
            }
        }
    }
    
}

