#if UNITY_2020_1_OR_NEWER

namespace DemoHub
{
    public class Startup
    {
        internal static void RunAspNetCore(string[] args) {
            System.Console.WriteLine("ASP.NET Core / Unity setup not configured");
        }
    }

    internal class Utils
    {
        internal Records CreateFakes(Fake fake) {
            return null;
        }
    }
}

#endif