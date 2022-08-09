using System;
using System.Diagnostics;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    // comment following pragma to verify getting warnings like:
    // [RS0030] The symbol '...' is banned in this project: ...
    #pragma warning disable RS0030
    
    public static class TestBannedApis
    {
        /// Used to verify that banned APIs works as expected. See: ./BannedSymbols.txt
        private static void CallBannedApis() {
            Process.Start("abc", "def");
            var type = Type.GetType("xyz");
            
            Activator.CreateInstance("abc", "def");
            Activator.CreateInstance("abc", "def", null);
        }
    }
}