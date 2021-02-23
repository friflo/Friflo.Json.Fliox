using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestInstanceFactory
    {
        // ------ interface support
        
        // missing: [JsonType (InstanceFactory = typeof(<factory class>))]
        interface IInvalid { }
        
        // incompatible InstanceFactory<> 
        [JsonType (InstanceFactory = typeof(AnimalFactory))]
        interface IBookWithInvalidFactory {
        }
        
        // missing: [JsonType (InstanceFactory = typeof(<factory class>))]
        abstract class Abstract { }
        
        // --- IBook
        [JsonType (InstanceFactory = typeof(BookFactory))]
        interface IBook { }

        class Book : IBook {
            public int int32;
        }

        class BookFactory : InstanceFactory<IBook>
        {
            public override IBook CreateInstance(string name) {
                return new Book();
            }
        }
        
        // --- IVehicle with InstanceFactory returning null 
        [JsonType (InstanceFactory = typeof(VehicleFactory))]
        interface IVehicle { }
        
        class VehicleFactory : InstanceFactory<IVehicle>
        {
            public override IVehicle CreateInstance(string name) {
                return null;
            }
        }

        [Test]  public void  TestInterfaceReflect()   { TestInterface(TypeAccess.Reflection); }
        [Test]  public void  TestInterfaceIL()        { TestInterface(TypeAccess.IL); }
        
        private void TestInterface(TypeAccess typeAccess) {
            var json = "{\"int32\":123}";
            using (var typeStore = new TypeStore(null, new StoreConfig(typeAccess)))
            using (var reader = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var writer = new JsonWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<IBook>(json);
                AreEqual(123, ((Book)result).int32);
                
                var jsonResult = writer.Write(result);
                AreEqual(json, jsonResult);

                reader.Read<IVehicle>("{}");
                StringAssert.Contains("No instance created in InstanceFactory: VehicleFactory path: '(root)'", reader.Error.msg.ToString());

                var e = Throws<InvalidOperationException>(() => reader.Read<IInvalid>("{}"));
                AreEqual("require attribute [JsonType(InstanceFactory = typeof(<factory class>))] on: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+IInvalid", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<Abstract>("{}"));
                AreEqual("require attribute [JsonType(InstanceFactory = typeof(<factory class>))] on: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+Abstract", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<IBookWithInvalidFactory>("{}"));
                AreEqual("require compatible InstanceFactory<> on: Friflo.Json.Tests.Common.UnitTest.Mapper.TestInstanceFactory+IBookWithInvalidFactory", e.Message);
            }
        }
        
        // ------ polymorphic interface support
        [JsonType (InstanceFactory = typeof(AnimalFactory))]
        interface IAnimal {
        }

        class Lion : IAnimal {
            public int int32;
        }

        class AnimalFactory : InstanceFactory<IAnimal>
        {
            public override string Discriminator => "animalType";

            public override IAnimal CreateInstance(string name) {
                if (name == "Lion")
                    return new Lion();
                return null;
            }
        }
        
        [Test]  public void  TestPolymorphicReflect()   { TestPolymorphic(TypeAccess.Reflection); }
        [Test]  public void  TestPolymorphicIL()        { TestPolymorphic(TypeAccess.IL); }
        
        private void TestPolymorphic(TypeAccess typeAccess) {
            var json = "{\"animalType\":\"Lion\",\"int32\":123}";
            using (var typeStore = new TypeStore(null, new StoreConfig(typeAccess)))
            using (var reader = new JsonReader(typeStore, JsonReader.NoThrow))
            using (var writer = new JsonWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<IAnimal>(json);
                AreEqual(123, ((Lion)result).int32);
                
                var jsonResult = writer.Write(result);
                AreEqual(json, jsonResult);
                
                reader.Read<IAnimal>("{\"animalType\":\"Tiger\"}");
                StringAssert.Contains("No instance created with name: 'Tiger' in InstanceFactory: AnimalFactory path: 'animalType'", reader.Error.msg.ToString());
                
                reader.Read<IAnimal>("{}");
                StringAssert.Contains("Expect discriminator \"animalType\": \"...\" as first JSON member when using InstanceFactory: AnimalFactory path: '(root)'", reader.Error.msg.ToString());
            }
        }
        
        // ------ polymorphic class support
        [JsonType (InstanceFactory = typeof(PersonFactory))]
        abstract class Person {
        }

        class Employee : Person {
            public int int32;
        }

        class PersonFactory : InstanceFactory<Person>
        {
            public override string Discriminator => "personType";

            public override Person CreateInstance(string name) {
                if (name == "Employee")
                    return new Employee();
                return null;
            }
        }
        
        [Test]  public void  TestAbstractReflect()   { TestAbstract(TypeAccess.Reflection); }
        [Test]  public void  TestAbstractIL()        { TestAbstract(TypeAccess.IL); }
        
        private void TestAbstract(TypeAccess typeAccess) {
            var json = "{\"personType\":\"Employee\",\"int32\":123}";
            using (var typeStore = new TypeStore(null, new StoreConfig(typeAccess)))
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
            public List<IBook>      iTest   = new List<IBook>();
            public List<IAnimal>    animals = new List<IAnimal>();
        }
        
        [Test]  public void  TestFactoryCollectionReflect()   { TestFactoryCollection(TypeAccess.Reflection); }
        [Test]  public void  TestFactoryCollectionIL()        { TestFactoryCollection(TypeAccess.IL); }
        
        private void TestFactoryCollection(TypeAccess typeAccess) {
            var json = @"
{
    ""iTest"": [
        {
            ""int32"":123
        }
    ],
    ""animals"": [
        {
            ""animalType"":""Lion"",
            ""int32"":123
        }
    ]
}";
            string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));
            
            using (var typeStore = new TypeStore(null, new StoreConfig(typeAccess)))
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