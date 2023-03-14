
using System;

namespace SchemaValidation
{
    public class Program
    {
        public static void Main(string[] args) {
            TestValidation.TestValidatePrimitives();
            TestValidation.TestValidateArray();
            TestValidation.TestValidateOptionalFields();
            TestValidation.TestValidateRequiredFields();
            TestValidation.TestValidatePolymorphType();
            
            TestValidation.GenerateSchemaModels();
            
            Console.WriteLine("tests successful");
        }
    }
}
