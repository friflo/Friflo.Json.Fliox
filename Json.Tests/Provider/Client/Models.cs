using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// ReSharper disable All
namespace Friflo.Json.Tests.Provider.Client
{
    // Note: Main property of all model classes
    // They are all POCO's aka Plain Old Class Objects. See https://en.wikipedia.org/wiki/Plain_old_CLR_object
    // As a result integration of these classes in other modules or libraries is comparatively easy.

    // ---------------------------------- entity models ----------------------------------
    public class TestMutate {
        [Key]       public  string          id { get; set; }
                    public  int             val1;
                    public  int             val2;
                    public  string          str;
    }
    
    public class TestOps {
        [Key]       public  string              id { get; set; }
    }
    
    public class TestQuantify {
        [Key]       public  string              id { get; set; }
                    public  int[]               intArray;
                    public  List<int>           intList;
                    public  TestObject[]        objectArray;
                    public  List<TestObject>    objectList;
    }
    
    public class TestObject
    {
                    public  int                 int32;
                    public  string              str;
    }
    
    public class CompareScalar {
        [Key]       public  string              id { get; set; }
                    public  int?                int32;
                    public  string              str;
                    public  bool?               boolean;
                    public  TestObject          obj;
    }
    
    public class TestString {
        [Key]       public  string              id { get; set; }
                    public  string              str;
    }
    
    public enum TestEnum {
        e1 = 101,
        e2 = 102
    }
    
    public class TestEnumEntity {
        [Key]       public  string          id { get; set; }
                    public  TestEnum        enumVal;
                    public  TestEnum?       enumValNull;
    }
    
    public class CursorEntity {
        [Key]       public  string          id { get; set; }
                    public  int             value;
    }
    
    public class TestIntKeyEntity {
        [Key]       public  int             id { get; set; }
                    public  string          value;
    }
    
    public class TestGuidKeyEntity {
        [Key]       public  Guid            id { get; set; }
                    public  string          value;
    }
}