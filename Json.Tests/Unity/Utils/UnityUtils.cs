
#if UNITY_5_3_OR_NEWER

using System.IO;
using UnityEditor;
using UnityEngine;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Unity.Utils
{
    public class UnityUtils : ScriptableObject
    {
        public static string GetProjectFolder() {
            ScriptableObject unityUtils = ScriptableObject.CreateInstance<UnityUtils>();
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(unityUtils));
            string expectedPath = "Json.Tests/Unity/Utils/UnityUtils.cs";
            if (!scriptPath.EndsWith(expectedPath))
                Fail("Expect UnityUtils located in: " + expectedPath);
            string projectBase = Path.GetDirectoryName(scriptPath) + "/../../";
            string baseDir = Directory.GetCurrentDirectory() + "/" + projectBase;
            return baseDir;
        }
    }
}

#endif
