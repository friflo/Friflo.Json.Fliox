using System;
using System.IO;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Managed.Utils;
using Friflo.Json.Tests.Unity.Utils;

namespace Friflo.Json.Tests.Common
{
    public class CommonUtils
    {
        public static string GetBasePath() {
#if UNITY_5_3_OR_NEWER
	        string baseDir = UnityUtils.GetProjectFolder();
#else
            string baseDir = Directory.GetCurrentDirectory() + "/../../../";
#endif
            return baseDir;
        }
        
        public static Bytes fromFile (String path) {
            string baseDir = CommonUtils.GetBasePath();
            byte[] data = File.ReadAllBytes(baseDir + path);
            ByteArray bytes = Arrays.CopyFrom(data);
            return new Bytes(bytes);
        }
    }
}