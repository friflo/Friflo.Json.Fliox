using System;
using System.IO;
using System.Text;

namespace Friflo.Json.Burst.Math.CodeGen
{
    class Program
    {
        private static void Main(string[] args) {
            GenerateType("bool", "Bln", "Bln");
            GenerateType("float", "Num", "Dbl");
            GenerateType("double", "Num", "Dbl");
            GenerateType("int", "Num", "Lng");
            // GenerateType("uint",    "Lng");
        }

        private static void GenerateType(string type, string readSuffix, string writeSuffix) {
            var read = new StringBuilder();
            var write = new StringBuilder();

            RenderType(read, write, type, readSuffix, writeSuffix);

            WriteFile(read, type + ".read.gen.cs");
            WriteFile(write, type + ".write.gen.cs");
        }

        private static void WriteFile(StringBuilder read, string fileName) {
            string baseDir = Directory.GetCurrentDirectory() + "/../../../../Burst.Math/";
            baseDir = Path.GetFullPath(baseDir);
            string path = baseDir + fileName;
            using (StreamWriter fileStream = new StreamWriter(path)) {
                fileStream.Write(read);
            }
        }

        private static void RenderType(StringBuilder read, StringBuilder write, string name, string readSuffix,
            string writeSuffix) {
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
            read.Append(header);
            write.Append(header);

            RenderTypeDim1(read, write, name, 2, readSuffix, writeSuffix);
            RenderTypeDim1(read, write, name, 3, readSuffix, writeSuffix);
            RenderTypeDim1(read, write, name, 4, readSuffix, writeSuffix);

            // 2x2, 2x3, 2x4, 3x2, 3x3, 3x4, 4x2, 4x3, 4x4
            RenderTypeDim2(read, write, name, 2, 2);
            RenderTypeDim2(read, write, name, 2, 3);
            RenderTypeDim2(read, write, name, 2, 4);

            RenderTypeDim2(read, write, name, 3, 2);
            RenderTypeDim2(read, write, name, 3, 3);
            RenderTypeDim2(read, write, name, 3, 4);

            RenderTypeDim2(read, write, name, 4, 2);
            RenderTypeDim2(read, write, name, 4, 3);
            RenderTypeDim2(read, write, name, 4, 4);

            var footer = $@"    }}
}}
";
            read.Append(footer);
            write.Append(footer);
        }

        private static string GetPascalCase(string type) {
            return type[0].ToString().ToUpper() + type.Substring(1);
        }

        private static void RenderTypeDim1(StringBuilder read, StringBuilder write, string type, int size,
            string readSuffix, string writeSuffix) {
            var pascal = GetPascalCase(type);

            // --- Reader
            var reader = $@"
        public static bool UseMember{pascal}{size}(this ref JObj i, ref JsonParser p, in Str32 key, ref {type}{size} value) {{
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {{
                Read{pascal}{size}(ref arr, ref p, ref value);
                return true;
            }}
            return false;
        }}

        private static void Read{pascal}{size}(ref JArr i, ref JsonParser p, ref {type}{size} value) {{
            int index = 0;
            while (i.NextArrayElement(ref p)) {{
                if (i.UseElement{readSuffix}(ref p)) {{
                    if (index < {size})
                        value[index++] = p.ValueAs{pascal}(out bool _);
                }} else 
                    p.ErrorMsg(""Json.Burst.Math"", ""expect JSON number"");
            }}
        }}
";
            read.Append(reader);

            // --- Writer
            var components = new StringBuilder();
            for (int i = 0; i < size; i++)
                components.Append($@"
            s.Element{writeSuffix}(value.{Coordinate[i]});");

            var writer = $@"
        public static void Member{pascal}{size}(this ref JsonSerializer s, in Str32 key, in {type}{size} value) {{
            s.MemberArrayStart(key, false);
            Write{pascal}{size}(ref s, in value);
            s.ArrayEnd();
        }}

        public static void Array{pascal}{size}(this ref JsonSerializer s, in {type}{size} value) {{
            s.ArrayStart(false);
            Write{pascal}{size}(ref s, in value);
            s.ArrayEnd();
        }}

        private static void Write{pascal}{size}(ref JsonSerializer s, in {type}{size} value) {{{components}
        }}
";
            write.Append(writer);
        }

        private static readonly char[] Coordinate =  { 'x', 'y', 'z', 'w' };

        private static void RenderTypeDim2(StringBuilder read, StringBuilder write, string type, int size1, int size2)
        {
            var pascal = GetPascalCase(type);
            var dim = $"{size1}x{size2}";
            
            // --- Reader
            var reader = $@"
        private static void Read{pascal}{dim}(ref JArr i, ref JsonParser p, ref {type}{dim} value) {{
            int index = 0;
            while (i.NextArrayElement(ref p)) {{
                if (i.UseElementArr(ref p, out JArr arr)) {{
                    if (index < {size2})
                        Read{pascal}{size1}(ref arr, ref p, ref value[index++]);
                }} else 
                    p.ErrorMsg(""Json.Burst.Math"", ""expect JSON number"");
            }}
        }}

        public static bool UseMember{pascal}{dim}(this ref JObj obj, ref JsonParser p, in Str32 key, ref {type}{dim} value) {{
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {{
                Read{pascal}{dim}(ref arr, ref p, ref value);
                return true;
            }}
            return false;
        }}
";
            read.Append(reader);
            
            // --- Writer
            var components = new StringBuilder();
            for (int i = 0; i < size2; i++)
                components.Append($@"
            Array{pascal}{size1}(ref s, in value.{Component[i]});");
            
            var writer = $@"
        public static void Member{pascal}{dim}(this ref JsonSerializer s, in Str32 key, in {type}{dim} value) {{
            s.MemberArrayStart(key, true);{components}
            s.ArrayEnd();
        }}
";
            write.Append(writer);

        }
        
        private static readonly string[] Component =  { "c0", "c1", "c2", "c3" };
    }
}