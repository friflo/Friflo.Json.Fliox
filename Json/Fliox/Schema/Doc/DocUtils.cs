// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using System.Xml.Linq;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Schema.Doc
{
    public static class TypeDoc
    {
        /// <summary>Convert the given <see paramref="html"/> to source code documentation</summary>
        public static string HtmlToDoc (string html, string indent, string start, string newLine, string end) {
            if (html == null)
                return "";
            var sb      = new StringBuilder();
            var lines   = CreateMarkdownLines(sb, html);
            
            // --- format markdown as code (Typescript) documentation
            if (lines.Length == 1)
                return $"{indent}{start} {lines[0]}{end}\n";

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
                    sb.Append(line);
                    sb.Append('\n');
                    continue;
                }
                sb.Append(indent);
                sb.Append(newLine);
                sb.Append(line);
                sb.Append('\n');
            }
            sb.Append(indent);
            sb.Append(end);
            sb.Append('\n');
            return sb.ToString();
        }
        
        /// <summary>Convert the given <see paramref="html"/> string to string[]. Each array item is a lines</summary>
        public static string[] CreateMarkdownLines(StringBuilder sb, string html) {
            // --- convert html to XElement
            var htmlElement = CreateHtmlElement(sb, html);
            
            // --- convert htmlElement to markdown
            var markdown    = MarkdownDoc.CreateMarkdown(sb, htmlElement);
            return markdown.Split('\n');
        }
        
        /// <summary>Convert the given <see paramref="html"/> string to an <see cref="XElement"/></summary>
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
        
        /// <summary>Return true if text of the given <see paramref="element"/> contains a new line</summary>
        public static bool HasNewLine (XElement element) {
            var nodes = element.DescendantNodes();
            foreach (var node in nodes) {
                if (node is XText text) {
                    if (text.Value.Contains("\n"))
                        return true;
                }
            }
            return false;
        }
    }
}