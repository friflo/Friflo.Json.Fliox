#if UNITY_5_3_OR_NEWER

#if !UNITY_DOTSRUNTIME
#endif
using NUnit.Framework;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.LowLevel;

#if !NET_DOTS

#endif

namespace Friflo.Json.Tests.Unity.Utils
{
    public class ECSTestsCommonBase
    {
        [SetUp]
        public virtual void Setup()
        {
#if UNITY_DOTSRUNTIME
            Unity.Core.TempMemoryScope.EnterScope();
#endif
        }

        [TearDown]
        public virtual void TearDown()
        {
#if UNITY_DOTSRUNTIME
            Unity.Core.TempMemoryScope.ExitScope();
#endif
        }
    }

    /**
     * Note
     * ECSTestsFixture is copied from Unity.Entities.Tests and all unnecessary parts are commented
     */
    public abstract class ECSTestsFixture : ECSTestsCommonBase
    {
        protected World m_PreviousWorld;
        public World World;
#if !UNITY_DOTSRUNTIME
        protected PlayerLoopSystem m_PreviousPlayerLoop;
#endif
        protected EntityManager m_Manager;
        protected EntityManager.EntityManagerDebug m_ManagerDebug;

        protected int StressTestEntityCount = 1000;
        private bool JobsDebuggerWasEnabled;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

#if !UNITY_DOTSRUNTIME
            // unit tests preserve the current player loop to restore later, and start from a blank slate.
            m_PreviousPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());
#endif

            m_PreviousWorld = World.DefaultGameObjectInjectionWorld;
            World = World.DefaultGameObjectInjectionWorld = new World("Test World");
            m_Manager = World.EntityManager;
            m_ManagerDebug = new EntityManager.EntityManagerDebug(m_Manager);

            // Many ECS tests will only pass if the Jobs Debugger enabled;
            // force it enabled for all tests, and restore the original value at teardown.
            JobsDebuggerWasEnabled = JobsUtility.JobDebuggerEnabled;
            JobsUtility.JobDebuggerEnabled = true;
        }

        [TearDown]
        public override void TearDown()
        {
            if (World != null && World.IsCreated)
            {
                // Clean up systems before calling CheckInternalConsistency because we might have filters etc
                // holding on SharedComponentData making checks fail
                while (World.Systems.Count > 0)
                {
                    World.DestroySystem(World.Systems[0]);
                }

                m_ManagerDebug.CheckInternalConsistency();

                World.Dispose();
                World = null;

                World.DefaultGameObjectInjectionWorld = m_PreviousWorld;
                m_PreviousWorld = null;
                m_Manager = default;
            }

            JobsUtility.JobDebuggerEnabled = JobsDebuggerWasEnabled;

#if !UNITY_DOTSRUNTIME
            PlayerLoop.SetPlayerLoop(m_PreviousPlayerLoop);
#endif

            base.TearDown();
        }
    }
}
#endif // UNITY_5_3_OR_NEWER
