// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
                    case "a":
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
                    case "code":    AppendCode(sb, element);                    return;
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
                    // handle symbols referring to a method
                    var bracket     = value.LastIndexOf('(');
                    var end         = bracket == -1 ? value.Length : bracket;
                    var lastIndex   = value.LastIndexOf('.', end - 1, end);
                    var typeName    = lastIndex == -1 ? value : value.Substring(lastIndex + 1, end - lastIndex - 1);
                    if (bracket == -1) {
                        AppendTag(sb, "<b>", "</b>", typeName);
                    } else {
                        AppendTag(sb, "<b>", "</b>", typeName + "()");
                    }
                    return;
                }
                if (attributeName == "href") {
                    var text    = element.Value;
                    var link    = attribute.Value;
                    var a       = $"<a href='{link}'>{text}</a>";
                    sb.Append(a);
                    // AppendTag(sb, a, "</a>", link);
                    return;
                }
            }
        }
        
        private static void AppendCode (StringBuilder sb, XElement element) {
            sb.Append("<code>");
            var localSb = new StringBuilder();
            AppendElement(localSb, element);
            var code = localSb.ToString().Trim();
            sb.Append(code);
            sb.Append("</code>");
        }
        
        /// <summary>Trim leading tabs and spaces. Normalize new lines</summary>
        private static void AppendTrimLines (StringBuilder sb, XText text) {
            string value    = text.Value;
            value           = value.Replace("\r\n", "\n");
            var lines       = value.Split('\n');
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