#if UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using UnityEditor;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
 
// See:
// [[SOLVED] Burst and Mono Cecil - Unity Forum] https://forum.unity.com/threads/solved-burst-and-mono-cecil.781148/
public class ILWeaver
{
    [InitializeOnLoadMethod]
    static void init()
    {
        List<string> assemblies = new List<string>();
        UnityEditor.Compilation.CompilationPipeline.compilationStarted += (o) => {
            EditorApplication.LockReloadAssemblies();
            assemblies.Clear();
        };
 
        UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished += (asmName, message) => {
            assemblies.Add(asmName);
        };
 
        UnityEditor.Compilation.CompilationPipeline.compilationFinished += (o) => {
            foreach(var asmName in assemblies) {
                AssemblyDefinition asmDef = AssemblyDefinition.ReadAssembly(asmName, new ReaderParameters() {
                    ReadWrite = true,
                });
                // Do your weaving here
 
 
                asmDef.Write();
                asmDef.Dispose();
            }
            EditorApplication.UnlockReloadAssemblies();
        };
    }
}

#endif