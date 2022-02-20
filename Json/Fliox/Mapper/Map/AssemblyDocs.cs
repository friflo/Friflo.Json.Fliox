// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

// ReSharper disable NotAccessedField.Local
// ReSharper disable UseNullPropagation
namespace Friflo.Json.Fliox.Mapper.Map
{
    internal class AssemblyDocs
    {
        private   readonly  string                      name;
        private   readonly  Dictionary<string, string>  signatures; // is null no documentation available
        
        internal            bool                        Available => signatures != null;
        
        private AssemblyDocs(string name, Dictionary<string, string>  signatures) {
            this.name       = name;
            this.signatures = signatures;
        }
        
        internal string GetDocumentation(string signature) {
            signatures.TryGetValue(signature, out var result);
            return result;
        }

        internal static AssemblyDocs Load(Assembly assembly) {
            var fullName        = assembly.FullName;
            var assemblyPath    = assembly.Location;
            var assemblyExt     = Path.GetExtension(assembly.Location);
            var docsPath        = assemblyPath.Substring(0, assemblyPath.Length - assemblyExt.Length) + ".xml";
            var documentation   = XDocument.Load(docsPath);

            var signatures  = GetSignatures (documentation);
            var docs        = new AssemblyDocs(fullName, signatures);
            return docs;
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

            foreach (XElement element in memberElements) {
                var signature   = element.Attribute("name");
                var summary     = element.Element("summary");
                if (signature == null || summary == null)
                    continue;
                signatures[signature.Value] = summary.Value;
            }
            return signatures;
        }
    }
}