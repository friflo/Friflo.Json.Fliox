#if UNITY_2020_1_OR_NEWER

using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using Unity.Entities;

namespace Friflo.Json.Tests.Unity
{
    // Notes!
    // For running a Burst unit test WithCode()
    //      Use at least one capturedVariable in the lambda (Job)
    //
    // If this requirements is not met the error bellow in the console is logged when running Play in the Editor:
    //      (0,0): Burst error BC1049: Zero-size empty struct `Friflo.BurstedJson.Tests.TestJsonBurst/BurstedCode/<>c__DisplayClass_OnUpdate_LambdaJob0` is not supported.
    //      To fix this issue, apply this attribute to the struct: `[StructLayout(LayoutKind.Sequential, Size = 1)]`.
    public class BurstedCode : SystemBase
    {
        protected override void OnUpdate() {
            int capturedVariable = 42;
            Job.WithBurst().WithCode(() =>
            {
                // ReSharper disable once UnusedVariable
                int val = capturedVariable;
            }).Schedule();
        }
    }
    
    // [StructLayout(LayoutKind.Sequential, Size = 1)]
    public class TestBurstedCode : LeakTestsFixture
    {
        [Test]
        public void TestParser() {

            var system = World.GetOrCreateSystem<BurstedCode>();
            system.Update();
            // NativeArray<int> arr2 = new NativeArray<int>(1, Allocator.TempJob);
        }
        

    }

}

#endif // UNITY_2020_1_OR_NEWER
