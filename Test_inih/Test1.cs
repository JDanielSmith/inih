using static Test_inih.unittest;

namespace Test_inih;

[TestClass]
public sealed class Test1
{

	[TestMethod]
	public void normal()
	{
		var actual = parse("normal.ini");
		Assert.AreEqual(0, actual);
	}
}
