namespace Test_inih;

[TestClass]
public sealed class Test1
{

	static int parse(string fname, out string consoleOut)
	{
		var originalOut = Console.Out;

		int result;
		try
		{
			using StringWriter stringWriter = new();
			Console.SetOut(stringWriter);

			result = unittest.parse(fname);
			consoleOut = stringWriter.ToString();
		}
		finally
		{
			Console.SetOut(originalOut);
		}
		return result;
	}

	[TestMethod]
	public void normal()
	{
		string consoleOut;
		var result = parse("normal.ini", out consoleOut);
		Assert.AreEqual(0, result);

		const string expectedOutput = @"... [section1]
... one=This is a test;
... two=1234;
... [ section 2 ]
... happy=4;
... sad=;
... [comment_test]
... test1=1;2;3;
... test2=2;3;4;this won't be a comment, needs whitespace before ';';
... test;3=345;
... test4=4#5#6;
... test7=;
... test8=; not a comment, needs whitespace before ';';
... [colon_tests]
... Content-Type=text/html;
... foo=bar;
... adams=42;
... funny1=with = equals;
... funny2=with : colons;
... funny3=two = equals;
... funny4=two : colons;
normal.ini: e=0 user=100
";
		var actualLines = consoleOut.Split([Environment.NewLine], StringSplitOptions.None);
		var expectedLines = expectedOutput.Split([Environment.NewLine], StringSplitOptions.None);
		Assert.HasCount(expectedLines.Length, actualLines);

		for (int i = 0; i < expectedLines.Length; i++)
		{
			Assert.AreEqual(expectedLines[i], actualLines[i], $"Line {i + 1} does not match.");
		}

		//Assert.AreEqual(expectedOutput, consoleOut);

	}

	[TestMethod]
	public void no_file()
	{
		string consoleOut;
		var result = parse("no_file.ini", out consoleOut);
		Assert.AreEqual(-1, result);

		Assert.AreEqual("no_file.ini: e=-1 user=0" + Environment.NewLine, consoleOut);
	}
}
