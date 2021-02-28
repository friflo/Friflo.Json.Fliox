using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;
#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestInstanceFactory
    {
        // --------------- interface
        
        // exception: missing [Fri.Instance] or [Fri.Polymorph] attribute
        interface IVehicle { }
        
        // exception: missing [Fri.Instance] or [Fri.Polymorph] attribute
        abstract class Abstract { }
        
        // exception: type is null
        [Fri.Instance(null)]
        interface ITestInstanceNull { }
        
        // exception: Book does not extend ITestIncompatibleInstance
        [Fri.Instance(typeof(Book))]
        interface ITestIncompatibleInstance { }

        // --- IBook
        [Fri.Instance(typeof(Book))]
        interface IBook { }

        class Book : IBook {
            public int int32;
        }



        [Test]  public void  TestInterfaceReflect()   { TestInterface(TypeAccess.Reflection); }
        [Test]  public void  TestInterfaceIL()        { TestInterface(TypeAccess.IL); }
        
        private void TestInterface(TypeAccess typeAccess) {
            var json = "{\"int32\":123}";
            using (var typeStore = new TypeStore(new StoreConfig(typeAccess)))
            using (var reader = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var writer = new JsonWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<IBook>(json);
                AreEqual(123, ((Book)result).int32);
                
                var jsonResult = writer.Write(result);
                AreEqual(json, jsonResult);
                
                var e = Throws<InvalidOperationException>(() => reader.Read<IVehicle>("{}"));
                AreEqual("type requires instantiatable types by [Fri.Instance()] or [Fri.Polymorph()] on: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+IVehicle", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<Abstract>("{}"));
                AreEqual("type requires instantiatable types by [Fri.Instance()] or [Fri.Polymorph()] on: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+Abstract", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<ITestIncompatibleInstance>("{}"));
                AreEqual("[Fri.Instance(Book)] type must extend annotated type: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+ITestIncompatibleInstance", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<ITestInstanceNull>("{}"));
                AreEqual("[Fri.Instance(null)] type must not be null on: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+ITestInstanceNull", e.Message);
            }
        }
        
        // --------------- polymorphic interface
        // exception: type is null
        [Fri.Polymorph(null)]
        abstract class TestPolymorphNull { }
        
        // exception: Book does not extend ITestIncompatibleInstance
        [Fri.Polymorph(typeof(Book))]
        abstract class TestIncompatiblePolymorph { }
        
        // exception - test missing [Fri.Discriminator]
        [Fri.Polymorph(typeof(TestNoDiscriminator))]
        abstract class TestNoDiscriminator { }
        
        // exception - missing [Fri.Polymorph]
        [Fri.Discriminator ("discriminator")]
        abstract class TestNoPolymorph { }
        
        
        [Fri.Discriminator("animalType")]
        [Fri.Polymorph(typeof(Lion), Discriminant = "lion")]
        interface IAnimal {
        }

        class Lion : IAnimal {
            public int int32;
        }
        
        [Test]  public void  TestPolymorphicReflect()   { TestPolymorphic(TypeAccess.Reflection); }
        [Test]  public void  TestPolymorphicIL()        { TestPolymorphic(TypeAccess.IL); }
        
        private void TestPolymorphic(TypeAccess typeAccess) {
            var json = "{\"animalType\":\"lion\",\"int32\":123}";
            using (var typeStore = new TypeStore(new StoreConfig(typeAccess)))
            using (var reader = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var writer = new JsonWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<IAnimal>(json);
                AreEqual(123, ((Lion)result).int32);
                
                var jsonResult = writer.Write(result);
                AreEqual(json, jsonResult);
                
                reader.Read<IAnimal>("{\"animalType\":\"Tiger\"}");
                StringAssert.Contains("No [Fri.Polymorph] type declared for discriminant: 'Tiger' on type: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+IAnimal", reader.Error.msg.ToString());
                
                reader.Read<IAnimal>("{}");
                StringAssert.Contains("Expect discriminator \"animalType\": \"...\" as first JSON member for type: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+IAnimal", reader.Error.msg.ToString());
                
                var e = Throws<InvalidOperationException>(() => reader.Read<TestIncompatiblePolymorph>("{}"));
                AreEqual("[Fri.Polymorph(Book)] type must extend annotated type: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+TestIncompatiblePolymorph", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<TestPolymorphNull>("{}"));
                AreEqual("[Fri.Polymorph(null)] type must not be null on: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+TestPolymorphNull", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<TestNoDiscriminator>("{}"));
                AreEqual("specified [Fri.Polymorph] attribute require [Fri.Discriminator] on: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+TestNoDiscriminator", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<TestNoPolymorph>("{}"));
                AreEqual("specified [Fri.Discriminator] require at least one [Fri.Polymorph] attribute on: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+TestNoPolymorph", e.Message);
            }
        }
        
        // --------------- polymorphic class
        [Fri.Discriminator ("personType")]
        [Fri.Polymorph(typeof(Employee))]
        abstract class Person {
        }

        class Employee : Person {
            public int int32;
        }

        [Test]  public void  TestAbstractReflect()   { TestAbstract(TypeAccess.Reflection); }
        [Test]  public void  TestAbstractIL()        { TestAbstract(TypeAccess.IL); }
        
        private void TestAbstract(TypeAccess typeAccess) {
            var json = "{\"personType\":\"Employee\",\"int32\":123}";
            using (var typeStore = new TypeStore(new StoreConfig(typeAccess)))
            using (var reader = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var writer = new JsonWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<Person>(json);
                AreEqual(123, ((Employee)result).int32);
                
                var jsonResult = writer.Write(result);
                AreEqual(json, jsonResult);
            }
        }
        
        // ------ factory instances within collection
        
        class FactoryCollection
        {
            public List<IBook>      books   = new List<IBook>();
            public List<IAnimal>    animals = new List<IAnimal>();
        }
        
        [Test]  public void  TestFactoryCollectionReflect()   { TestFactoryCollection(TypeAccess.Reflection); }
        [Test]  public void  TestFactoryCollectionIL()        { TestFactoryCollection(TypeAccess.IL); }
        
        private void TestFactoryCollection(TypeAccess typeAccess) {
            var json = @"
{
    ""books"": [
        {
            ""int32"":123
        }
    ],
    ""animals"": [
        {
            ""animalType"":""lion"",
            ""int32"":123
        }
    ]
}";
            string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));
            
            using (var typeStore = new TypeStore(new StoreConfig(typeAccess)))
            using (var reader = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var writer = new JsonWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<FactoryCollection>(json);
                
                var jsonResult = writer.Write(result);
                AreEqual(expect, jsonResult);
            }
        }
    }
}