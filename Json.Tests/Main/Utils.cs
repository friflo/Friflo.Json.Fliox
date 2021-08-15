#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Friflo.Json.Tests.Main
{
    public static class Utils
    {
        public static byte[] Zip (Dictionary<string, string> files) {
            using (var memoryStream = new MemoryStream()) {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true)) {
                    foreach (var pair in files) {
                        var fileName    = pair.Key;
                        var content     = pair.Value;
                        var entry = archive.CreateEntry(fileName);
                        using (var entryStream = entry.Open())
                        using (var streamWriter = new StreamWriter(entryStream)) {
                            streamWriter.Write(content);
                        }
                    }
                }
                return memoryStream.ToArray();
            }
        }
    }
}

#endif
