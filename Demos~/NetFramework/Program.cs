using System;
using Friflo.Json.Fliox;

class TestClass
{
    public int val;
}

internal  static class  Program {
    
    public static void Main(string[] args) {
        var test = new TestClass { val = 42 };
        var json = JsonSerializer.Serialize(test);
        Console.WriteLine($"JSON:  {json}");
    }
}