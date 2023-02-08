using System;
using System.Threading;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    public static class TestSynchronization
    {
        [Test]
        public static void TestManualResetEvent() {
            // --- output - thread1 / thread2 order is arbitrary
            // mre.Set()
            // thread1 signaled
            // thread2 signaled
            var mre = new ManualResetEvent(false);
            var thread1 = new Thread(() => {
                mre.WaitOne();
                mre.WaitOne();  // does not block - mre stay signaled until calling mre.Reset()
                Console.WriteLine("thread1 signaled");
            }) { Name = "thread1"};
            
            var thread2 = new Thread(() => {
                mre.WaitOne();
                mre.WaitOne();   // does not block - mre stay signaled until calling mre.Reset()
                Console.WriteLine("thread2 signaled");
            }) { Name = "thread2"};
            
            thread1.Start();
            thread2.Start();
            
            Console.WriteLine("mre.Set()"); // signal event
            mre.Set();
        }
    }
}