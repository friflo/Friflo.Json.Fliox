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
            /*
            RenderTypeDim2(sb, name, 2, 2);
            RenderTypeDim2(sb, name, 2, 3);
            RenderTypeDim2(sb, name, 2, 4);
            
            RenderTypeDim2(sb, name, 3, 2);
            RenderTypeDim2(sb, name, 3, 3);
            RenderTypeDim2(sb, name, 3, 4);

            RenderTypeDim2(sb, name, 4, 2);
            RenderTypeDim2(sb, name, 4, 3); */
            RenderTypeDim2(sb, name, 4, 4);

            var footer = $@"    }}
}}
";
            sb.Append(footer);
        }

        private static void RenderTypeDim1(StringBuilder sb, string type, int size)
        {
            var str = $@"
        public static bool UseMemberFloatX{size}(this ref JObj i, ref JsonParser p, in Str32 key, ref {type}{size} value) {{
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {{
                ReadFloat{size}(ref arr, ref p, ref value);
                return true;
            }}
            return false;
        }}

        private static void ReadFloat{size}(ref JArr i, ref JsonParser p, ref {type}{size} value) {{
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
        
        private static void RenderTypeDim2(StringBuilder sb, string type, int size1, int size2)
        {
            var dim = $"{size1}x{size2}";
            var str = $@"
        private static void ReadFloat{dim}(ref JArr i, ref JsonParser p, ref {type}{dim} value) {{
            int index = 0;
            while (i.NextArrayElement(ref p)) {{
                if (i.UseElementArr(ref p, out JArr arr)) {{
                    if (index < {size1})
                        ReadFloat{size2}(ref arr, ref p, ref value[index++]);
                }} else 
                    p.ErrorMsg(""Json.Burst.Math"", ""expect JSON number"");
            }}
        }}
        
        public static bool UseMemberFloatX{dim}(this ref JObj obj, ref JsonParser p, in Str32 key, ref {type}{dim} value) {{
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {{
                ReadFloat{dim}(ref arr, ref p, ref value);
                return true;
            }}
            return false;
        }}
";
            sb.Append(str);
        }
    }
}