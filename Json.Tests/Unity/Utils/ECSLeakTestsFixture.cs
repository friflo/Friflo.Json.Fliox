#if UNITY_5_3_OR_NEWER
using System.Reflection;
using NUnit.Framework;
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
    }
}
#endif // UNITY_5_3_OR_NEWER

