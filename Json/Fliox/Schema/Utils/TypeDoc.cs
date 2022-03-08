// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using System.Xml.Linq;

namespace Friflo.Json.Fliox.Schema.Utils
{
    public static class TypeDoc
    {
        public static string HtmlToText (string html, string indent, string start, string newLine, string end) {
            if (html == null)
                return "";
            var sb      = new StringBuilder();
            // --- create valid xml to enable parsing
            sb.Append("<summary>");
            sb.Append(html);
            sb.Append("</summary>");
            var xml     = sb.ToString();
            var docs    = XDocument.Parse(xml);
            var root    = docs.Root;
            if (root == null)
                return "";
            
            // --- convert html to code documentation string with markdown style
            sb.Clear();
            MarkdownDoc.AppendElementText(sb, root);
            var str     = sb.ToString();
            
            // --- format markdown as code (Typescript) documentation
            var lines   = str.Split('\n');
            if (lines.Length == 1) {
                return $"{indent}{start} {lines[0]}{end}\n";   
            }
            sb.Clear();
            var firstLine   = true;
            sb.Append(indent);
            sb.Append(start);
            sb.Append('\n');
            sb.Append(indent);
            sb.Append(newLine);
            foreach (var line in lines) {
                if (firstLine) {
                    firstLine = false;
                    sb.Append(' ');
                    sb.Append(line);
                    sb.Append('\n');
                    continue;
                }
                sb.Append(indent);
                sb.Append(newLine);
                sb.Append(' ');
                sb.Append(line);
                sb.Append('\n');
            }
            sb.Append(indent);
            sb.Append(end);
            sb.Append('\n');
            return sb.ToString();
        }
    }
}