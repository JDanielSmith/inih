using Crt;
using static Crt.stdio;
using static Crt.stdlib;
using static Crt.@string;

using static inih.ini;

namespace Test_inih;

[TestClass]
public sealed class Test1
{
	static int User;
	static char[] Prev_section = new char[50];

	#if INI_HANDLER_LINENO
	int dumper(void* user, const char* section, const char* name,
			   const char* value, int lineno)
	#else
	static int dumper(VoidPointer user, ConstPointer<char> section, ConstPointer<char> name,
		   ConstPointer<char> value)
	#endif
	{

		User = x((Pointer<int>) user);
		if (!name || (bool)strcmp(section, Prev_section)) {
			printf("... [%s]\n", section);
		strncpy(Prev_section, section, @sizeof(Prev_section));
		Prev_section[@sizeof(Prev_section) - 1] = '\0';
		}
		if (!name) {
			return 1;
		}

		#if INI_HANDLER_LINENO
		printf("... %s%s%s;  line %d\n", name, value ? "=" : "", value ? value : "", lineno);
		#else
		printf("... %s%s%s;\n", name, value ? "=" : "", value ? value : "");
		#endif

		if (!value)
		{
			/* Happens when INI_ALLOW_NO_VALUE=1 and line has no value (no '=' or ':') */
			return 1;
		}

		return strcmp(name, "user") == 0 && strcmp(value, "parse_error") == 0 ? 0 : 1;
	}


	static int[] parse_u = [100];

	static int parse(ConstPointer<char> fname)
	{
		int e;

		Prev_section[0] = '\0';
		e = ini_parse(fname, dumper, AddressOf(parse_u));
		if (e == -1)
		{
			printf("%s: can't open file\n", fname);
			return e;
		}
		printf("%s: e=%d user=%d\n", fname, e, User);
		parse_u[0]++;
		return e;
	}
	static int parse(string fname)
	{
		return parse(fname.AsConstPointer());
	}

	[TestMethod]
	public void normal()
	{
		var actual = parse("normal.ini");
		Assert.AreEqual(0, actual);
	}
}
