// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

// ReSharper disable UseNullPropagation
namespace Friflo.Json.Fliox.Mapper.Map
{
    // ------------------------------------ AssemblyDocs ------------------------------------
    internal sealed class AssemblyDocs
    {
        private     readonly    Dictionary <string, AssemblyDoc>   assemblyDocs =  new Dictionary <string, AssemblyDoc >();
    
        private AssemblyDoc GetAssemblyDoc(Assembly assembly) {
            // todo: may use Assembly reference instead of Assembly name as key
            var name = assembly.GetName().Name;
            if (name == null)
                return null;
            if (!assemblyDocs.TryGetValue(name, out var docs)) {
                docs = AssemblyDoc.Load(name, assembly);
                assemblyDocs[name] = docs;
            }
            if (!docs.Available)
                return null;
            return docs;
        }
        
        internal string GetDocs(Assembly assembly, string signature) {
            if (assembly == null)
                return null;
            var docs = GetAssemblyDoc(assembly);
            if (docs == null)
                return null;
            var documentation = docs.GetDocumentation(signature);
            return documentation;
        }
    }

    // ------------------------------------ AssemblyDoc ------------------------------------
    internal sealed class AssemblyDoc
    {
        private   readonly  string                      name;
        private   readonly  Dictionary<string, string>  signatures; // is null if no documentation available
        
        internal            bool                        Available => signatures != null;

        public override     string                      ToString()   => name;

        private AssemblyDoc(string name, Dictionary<string, string>  signatures) {
            this.name       = name;
            this.signatures = signatures;
        }
        
        internal string GetDocumentation(string signature) {
            signatures.TryGetValue(signature, out var result);
            return result;
        }

        internal static AssemblyDoc Load(string name, Assembly assembly) {
            var assemblyPath    = assembly.Location;
            var assemblyExt     = Path.GetExtension(assembly.Location);
            var docsPath        = assemblyPath.Substring(0, assemblyPath.Length - assemblyExt.Length) + ".xml";
            if (!File.Exists(docsPath))
                return new AssemblyDoc(name, null);

            try {
                var documentation   = XDocument.Load(docsPath);
                var signatures      = GetSignatures (documentation);
                var docs            = new AssemblyDoc(name, signatures);
                return docs;
            } catch  {
                return new AssemblyDoc(name, null);
            }
        }
        
        private static Dictionary<string, string> GetSignatures (XDocument documentation) {
            var doc     = documentation.Element("doc");
            if (doc == null)
                return null;
            var members = doc.Element("members");
            if (members == null)
                return null;
            var memberElements  = members.Elements();
            var signatures      = new Dictionary<string, string>();
            var sb              = new StringBuilder();

            foreach (XElement element in memberElements) {
                var signature   = element.Attribute("name");
                var summary     = element.Element("summary");
                if (signature == null || summary == null)
                    continue;
                // if (signature.Value.Contains("envColor")) { int i = 33; }
                var text = GetElementText(sb, summary);
                text = text.Trim();
                signatures[signature.Value] = text;
            }
            return signatures;
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
                return TrimLines(xtext);
            }
            if (node is XElement xElement) {
                var name    = xElement.Name.LocalName;
                var value   = xElement.Value;
                switch (name) {
                    case "see":
                    case "seealso":
                    case "paramref":
                    case "typeparamref":
                        return GetAttributeText(xElement);
                    case "para":    return GetContainerText(xElement, "p");
                    case "list":    return GetContainerText(xElement, "ul");
                    case "item":    return GetContainerText(xElement, "li");
                    
                    case "br":      return "<br/>";
                    case "b":       return $"<b>{value}</b>";
                    case "i":       return $"<i>{value}</i>";
                    case "c":       return $"<c>{value}</c>";
                    case "code":    return $"<code>{value}</code";
                    case "returns": return "";
                    default:        return value;
                }
            }
            return "";
        }
        
        private static string GetContainerText (XElement xElement, string tag) {
            var sb = new StringBuilder();
            var value = GetElementText(sb, xElement);
            return $"<{tag}>{value}</{tag}>";
        }
        
        private static string GetAttributeText (XElement xElement) {
            var attributes = xElement.Attributes();
            // if (xElement.Value.Contains("TypeValidator")) { int i = 111; }
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
        private static string TrimLines (XText node) {
            string value    = node.Value;
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