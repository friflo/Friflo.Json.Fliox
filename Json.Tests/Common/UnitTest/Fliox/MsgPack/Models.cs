using System;
using static Friflo.Json.Fliox.MsgPack.MsgFormatUtils;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack
{
    public class Sample
    {
        public int      x;
        public Child    child;
        
        // { "x": 2147483647, "child": null }
        public static  ReadOnlySpan<byte> Test   => HexToSpan("82 A1 78 CE 7F FF FF FF A5 63 68 69 6C 64 C0");

        // { "x": 111222333444, "child": null }
        public static  ReadOnlySpan<byte> Error  => HexToSpan("82 A1 78 CB 42 39 E5 5E 20 04 00 00 A5 63 68 69 6C 64 C0");
    }

    public class Child
    {
        public int y;
    }
}