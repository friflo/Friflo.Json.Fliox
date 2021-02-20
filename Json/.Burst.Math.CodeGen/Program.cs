using System;
using System.IO;
using System.Text;

namespace Friflo.Json.Burst.Math.CodeGen
{
    class Program
    {
        private static void Main(string[] args) {
            var sb = new StringBuilder();

            string type = "float";
            
            RenderType(sb, type);
            
            string baseDir = Directory.GetCurrentDirectory() + "/../../../../Burst.Math/";
            baseDir = Path.GetFullPath(baseDir);
            string path = baseDir + type + ".gen.cs";
            using (StreamWriter fileStream = new StreamWriter(path)) {
                fileStream.Write(sb);
            }
        }
        
        private static void RenderType(StringBuilder sb, string name) {
            
            sb.Append("// Copyright (c) Ullrich Praetz. All rights reserved.\n// See LICENSE file in the project root for full license information.");
            
            
        }
    }
}