using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Schema;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.Examples.Schema
{
    class Person
    {
                    public  int     age;
        [Required]  public  string  name;
    }
    
    public static class SchemaValidation
    {
        [Test]
        public static void Run() {
            var json = "{\"age\":42,\"name\":\"Peter\"}";
            var success = JsonValidator.Validate(json, typeof(Person), out var error);
            Assert.IsTrue(success);
        }
    }
}