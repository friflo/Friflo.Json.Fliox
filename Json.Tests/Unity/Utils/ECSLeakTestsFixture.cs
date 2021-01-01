using System.Diagnostics;
using System.Reflection;
using System.Text;
using Friflo.Json.Burst;
using NUnit.Framework;

using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Friflo.Json.Tests.Unity.Utils
{
    // Log unit test leaks of native containers
    public class ECSLeakTestsFixture : ECSTestsFixture
    {
        AtomicSafetyHandle m_Safety;
        DisposeSentinel ds;

        [SetUp]
        public override void Setup() {
            base.Setup();
            DisposeSentinel.Create(out m_Safety, out ds, 0, Allocator.Invalid);
        }

        [TearDown]
        public override void TearDown() {
            if (ds != null) { // ds == null, if leak detection is disabled 
                FieldInfo stackTraceField =
                    ds.GetType().GetField("m_StackTrace", BindingFlags.Instance | BindingFlags.NonPublic);
                object stackTrace = stackTraceField.GetValue(ds); // Always null. Even when DisposeSentinel.Dispose() log errors to the console

                DisposeSentinel.Dispose(ref m_Safety, ref ds);
            }
            base.TearDown();
        }
    }
}
#else
namespace Friflo.Json.Tests.Unity.Utils
{
    public class ECSLeakTestsFixture
    {
        private int currentFrameCount;
        [SetUp]
        public void Setup() {
            DebugUtils.StartLeakDetection();
            StackTrace stackTrace = new StackTrace(true);
            currentFrameCount = stackTrace.FrameCount;
        }

        [TearDown]
        public void TearDown() {
            DebugUtils.StopLeakDetection();
            
            if (DebugUtils.Allocations.Count > 0) {
                StringBuilder msg = new StringBuilder();
                foreach (var allocation in DebugUtils.Allocations) {
                    StackFrame[] frames = allocation.Value.GetFrames();
                    allocation.Value.GetFrames();
                    /* int lastFrameIndex;
                    for (int i = frames.Length - 1; i > 0; i--) {
                        StackFrame frame = frames[i];
                        MethodBase method = frame.GetMethod();
                        var module = method.Module;
                        if (module.Name.Contains("Friflo")) {
                            lastFrameIndex = i;
                        }
                    } */

                    for (int n = 1; n <= frames.Length - currentFrameCount + 1; n++) {
                        StackFrame f = frames[n];
                        msg.Append("  ");
                        MethodBase m = f.GetMethod();
                        if (m.ReflectedType != null)
                            msg.Append($"{m.ReflectedType.Namespace}.{m.ReflectedType.Name} - ");
                        
                        // msg.Append(m); // Note: create very long lines. So using only the method name:
                        msg.Append($"{m.Name}()");
                        
                        msg.Append($"  (at {f.GetFileName()}:{f.GetFileLineNumber()})\n");
                    }
                    msg.Append("\n");
                }
                Fail($"Found {DebugUtils.Allocations.Count} resource leaks\n{msg}");
            }
        }
    }
}
#endif // UNITY_5_3_OR_NEWER

