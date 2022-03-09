// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using System.Xml.Linq;

namespace Friflo.Json.Fliox.Schema.Doc
{
    public static class MarkdownDoc
    {
        public static string CreateMarkdown(StringBuilder sb, XElement element) {
            sb.Clear();
            AppendElement(sb, element);
            return sb.ToString();
        }

        private static void AppendElement(StringBuilder sb, XElement element) {
            var nodes = element.DescendantNodes();
            foreach (var node in nodes) {
                if (node.Parent != element)
                    continue;
                AppendNode(sb, node);
            }
        }
        
        private static void AppendElement(StringBuilder sb, XElement element, string start, string end) {
            sb.Append(start);
            AppendElement(sb, element);
            sb.Append(end);
        }
        
        private static void AppendNode (StringBuilder sb, XNode node) {
            if (node is XText xtext) {
                sb.Append(xtext.Value);
                return;
            }
            if (node is XElement element) {
                var name    = element.Name.LocalName;
                switch (name) {
                    case "br":      AppendElement(sb, element, "", "  ");       return; // force new line in markdown
                    case "a":       AppendElement(sb, element);                 return;
                    case "p":       AppendElement(sb, element);                 return;
                    case "ul":      AppendElement(sb, element);                 return;
                    case "li":      AppendLi     (sb, element);                 return;
                    case "b":       AppendElement(sb, element, "**", "**");     return;
                    case "i":       AppendElement(sb, element, "*", "*");       return;
                    case "code":    AppendCode   (sb, element);                 return;
                    default:
                        // Note: should not be reached
                        // dont error but explicit cases are easier to read and debug
                        AppendElement(sb, element);
                        return;
                }
            }
        }
        
        private static void AppendLi (StringBuilder sb, XElement element) {
            var localSb = new StringBuilder();
            AppendElement(localSb, element);
            var text        = localSb.ToString();
            var lines       = text.Split('\n');
            var firstLine   = true;
            foreach (var line in lines) {
                if (line == "")
                    continue;
                sb.Append(firstLine ? "- " : "  ");
                firstLine = false;
                sb.Append(line);
                sb.Append('\n');
            }
        }
        
        private static void AppendCode (StringBuilder sb, XElement element) {
            var hasNewLine = TypeDoc.HasNewLine(element); 
            sb.Append(hasNewLine ? "```\n" : "`");
            AppendElement(sb, element);
            sb.Append(hasNewLine ? "\n```" : "`");
        }
    }
}