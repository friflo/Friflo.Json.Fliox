using System.Diagnostics;

namespace Friflo.Json.Tests.Common.Utils
{
    public static class TimeUtil
    {
        static readonly long MillisFreq = Stopwatch.Frequency / 1000;
        static readonly long MicroFreq = Stopwatch.Frequency / 1000000;
    
        static readonly long StartTime = Stopwatch.GetTimestamp();

        public static int GetMs() {
            return (int)((Stopwatch.GetTimestamp() - StartTime) / MillisFreq);
        }
    
        public static long GetMicro() {
            return (Stopwatch.GetTimestamp() - StartTime) / MicroFreq;
        }

    }
}