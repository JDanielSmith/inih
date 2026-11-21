using Crt;
using Crt.Core;
using static Crt.stdio;
using static Crt.stdlib;
using static Crt.@string;
using static inih.ini;

namespace Test_inih;

internal static partial class unittest
{
	static int[] User_ = [default]; 	static ref int User => ref User_[0];
	static char[] Prev_section = new char[50];

#if INI_HANDLER_LINENO
	int dumper(void* user, const char* section, const char* name,
			   const char* value, int lineno)
#else
	static int dumper(Pointer<int> user, ReadOnlyPointer<char> section, ReadOnlyPointer<char> name,
		   ReadOnlyPointer<char> value)
#endif
	{
		User = x((Pointer<int>)user);
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


	static int parse(ReadOnlyPointer<char> fname) =>  parse_.Invoke(fname);
	static class parse_
	{
		static int[] u_ = [100]; static ref int u => ref u_[0];
		static internal int Invoke(ReadOnlyPointer<char> fname)
		{
			int e;
			Prev_section[0] = '\0';
			e = ini_parse(fname, dumper, AddressOf(u_));
			printf("%s: e=%d user=%d\n", fname, e, User);
			u++;
			return e;
		}

	}

	internal static int parse(string fname)
	{
		return parse(fname.Pointer);
	}
}
