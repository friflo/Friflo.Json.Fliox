// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using System.Xml.Linq;

namespace Friflo.Json.Fliox.Mapper.Map
{
    /// convert C# / .NET xml to HTML
    internal static class AssemblyDocsHtml
    {
        internal static void AppendElement(StringBuilder sb, XElement element) {
            var nodes = element.DescendantNodes();
            foreach (var node in nodes) {
                if (node.Parent != element)
                    continue;
                AppendNode(sb, node);
            }
        }
        
        private static void AppendNode (StringBuilder sb, XNode node) {
            if (node is XText text) {
                AppendTrimLines(sb, text);
                return;
            }
            if (node is XElement element) {
                var name    = element.Name.LocalName;
                var value   = element.Value;
                switch (name) {
                    case "see":
                    case "seealso":
                    case "paramref":
                    case "typeparamref":
                                    AppendAttribute(sb, element);               return;
                    
                    case "para":    AppendContainer(sb, element, "p");          return;
                    case "list":    AppendContainer(sb, element, "ul");         return;
                    case "item":    AppendContainer(sb, element, "li");         return;
                    
                    case "br":      sb.Append("<br/>");                         return;
                    case "b":       AppendTag(sb, "<b>",    "</b>",    value);  return;
                    case "i":       AppendTag(sb, "<i>",    "</i>",    value);  return;
                    case "c":       AppendTag(sb, "<c>",    "</c>",    value);  return;
                    case "code":    AppendTag(sb, "<code>", "</code>", value);  return;
                    case "returns":                                             return;
                    default:        sb.Append(value);                           return;
                }
            }
        }
        
        private static void AppendTag (StringBuilder sb, string start, string end, string value) {
            sb.Append(start);
            sb.Append(value);
            sb.Append(end);
        }
        
        private static void AppendContainer (StringBuilder sb, XElement element, string tag) {
            sb.Append('<'); sb.Append(tag); sb.Append('>');
            AppendElement(sb, element);
            sb.Append("</"); sb.Append(tag); sb.Append('>');
        }
        
        private static void AppendAttribute (StringBuilder sb, XElement element) {
            var attributes = element.Attributes();
            foreach (var attribute in attributes) {
                var attributeName = attribute.Name;
                if (attributeName == "cref" || attributeName == "name") {
                    var value       = attribute.Value;
                    var lastIndex   = value.LastIndexOf('.');
                    var typeName    = lastIndex == -1 ? value : value.Substring(lastIndex + 1);
                    AppendTag(sb, "<b>", "</b>", typeName);
                    return;
                }
                if (attributeName == "href") {
                    var link    = attribute.Value;
                    var a       = $"<a href='{link}'>";
                    AppendTag(sb, a, "</a>", link);
                    return;
                }
            }
        }
        
        /// <summary>Trim leading tabs and spaces. Normalize new lines</summary>
        private static void AppendTrimLines (StringBuilder sb, XText text) {
            string value    = text.Value;
            value           = value.Replace("\r\n", "\n");
            var lines       = value.Split("\n");
            if (lines.Length == 1) {
                sb.Append(value);
                return;
            }
            bool first  = true;
            foreach (var line in lines) {
                if (first) {
                    first = false;
                    sb.Append(line);
                    continue;
                }
                sb.Append('\n');
                sb.Append(line.TrimStart());
            }
        }
    }
}