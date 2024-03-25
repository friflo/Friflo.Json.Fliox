// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
    /// <summary> Contains documentation of multiple assemblies </summary>
    internal sealed class AssemblyDocs
    {
        private        readonly Dictionary <string, AssemblyDoc>    assemblyDocs      =  new Dictionary <string, AssemblyDoc >();
        private static readonly Dictionary <string, AssemblyDoc>    AssemblyDocsCache =  new Dictionary <string, AssemblyDoc >();
    
        private AssemblyDoc GetAssemblyDoc(Assembly assembly) {
            // todo: may use Assembly reference instead of Assembly name as key
            var name = assembly.GetName().Name;
            if (name == null)
                return null;
            if (!assemblyDocs.TryGetValue(name, out var docs)) {
                var cache = AssemblyDocsCache;
                lock (cache) {
                    if (!cache.TryGetValue(name, out docs)) {
                        docs = AssemblyDoc.Load(name, assembly);
                        cache.Add(name, docs);
                    }
                }
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
    /// <summary> Contains documentation of a single assembly </summary>
    internal sealed class AssemblyDoc
    {
        private   readonly  string                          name;
        private   readonly  Dictionary<string, XElement>    signatures; // is null if no documentation available
        private   readonly  StringBuilder                   sb;
        
        internal            bool                            Available => signatures != null;

        public override     string                          ToString()   => name;

        private AssemblyDoc(string name, Dictionary<string, XElement>  signatures) {
            this.name       = name;
            this.signatures = signatures;
            sb              = new StringBuilder();
        }
        
        internal string GetDocumentation(string signature) {
            if (!signatures.TryGetValue(signature, out var result))
                return null;
            // if (signature.Contains("clientId")) { int i = 22; }
            AssemblyDocsHtml.AppendElement(sb, result);
            var text    = sb.ToString();
            text        = text.Trim();
            sb.Clear();
            return text;
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
        
        private static Dictionary<string, XElement> GetSignatures (XDocument documentation) {
            var doc     = documentation.Element("doc");
            if (doc == null)
                return null;
            var members = doc.Element("members");
            if (members == null)
                return null;
            var memberElements  = members.Elements();
            var signatures      = new Dictionary<string, XElement>();

            foreach (XElement element in memberElements) {
                var signature   = element.Attribute("name");
                var summary     = element.Element("summary");
                if (signature == null || summary == null)
                    continue;
                signatures[signature.Value] = summary;
            }
            return signatures;
        }
    }
}