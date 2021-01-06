using System;
using Friflo.Json.Burst;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common
{
    public class TestParserPerformance
    {
        // Add JSON samples from https://github.com/ysharplanguage/FastJsonParser
        [Test]
        public void TestPerf_oj_highly_nested() {   jsonIterate ("assets/codec/_oj-highly-nested.json", 67);    }

        [Test]
        public void TestPerf_boon_small()       {   jsonIterate ("assets/codec/boon-small.json",        20);    }

        [Test]
        public void TestPerf_tiny()             {   jsonIterate ("assets/codec/tiny.json",              19);    }

        [Test]
        public void TestPerf_dicos()            {   jsonIterate ("assets/codec/dicos.json",             72);    }

        [Test]
        public void Boon_actionLabel()          {   jsonIterate ("assets/codec/boon/actionLabel.json",  77);    }
        
        [Test]
        public void Boon_medium()               {   jsonIterate ("assets/codec/boon/medium.json",       54);    }
    
        [Test]
        public void Boon_menu()                 {   jsonIterate ("assets/codec/boon/menu.json",         22);    }
        
        [Test]
        public void Boon_sgml()                 {   jsonIterate ("assets/codec/boon/sgml.json",         25);    }
    
        [Test]
        public void Boon_small()                {   jsonIterate ("assets/codec/boon/small.json",         4);    }
    
        [Test]
        public void Boon_webxml()               {   jsonIterate ("assets/codec/boon/webxml.json",      100);    }
    
        [Test]
        public void Boon_widget()               {   jsonIterate ("assets/codec/boon/widget.json",       46);    }

        //
        [Test]
        public void JsonExamples_canada()       {   jsonIterate ("assets/jsonexamples/canada.json",     223228);    }
    
        [Test]
        public void JsonExamples_citm_catalog() {   jsonIterate ("assets/jsonexamples/citm_catalog.json", 59166);   }
    
        [Test]
        public void JsonExamples_log()          {   jsonIterate ("assets/jsonexamples/log.json",        49);    }
    
        [Test]
        public void JsonExamples_twitter()      {   jsonIterate ("assets/jsonexamples/twitter.json",  16228);   }
        
        private void jsonIterate(String path, int expectedCount)
        {
            using (Bytes bytes = CommonUtils.FromFile(path)) {
                int impliedThroughput = CommonUtils.IsUnityEditor() ? 500_000 : 2_000_000; // MB/sec
                int iterations = impliedThroughput / bytes.Len;
                iterations = Math.Max(1, iterations); 
                long start = TimeUtil.GetMicro();
                using (JsonParser parser = new JsonParser()) {
                    int count = 0;
                    for (int n = 0; n < iterations; n++) {
                        count = 0;
                        parser.InitParser(bytes);
                        while (true) {
                            JsonEvent ev = parser.NextEvent();
                            if (ev == JsonEvent.EOF)
                                break;
                            if (ev == JsonEvent.Error)
                                Fail(parser.error.msg.ToString());
                            count++;
                        }
                    }
                    AreEqual(expectedCount, count);
                }
                double sec = (TimeUtil.GetMicro() - start) / 1_000_000.0;
                int bytesPerSec = (int)(iterations * bytes.Len / sec);
                string fullPath = CommonUtils.GetBasePath() + path;
                TestContext.Out.WriteLine($"{fullPath}:1 - size: {bytes.Len} bytes, {bytesPerSec/1_000_000} MB/s ");
            }
        }
    }
}