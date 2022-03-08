// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using System.Xml.Linq;

namespace Friflo.Json.Fliox.Schema.Utils
{
    public static class MarkdownDoc
    {
        public static void AppendElementText(StringBuilder sb, XElement element) {
            var nodes = element.DescendantNodes();
            // var nodes = element.DescendantsAndSelf();
            // if (element.Value.Contains("Check some new lines")) { int i = 42; }
            foreach (var node in nodes) {
                if (node.Parent != element)
                    continue;
                AppendNodeText(sb, node);
            }
        }
        
        private static void AppendNodeText (StringBuilder sb, XNode node) {
            if (node is XText xtext) {
                sb.Append(xtext.Value);
                return;
            }
            if (node is XElement element) {
                var name    = element.Name.LocalName;
                switch (name) {
                    case "br":
                        AppendElementText(sb, element);
                        sb.Append("  "); // force new line in markdown
                        return;
                    case "ul":  AppendElementText(sb, element);    return;
                    case "li":  AppendLiText(sb, element);         return;
                    case "b":
                        sb.Append("**");
                        AppendElementText(sb, element);
                        sb.Append("**");
                        return;
                    case "i":
                        sb.Append('*');
                        AppendElementText(sb, element);
                        sb.Append('*');
                        return;
                    case "code":    AppendCode(sb, element);    return;
                    default:
                        AppendElementText(sb, element);
                        return;
                }
            }
        }
        
        private static void AppendLiText (StringBuilder sb, XElement element) {
            var localSb = new StringBuilder();
            AppendElementText(localSb, element);
            var text        = localSb.ToString();
            var lines       = text.Split("\n");
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
            var bounds = HasNewLine(element) ? "```\n" : "`"; 
            sb.Append(bounds);
            AppendElementText(sb, element);
            sb.Append(bounds);
        }
        
        private static bool HasNewLine (XElement element) {
            var nodes = element.DescendantNodes();
            foreach (var node in nodes) {
                if (node is XText text) {
                    if (text.Value.Contains('\n'))
                        return true;
                }
            }
            return false;
        }
    }
}