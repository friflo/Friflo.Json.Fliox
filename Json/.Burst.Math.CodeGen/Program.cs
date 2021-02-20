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
        
        private static void RenderType(StringBuilder sb, string name)
        {
            var header = $@"// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Unity.Mathematics;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Burst.Math
{{
    public static partial class Json
    {{";
            sb.Append(header);

            RenderTypeDim1(sb, name, 2);
            RenderTypeDim1(sb, name, 3);
            RenderTypeDim1(sb, name, 4);

            var footer = $@"    }}
}}
";
            sb.Append(footer);
        }

        private static void RenderTypeDim1(StringBuilder sb, string name, int size) {
        
        var str = $@"
        public static bool UseMemberFloatX{size}(this ref JObj i, ref JsonParser p, in Str32 key, ref float{size} value) {{
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {{
                ReadFloat{size}(ref arr, ref p, ref value);
                return true;
            }}
            return false;
        }}

        private static void ReadFloat{size}(ref JArr i, ref JsonParser p, ref float{size} value) {{
            int index = 0;
            while (i.NextArrayElement(ref p)) {{
                if (i.UseElementNum(ref p)) {{
                    if (index < {size})
                        value[index++] = p.ValueAsFloat(out bool _);
                }} else 
                    p.ErrorMsg(""Json.Burst.Math"", ""expect JSON number"");
            }}
        }}
";
        sb.Append(str);
        }
    }
}