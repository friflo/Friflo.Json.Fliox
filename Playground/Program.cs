using NUnit.Framework;

namespace Friflo.Playground
{
    public class Program
    {
        public class Info
        {
            [Test]
            public void Message() {
                Assert.Fail("Playground test are not part of CI. They are used for evaluation and may fail. CI tests in: Friflo.Json.Tests.Common");
            }
        }
        
        public static void Main(string[] args)
        {
        }
    }
}