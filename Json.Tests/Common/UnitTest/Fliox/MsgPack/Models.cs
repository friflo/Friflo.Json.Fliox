
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack
{
    public class Sample
    {
        public int      x;
        public Child    child;
        
        // { "x": 2147483647, "child": null }
        public static readonly byte[] Test  = new byte[] { 130, 161, 120, 206, 127, 255, 255, 255, 165, 99, 104, 105, 108, 100, 192 };
        // { "x": 111222333444, "child": null }
        public static readonly byte[] Error = new byte[] { 130, 161, 120, 203, 66, 57, 229, 94, 32, 4, 0, 0, 165, 99, 104, 105, 108, 100, 192 };

    }

    public class Child
    {
        public int y;
    }
}