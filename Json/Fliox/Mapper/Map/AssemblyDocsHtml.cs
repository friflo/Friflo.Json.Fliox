// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using System.Xml.Linq;

namespace Friflo.Json.Fliox.Mapper.Map
{
    /// convert C# / .NET xml to HTML
    internal static class AssemblyDocsHtml
    {
        internal static string GetElementText(StringBuilder sb, XElement element) {
            var nodes = element.DescendantNodes();
            // var nodes = element.DescendantsAndSelf();
            // if (element.Value.Contains("Check some new lines")) { int i = 42; }
            foreach (var node in nodes) {
                if (node.Parent != element)
                    continue;
                var nodeText = GetNodeText(node);
                sb.Append(nodeText);
            }
            var text = sb.ToString();
            sb.Clear();
            return text;
        }
        
        private static string GetNodeText (XNode node) {
            if (node is XText text) {
                return TrimLines(text);
            }
            if (node is XElement element) {
                var name    = element.Name.LocalName;
                var value   = element.Value;
                switch (name) {
                    case "see":
                    case "seealso":
                    case "paramref":
                    case "typeparamref":
                        return GetAttributeText(element);
                    case "para":    return GetContainerText(element, "p");
                    case "list":    return GetContainerText(element, "ul");
                    case "item":    return GetContainerText(element, "li");
                    
                    case "br":      return "<br/>";
                    case "b":       return $"<b>{value}</b>";
                    case "i":       return $"<i>{value}</i>";
                    case "c":       return $"<c>{value}</c>";
                    case "code":    return $"<code>{value}</code>";
                    case "returns": return "";
                    default:        return value;
                }
            }
            return "";
        }
        
        private static string GetContainerText (XElement element, string tag) {
            var sb = new StringBuilder();
            var value = GetElementText(sb, element);
            return $"<{tag}>{value}</{tag}>";
        }
        
        private static string GetAttributeText (XElement element) {
            var attributes = element.Attributes();
            // if (element.Value.Contains("TypeValidator")) { int i = 111; }
            foreach (var attribute in attributes) {
                var attributeName = attribute.Name;
                if (attributeName == "cref" || attributeName == "name") {
                    var value       = attribute.Value;
                    var lastIndex   = value.LastIndexOf('.');
                    var typeName    = lastIndex == -1 ? value : value.Substring(lastIndex + 1);
                    return $"<b>{typeName}</b>";                            
                }
                if (attributeName == "href") {
                    var link = attribute.Value;
                    return $"<a href='{link}'>{link}</a>";
                }
            }
            return "";
        }
        
        /// <summary>Trim leading tabs and spaces. Normalize new lines</summary>
        private static string TrimLines (XText text) {
            string value    = text.Value;
            value           = value.Replace("\r\n", "\n");
            var lines       = value.Split("\n");
            if (lines.Length == 1)
                return value;
            var sb      = new StringBuilder();
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
            return sb.ToString();
        }
    }
}