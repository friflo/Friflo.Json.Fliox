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
            var xml     = $"<summary>{html}</summary>";
            var docs        = XDocument.Parse(xml);
            var root        = docs.Root;
            if (root == null)
                return "";
            var str         = XElementToString(root);
            var lines       = str.Split('\n');
            if (lines.Length == 1) {
                return $"{indent}{start} {lines[0]}{end}\n";   
            }
            var sb          = new StringBuilder();
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
        
        private static string XElementToString (XElement element) {
            var sb = new StringBuilder();
            return GetElementText(sb, element);
        }
        
        private static string GetElementText(StringBuilder sb, XElement element) {
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
            if (node is XText xtext) {
                return xtext.Value;
            }
            if (node is XElement xElement) {
                var name    = xElement.Name.LocalName;
                switch (name) {
                    case "ul":  return XElementToString(xElement);
                    case "li":  return GetLiText(xElement);
                    case "b":   return $"**{XElementToString(xElement)}**";
                    case "i":   return $"*{XElementToString(xElement)}*";
                    default:
                        return XElementToString(xElement);
                }
            }
            return "";
        }
        
        private static string GetLiText (XElement element) {
            var text        = XElementToString(element);
            text            = text.Trim();
            var lines       = text.Split("\n");
            var sb          = new StringBuilder();
            var firstLine   = true;
            foreach (var line in lines) {
                sb.Append(firstLine ? "- " : "  ");
                firstLine = false;
                sb.Append(line);
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}