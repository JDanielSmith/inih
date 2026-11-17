using Crt;
using System.Xml.Linq;
using static Crt.stdio;
using static Crt.stdlib;
using static Crt.@string;
using static inih.ini;

namespace Test_inih;

internal static partial class unittest
{
	static int[] User = [0];
	static char[] Prev_section = new char[50];

#if INI_HANDLER_LINENO
	int dumper(void* user, const char* section, const char* name,
			   const char* value, int lineno)
#else
	static int dumper(VoidPointer user, ConstPointer<char> section, ConstPointer<char> name,
		   ConstPointer<char> value)
#endif
	{
		User[0] = x((Pointer<int>)user);
		if (!name || (bool)strcmp(section, Prev_section))
		{
			printf("... [%s]\n", section);
			strncpy(Prev_section, section, @sizeof(Prev_section));
			Prev_section[@sizeof(Prev_section) - 1] = '\0';
		}
		if (!name)
		{
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


	static int parse(ConstPointer<char> fname) => new parse_(fname).Invoke();
	sealed class parse_(ConstPointer<char> fname)
	{
		static int[] u = [100];
		internal int Invoke()
		{
			int e;
			Prev_section[0] = '\0';
			e = ini_parse(fname, dumper, AddressOf(u));
			if (e == -1)
			{
				printf("%s: can't open file\n", fname);
				return e;
			}
			printf("%s: e=%d user=%d\n", fname, e, User);
			u[0]++;
			return e;
		}

	}

	internal static int parse(string fname)
	{
		return parse(fname.AsConstPointer());
	}
}
