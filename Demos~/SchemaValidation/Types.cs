#pragma warning disable CS0649

using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;

namespace SchemaValidation
{
    /// class with optional fields
    class OptionalFields
    {
        public  int?    age;
        public  string  name;
        public  Gender? gender;
        public  int[]   intArray;
    }
    
    /// class with required fields
    class RequiredFields
    {
                    public  int     age;
        [Required]  public  string  name;
                    public  Gender  gender;
        [Required]  public  int[]   intArray;
    }
        
    enum Gender
    {
        male,
        female
    }
    
    // --- polymorph class type
    [Discriminator("vehicleType")]
    [PolymorphType(typeof(Car),     "car")]
    [PolymorphType(typeof(Bike),    "bike")]
    class Vehicle { }
        
    class Car : Vehicle
    {
        public int  seatCount;
    }
        
    class Bike : Vehicle
    {
        public bool hasLuggageRack;
    }
}