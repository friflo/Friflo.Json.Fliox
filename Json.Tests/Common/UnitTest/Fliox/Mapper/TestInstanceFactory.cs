// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;
#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    public class TestInstanceFactory
    {
        // --------------- interface
        
        // exception: missing [InstanceType] or [PolymorphType] attribute
        interface IVehicle { }
        
        // exception: missing [InstanceType] or [PolymorphType] attribute
        abstract class Abstract { }
        
        // exception: type is null
        [InstanceType(null)]
        interface ITestInstanceNull { }
        
        // exception: Book does not extend ITestIncompatibleInstance
        [InstanceType(typeof(Book))]
        interface ITestIncompatibleInstance { }

        // --- IBook
        [InstanceType(typeof(Book))]
        interface IBook { }

        class Book : IBook {
            public int int32;
        }



        [Test]
        public void TestInterface() {
            var json = "{\"int32\":123}";
            using (var typeStore = new TypeStore(new StoreConfig()))
            using (var reader = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer = new ObjectWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<IBook>(json);
                AreEqual(123, ((Book)result).int32);
                
                var jsonResult = writer.Write(result);
                AreEqual(json, jsonResult);
                
                var e = Throws<InvalidOperationException>(() => reader.Read<IVehicle>("{}"));
                AreEqual("type requires concrete types by [InstanceType()] or [PolymorphType()] on: IVehicle", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<Abstract>("{}"));
                AreEqual("type requires concrete types by [InstanceType()] or [PolymorphType()] on: Abstract", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<ITestIncompatibleInstance>("{}"));
                AreEqual("[InstanceType(Book)] type must extend annotated type: ITestIncompatibleInstance", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<ITestInstanceNull>("{}"));
                AreEqual("[InstanceType(null)] type must not be null on: ITestInstanceNull", e.Message);
            }
        }
        
        // --------------- polymorphic interface
        // exception: type is null
        [PolymorphType(null)]
        abstract class TestPolymorphNull { }
        
        // exception: Book does not extend ITestIncompatibleInstance
        [PolymorphType(typeof(Book))]
        abstract class TestIncompatiblePolymorph { }
        
        // exception - test missing [Discriminator]
        [PolymorphType(typeof(TestNoDiscriminator))]
        abstract class TestNoDiscriminator { }
        
        // exception - missing [PolymorphType]
        [Discriminator ("discriminator")]
        abstract class TestNoPolymorph { }
        
        
        [Discriminator("animalType")]
        [PolymorphType(typeof(Lion), "lion")]
        interface IAnimal {
        }

        class Lion : IAnimal {
            public int int32;
        }
        
       
        [Test]
        public void TestPolymorphic() {
            var json = "{\"animalType\":\"lion\",\"int32\":123}";
            using (var typeStore = new TypeStore(new StoreConfig()))
            using (var reader = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer = new ObjectWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<IAnimal>(json);
                AreEqual(123, ((Lion)result).int32);
                
                var jsonResult = writer.Write(result);
                AreEqual(json, jsonResult);
                
                reader.Read<IAnimal>("{\"animalType\":\"Tiger\"}");
                StringAssert.Contains("No [PolymorphType] type declared for discriminant: 'Tiger' on type: IAnimal", reader.Error.msg.AsString());
                
                reader.Read<IAnimal>("{}");
                StringAssert.Contains("Expect discriminator 'animalType': '...' as first JSON member for type: IAnimal", reader.Error.msg.AsString());
                
                var e = Throws<InvalidOperationException>(() => reader.Read<TestIncompatiblePolymorph>("{}"));
                AreEqual("[PolymorphType(Book)] type must extend annotated type: TestIncompatiblePolymorph", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<TestPolymorphNull>("{}"));
                AreEqual("[PolymorphType(null)] type must not be null on: TestPolymorphNull", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<TestNoDiscriminator>("{}"));
                AreEqual("specified [PolymorphType] attribute require [Discriminator] on: TestNoDiscriminator", e.Message);
                
                e = Throws<InvalidOperationException>(() => reader.Read<TestNoPolymorph>("{}"));
                AreEqual("specified [Discriminator] require at least one [PolymorphType] attribute on: TestNoPolymorph", e.Message);
            }
        }
        
        // --------------- polymorphic class
        [Discriminator ("personType")]
        [PolymorphType(typeof(Employee))]
        abstract class Person {
        }

        class Employee : Person {
            public int int32;
        }

       
        [Test]
        public void TestAbstract() {
            var json = "{\"personType\":\"Employee\",\"int32\":123}";
            using (var typeStore = new TypeStore(new StoreConfig()))
            using (var reader = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer = new ObjectWriter(typeStore))
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
        
        [Test]
        public void TestFactoryCollection() {
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
            
            using (var typeStore = new TypeStore(new StoreConfig()))
            using (var reader = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer = new ObjectWriter(typeStore))
            {
                // typeStore.AddInstanceFactory(new TestFactory());
                var result = reader.Read<FactoryCollection>(json);
                
                var jsonResult = writer.Write(result);
                AreEqual(expect, jsonResult);
            }
        }
    }
}