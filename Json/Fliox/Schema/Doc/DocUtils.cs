// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using System.Xml.Linq;

namespace Friflo.Json.Fliox.Schema.Doc
{
    public static class DocUtils
    {
        public static string[] CreateMarkdownLines(StringBuilder sb, string html) {
            // --- convert html to XElement
            var htmlElement = CreateHtmlElement(sb, html);
            
            // --- convert htmlElement to markdown
            var markdown    = MarkdownDoc.CreateMarkdown(sb, htmlElement);
            return markdown.Split('\n');
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        public static XElement CreateHtmlElement(StringBuilder sb, string html) {
            sb.Clear();
            // --- create valid xml to enable parsing
            sb.Append("<summary>");
            sb.Append(html);
            sb.Append("</summary>");
            var xml     = sb.ToString();
            var docs    = XDocument.Parse(xml);
            return docs.Root;
        }
        
        public static bool HasNewLine (XElement element) {
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