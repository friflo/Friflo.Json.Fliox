using System;
using System.Collections;
using System.Threading.Tasks;

namespace Friflo.Json.Tests.Common.Utils
{
    // Await utilities to async Task methods as a [UnityTest] in Unity Test Runner
    // see: [Async await in Unittests - Unity Forum] https://forum.unity.com/threads/async-await-in-unittests.513857/
    public static class RunAsync {
        public static IEnumerator Await(Task task) {
            return Await(task, i => { });
        }
        
        public static IEnumerator Await(Task task, Action<int> yieldCount) {
            int yieldCounter = 0;
            while (!task.IsCompleted) {
                yieldCounter++;
                yield return null;
            }
            yieldCount(yieldCounter);
            if (task.IsFaulted) {
                throw task.Exception;
            }
        }
        
        public static IEnumerator Await(Func<Task> taskDelegate)
        {
            return Await(taskDelegate.Invoke(), i => { });
        }
    }

    public static class Logger
    {
        public static void Info(string msg) {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.Log(msg);
#else
            Console.WriteLine(msg);
#endif
        }
    }
}
    
#if UNITY_5_3_OR_NEWER
    namespace UnitTest.Dummy {
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class TestAttribute : Attribute {
            public TestAttribute () {}
        }
    }
#else
    namespace UnityEngine.TestTools {
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class UnityTest : Attribute {
            public UnityTest () {}
        }
    }
#endif


