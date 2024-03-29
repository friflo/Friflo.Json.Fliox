﻿// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public class TestParserPerformance : LeakTestsFixture
    {
        // Add JSON samples from https://github.com/ysharplanguage/FastJsonParser
        [Test]
        public void TestPerf_oj_highly_nested() {   jsonIterate ("assets~/Burst/codec/_oj-highly-nested.json", 67);    }

        [Test]
        public void TestPerf_boon_small()       {   jsonIterate ("assets~/Burst/codec/boon-small.json",        20);    }

        [Test]
        public void TestPerf_tiny()             {   jsonIterate ("assets~/Burst/codec/tiny.json",              19);    }

        [Test]
        public void TestPerf_dicos()            {   jsonIterate ("assets~/Burst/codec/dicos.json",             72);    }

        [Test]
        public void Boon_actionLabel()          {   jsonIterate ("assets~/Burst/codec/boon/actionLabel.json",  77);    }
        
        [Test]
        public void Boon_medium()               {   jsonIterate ("assets~/Burst/codec/boon/medium.json",       54);    }
    
        [Test]
        public void Boon_menu()                 {   jsonIterate ("assets~/Burst/codec/boon/menu.json",         22);    }
        
        [Test]
        public void Boon_sgml()                 {   jsonIterate ("assets~/Burst/codec/boon/sgml.json",         25);    }
    
        [Test]
        public void Boon_small()                {   jsonIterate ("assets~/Burst/codec/boon/small.json",         4);    }
    
        [Test]
        public void Boon_webxml()               {   jsonIterate ("assets~/Burst/codec/boon/webxml.json",      100);    }
    
        [Test]
        public void Boon_widget()               {   jsonIterate ("assets~/Burst/codec/boon/widget.json",       46);    }

        //
        [Test]
        public void JsonExamples_canada()       {   jsonIterate ("assets~/Burst/jsonexamples/canada.json",     223228);    }
    
        [Test]
        public void JsonExamples_citm_catalog() {   jsonIterate ("assets~/Burst/jsonexamples/citm_catalog.json", 59166);   }
    
        [Test]
        public void JsonExamples_log()          {   jsonIterate ("assets~/Burst/jsonexamples/log.json",        49);    }
    
        [Test]
        public void JsonExamples_twitter()      {   jsonIterate ("assets~/Burst/jsonexamples/twitter.json",  16228);   }
        
        private void jsonIterate(String path, int expectedCount)
        {
            using (Bytes bytes = CommonUtils.FromFile(path)) {
                int impliedThroughput = CommonUtils.IsUnityEditor() ? 500_000 : 2_000_000; // MB/sec
                int iterations = impliedThroughput / bytes.Len;
                iterations = System.Math.Max(1, iterations);
                long start = TimeUtil.GetMicro();
                using (Utf8JsonParser parser = new Utf8JsonParser()) {
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