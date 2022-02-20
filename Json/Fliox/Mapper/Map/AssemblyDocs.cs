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
    // ------------------------------------ AssemblyDocs ------------------------------------
    internal class AssemblyDocs
    {
        private     readonly    Dictionary <string, AssemblyDoc>   assemblyDocs =  new Dictionary <string, AssemblyDoc >();
    
        private AssemblyDoc GetAssemblyDoc(Assembly assembly) {
            var name = assembly.GetName().Name;
            if (name == null)
                return null;
            if (!assemblyDocs.TryGetValue(name, out var docs)) {
                docs = AssemblyDoc.Load(assembly);
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
    internal class AssemblyDoc
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

        internal static AssemblyDoc Load(Assembly assembly) {
            var name            = assembly.GetName().Name;
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