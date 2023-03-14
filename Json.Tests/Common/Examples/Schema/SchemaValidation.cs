#pragma warning disable CS0649

using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Schema;
using Friflo.Json.Fliox.Schema.Language;
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
        /// Validate JSON with a Schema
        [Test]
        public static void Run() {
            var json = "{\"age\":42,\"name\":\"Peter\"}";
            var success = JsonValidator.Validate(json, typeof(Person), out var error);
            Assert.IsTrue(success);
        }

        /// Generate types for: C#, GraphQL, HTML, JSON Schema, Kotlin, Markdown and Typescript in folder: ./schema
        [Test]
        public static void GenerateSchemaModels() {
            var schemaModels = SchemaModel.GenerateSchemaModels(typeof(Person));
            foreach (var schemaModel in schemaModels) {
                var folder = $"./schema/{schemaModel.type}";
                schemaModel.WriteFiles(folder);
            }
        }
    }
}