// Copyright (c) Ullrich Praetz. All rights reserved.  
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Tests.Common.Utils
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
        
        public static Bytes FromFile (String path) {
            string baseDir = CommonUtils.GetBasePath();
            byte[] data = File.ReadAllBytes(baseDir + path);
            Bytes dst = new Bytes(0);
            Arrays.ToBytes(ref dst, data);
            return dst;
        }
        
        public static  Bytes FromString (String str) {

            Bytes buffer = new Bytes(256);
            str = str. Replace ('\'', '\"');
            buffer.AppendString(str);
            Bytes ret = buffer.SwapWithDefault();
            buffer.Dispose();
            return ret;
        }
        
        public static void ToFile (String path, Bytes bytes) {
            string baseDir = CommonUtils.GetBasePath();
            byte[] dst = new byte[bytes.Len];
            Arrays.ToManagedArray(dst, bytes);
            using (FileStream fileStream = new FileStream(baseDir + path, FileMode.Create)) {
                fileStream.Write(dst, 0, dst.Length);
            }
        }
    }

}