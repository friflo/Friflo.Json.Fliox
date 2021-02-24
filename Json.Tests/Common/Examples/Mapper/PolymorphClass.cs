using System.Linq;
using Friflo.Json.Mapper;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.Examples.Mapper
{
    public class TestPolymorphClass
    {
        [Discriminator("vehicleType")]
        [Polymorph(typeof(Car),     Discriminant = "car")]
        [Polymorph(typeof(Bike),    Discriminant = "bike")]
        class Vehicle {
        }
        
        class Car : Vehicle {
            public int  seatCount;
        }
        
        class Bike : Vehicle {
            public bool hasLuggageRack;
        }

        
        [Test]
        public void Run() {
            string json = @"
            [
                {
                    ""vehicleType"":    ""car"",
                    ""seatCount"":      4
                },
                {
                    ""vehicleType"":    ""bike"",
                    ""hasLuggageRack"": true
                }
            ]";
            using (var m = new JsonMapper()) {
                var vehicles = m.Read<Vehicle[]>(json);

                var result = m.Write(vehicles);
                string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));

                Assert.AreEqual(expect, result);
            }
        }
    }
}