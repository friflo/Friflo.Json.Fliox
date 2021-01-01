using System;
using System.Globalization;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common
{
    public class TestFloatingPoint : ECSLeakTestsFixture
    {
	    public bool javaReference = false;

	    struct ParseDblCx
	    {
		    public ValueError cx;
		    public Bytes bytes;
		    public ValueParser parser;
	    }
	    
		[Test]
		public void ParseDouble()
		{
			if (javaReference)
			{
				AreEqual(  	 100.0		, Double. Parse(  "1E2")); 
				AreEqual(  	 100.0		, Double. Parse(  "1e2")); 
				AreEqual(  	 100.0		, Double. Parse(  "1e+2")); 
				AreEqual(  	   0.1		, Double. Parse(  "1e-1")); 
				AreEqual(  	   0.001	, Double. Parse(  "0.1e-2")); 
				AreEqual(  	1000.0		, Double. Parse( "10e2")); 
				AreEqual(  	 100.0		, Double. Parse(  "1.e2")); 

				AreEqual(  	 1.7976931348623157e+308		, Double. Parse(  "1.7976931348623157e+308"));	// Double.MAX_VALUE
				AreEqual(  	 2.2250738585072014E-308		, Double. Parse(  "2.2250738585072014E-308"));	// Double.MIN_NORMAL
				AreEqual(  	 4.9e-324						, Double. Parse(  "4.9e-324"));					// Double.MIN_VALUE
			
				AreEqual(  	-1.7976931348623157e+308		, Double. Parse( "-1.7976931348623157e+308"));	// Double.MAX_VALUE
				AreEqual(  	-2.2250738585072014E-308		, Double. Parse( "-2.2250738585072014E-308"));	// Double.MIN_NORMAL
				AreEqual(  	-4.9e-324						, Double. Parse( "-4.9e-324"));					// Double.MIN_VALUE
			
				// Double.POSITIVE_INFINITY & Double.NEGATIVE_INFINITY
				AreEqual(  	 1.0 / 0.0						, Double. Parse(  "1.7976931348623159e+308"));
				AreEqual(  	-1.0 / 0.0						, Double. Parse( "-1.7976931348623159e+308"));
				AreEqual(  	 1.0 / 0.0						, Double. Parse(  "1e+309"));
				AreEqual(  	 0.0							, Double. Parse(  "4.9e-325"));
			}

			using (Bytes bytes = new Bytes(32)) {
				ParseDblCx parseCx = new ParseDblCx {
					bytes = bytes
				};
				parseCx.parser.InitValueParser();
				AreEqual(     0.0,        ParseDbl(           "0",		ref parseCx)); 
				AreEqual(	1234567890.0, ParseDbl(	 "1234567890",		ref parseCx));
				AreEqual(	123.0		, ParseDbl(	  	    "123",		ref parseCx)); 
				AreEqual(	 12.3		, ParseDbl(			 "12.3",	ref parseCx)); 
				AreEqual(	  1.23		, ParseDbl(			  "1.23",	ref parseCx));
				AreEqual(	  0.12		, ParseDbl(			   ".12",	ref parseCx));
				AreEqual(	  0.12		, ParseDbl(			  "0.12",	ref parseCx));
				AreEqual(	  0.12		, ParseDbl(			  "0.1200", ref parseCx));
				AreEqual(	  0.12		, ParseDbl(			"000.12",	ref parseCx));
				AreEqual(	  0.012		, ParseDbl(			  "0.012",	ref parseCx));
				AreEqual(	 +1.23		, ParseDbl(			  "1.23",	ref parseCx));
				AreEqual(	 -1.23		, ParseDbl(			 "-1.23",	ref parseCx));
				
				AreEqual(	 100.0		, ParseDbl(			  "1E2",	ref parseCx));
				AreEqual(	 100.0		, ParseDbl(			  "1e2",	ref parseCx));
				AreEqual(	 100.0		, ParseDbl(			  "1e+2",	ref parseCx));
				AreEqual(	   0.1		, ParseDbl(			  "1e-1",	ref parseCx)); 
				AreEqual(	   0.001	, ParseDbl(			  "0.1e-2", ref parseCx)); 
				AreEqual(	1000.0		, ParseDbl(			 "10e2",	ref parseCx)); 
				AreEqual(	 100.0		, ParseDbl(			  "1.e2",	ref parseCx));
				
				AreEqual(  1.7976931348623157e+308,	ParseDbl(  "1.7976931348623157e+308",	ref parseCx)); // Double.MAX_VALUE
				AreEqual(  2.2250738585072014E-308,	ParseDbl(  "2.2250738585072014E-308",	ref parseCx)); // Double.MIN_NORMAL
				AreEqual(  4.9e-324, 				ParseDbl(  "4.9e-324",					ref parseCx)); // Double.MIN_VALUE
				
				AreEqual( -1.7976931348623157e+308,	ParseDbl( "-1.7976931348623157e+308",	ref parseCx)); // Double.MAX_VALUE
				AreEqual( -2.2250738585072014E-308,	ParseDbl( "-2.2250738585072014E-308",	ref parseCx)); // Double.MIN_NORMAL
				AreEqual( -4.9e-324, 				ParseDbl( "-4.9e-324",					ref parseCx)); // Double.MIN_VALUE
				
				AreEqual(  1.0 / 0.0, 				ParseDbl(  "1.7976931348623159e+308",	ref parseCx));
				AreEqual( -1.0 / 0.0, 				ParseDbl( "-1.7976931348623159e+308",	ref parseCx));
				AreEqual(  1.0 / 0.0, 				ParseDbl(  "1e+309",					ref parseCx));
				AreEqual(  0.0, 					ParseDbl(  "4.9e-325",					ref parseCx));
				
				AreEqual(	   0.0		, ParseDbl(			  "",		ref parseCx)); IsTrue (parseCx.cx.IsErrSet()); 
				AreEqual(	   0.0		, ParseDbl(			  "1e",		ref parseCx)); IsTrue (parseCx.cx.IsErrSet()); 
				AreEqual(	   0.0		, ParseDbl(			  "1e+",	ref parseCx)); IsTrue (parseCx.cx.IsErrSet());
				parseCx.parser.Dispose();
			}
		}

		double ParseDbl(String value, ref ParseDblCx parseCx) {
			parseCx.bytes.Clear();
			parseCx.bytes.FromString(value);
			return parseCx.parser.ParseDouble(ref parseCx.bytes, ref parseCx.cx, out _);
		}
		
		bool isDouble;
		
		private void WriteDouble (double val)
		{
			using (ValueFormat bb = new ValueFormat()) {
				bb.InitTokenFormat();
				Bytes bytes = new Bytes(0);
				if (isDouble)
				{
					bb.AppendDbl(ref bytes, val); 
				//	FFLog.log("  " + val .ToString());
				//	FFLog.log("  " + bb.ToString());
				//	FFLog.log("");
	                double ret  =  Double. Parse( bytes.ToString() , NumberFormatInfo.InvariantInfo);
					long l1 = BitConverter.DoubleToInt64Bits (val);
					long l2 = BitConverter.DoubleToInt64Bits (ret);
					if (l1 != l2)
					{
						long dif = Math. Abs (l1 - l2);
						if (dif > 11)
							Fail("Conversion failed of double: " + val);
					}
				}
				else
				{
					float flt = (float)val;
					bb.AppendFlt(ref bytes, flt); 
				//	FFLog.log("  " + flt .ToString(NumberFormatInfo.InvariantInfo));
				//	FFLog.log("  " + bb.ToString());
				//	FFLog.log("");
					float ret = Single.Parse( bytes.ToString() , NumberFormatInfo.InvariantInfo);
					long l1 = BitConverter.DoubleToInt64Bits (flt) >> (52-23);
					long l2 = BitConverter.DoubleToInt64Bits (ret) >> (52-23);
					if (l1 != l2)
					{
						long dif = Math. Abs (l1 - l2);
						// Java: exact; C# dif: 1
						if (dif > 1)
							Fail("Conversion failed of double: " + val);
					}
				}
				bytes.Dispose();
			}
		}
		
		[Test]
		public void WriteDouble()
		{
			isDouble = true;
			WriteDblFlt();
		}
		
		[Test]
		public void WriteFloat()
		{
			WriteDblFlt();		
		}
		
		private void WriteDblFlt()
		{
			WriteDouble(  1.0 / 0.0					);  // Double.POSITIVE_INFINITY
			WriteDouble( -1.0 / 0.0					);	// Double.NEGATIVE_INFINITY
	//		WriteDouble( 0.0d / 0.0					);	// Double.NaN
			WriteDouble( 10000						);
			WriteDouble( 10000.123					);
			WriteDouble( 0.1						);
			WriteDouble( 0.001						);
			WriteDouble( 0.0001						);
			WriteDouble( 0.123456789012345678		);
			WriteDouble( 0.123456789012345678e200	);
			WriteDouble( 0.123456789012345678e-200	);
			WriteDouble( 0.9999999999999999			);	// 16 * 9
			WriteDouble( 0.2						);
			WriteDouble( 0.99						);
			WriteDouble( 0.0						);
			WriteDouble( 1.0						);
			WriteDouble( 2.0						);
			WriteDouble( 3.0						);
			WriteDouble( 4.0						);
			WriteDouble( 8.0						);
			WriteDouble( 10.0						);
			WriteDouble( 1e200						);
			WriteDouble( 1e-200						);
			WriteDouble( 1.7976931348623157e+308	);
			WriteDouble( 4.9e-294					);
			WriteDouble( 4.9e-295					);  // will use Double.toString();
			WriteDouble( 4.9e-324					);  // will use Double.toString();
			
		}
		
		[Test]
		public void WriteDoublePerf()
		{
			using (ValueFormat bb = new ValueFormat()) {
				bb.InitTokenFormat();
				Bytes bytes = new Bytes(64);
				long s = 0;
				double d = 0;
				int count = 1; // 10000000;
				for (int  n = 0; n < count; n++)
				{
					d += 0.00000000000000001;
					// s += Double.toString(d).length();
					bytes.Clear();
					bb.AppendDbl(ref bytes, d);
					s += bytes.Len;
				}
				bytes.Dispose();
				TestContext.Out.WriteLine($"WriteDoublePerf: {s}");
			}
		}
	
		static	String	testFloat = "1234.56789";
				int 	num3 = 		10;
		
		[Test]
		public void TestParseDouble()
		{
			double sum = 0;
			for (int n = 0; n < num3; n++)
				sum += Double. Parse (testFloat); 
			TestContext.Out.WriteLine($"WriteDoublePerf: {sum}");
		}
		
		public void TestParseDoubleFast()
		{
			ValueError valueError = new ValueError();
			ValueParser parser = new ValueParser();
			parser.InitValueParser();
			double sum = 0;
			Bytes bytes = new Bytes (testFloat);
			for (int n = 0; n < num3; n++) {
				sum += parser.ParseDouble(ref bytes, ref valueError, out _);
			}
			parser.Dispose();
			TestContext.Out.WriteLine($"TestParseDoubleFast: {sum}");
		}
    }
}