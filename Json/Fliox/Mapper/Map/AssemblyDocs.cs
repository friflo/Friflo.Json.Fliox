// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

namespace Friflo.Json.Fliox.Mapper.Map
{
    internal class AssemblyDocs
    {
        private   readonly  string                      name;
        internal  readonly  bool                        available;
        private   readonly  Dictionary<string, string>  signatures; 
        
        private AssemblyDocs(string name, bool available, Dictionary<string, string>  signatures) {
            this.name       = name;
            this.available  = available;
            this.signatures = signatures;
        }

        internal static AssemblyDocs Load(Assembly assembly) {
            var name            = assembly.FullName;
            var path            = assembly.Location;
            var documentation   = XDocument.Load(path);

            var signatures  = GetSignatures (documentation);
            var docs        = new AssemblyDocs(name, true, signatures);
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