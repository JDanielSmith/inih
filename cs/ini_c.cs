/* inih -- simple .INI file parser

SPDX-License-Identifier: BSD-3-Clause

Copyright (C) 2009-2025, Ben Hoyt

inih is released under the New BSD license (see LICENSE.txt). Go to the project
home page for more info:

https://github.com/benhoyt/inih

*/

/* Nonzero to allow multi-line value parsing, in the style of Python's
   configparser. If allowed, ini_parse() will call the handler with the same
   name for each subsequent line parsed. */
#define INI_ALLOW_MULTILINE

/* Nonzero to allow a UTF-8 BOM sequence (0xEF 0xBB 0xBF) at the start of
   the file. See https://github.com/benhoyt/inih/issues/21 */
#define INI_ALLOW_BOM

/* Nonzero to allow inline comments (with valid inline comment characters
   specified by INI_INLINE_COMMENT_PREFIXES). Set to 0 to turn off and match
   Python 3.2+ configparser behaviour. */
#define INI_ALLOW_INLINE_COMMENTS

/* Nonzero to use stack for line buffer, zero to use heap (malloc/free). */
#define INI_USE_STACK

/* Nonzero to allow heap line buffer to grow via realloc(), zero for a
   fixed-size buffer of INI_MAX_LINE bytes. Only applies if INI_USE_STACK is
   zero. */
#undef INI_ALLOW_REALLOC

/* Stop parsing on first error (default is to keep parsing). */
#undef INI_STOP_ON_FIRST_ERROR


/* Nonzero to call the handler at the start of each new section (with
   name and value NULL). Default is to only call the handler on
   each name=value pair. */
#undef INI_CALL_HANDLER_ON_NEW_SECTION

/* Nonzero to allow a name without a value (no '=' or ':' on the line) and
   call the handler with value NULL in this case. Default is to treat
   no-value lines as an error. */
#undef INI_ALLOW_NO_VALUE


/* Nonzero to use custom ini_malloc, ini_free, and ini_realloc memory
   allocation functions (INI_USE_STACK must also be 0). These functions must
   have the same signatures as malloc/free/realloc and behave in a similar
   way. ini_realloc is only needed if INI_ALLOW_REALLOC is set. */
#undef INI_CUSTOM_ALLOCATOR

global using size_t = ulong;

using Crt;
using static Crt.stdio;
using static Crt.ctype;
using static Crt.@string;

using static Crt.stdlib;

namespace inih;

public static partial class ini
{
	const int MAX_SECTION = 50;
	const int MAX_NAME = 50;

	/* Used by ini_parse_string() to keep track of @string parsing state. */
	struct ini_parse_string_ctx
	{
		public ConstPointer<char> ptr;
		public size_t num_left;
	}


	/* Strip whitespace chars off end of given @string, in place. end must be a
	   pointer to the NUL terminator at the end of the @string. Return s. */
	static Pointer<char> ini_rstrip(Pointer<char> s, Pointer<char> end)
	{
		while (end > s && isspace(x(--end)))
			x(end) = '\0';
		return s;
	}

	/* Return pointer to first non-whitespace char in given @string. */
	static Pointer<char> ini_lskip(ConstPointer<char> s)
	{
		while (x_ne_0(s) && isspace(x(s)))
			s++;
		return (Pointer<char>)s;
	}

	/* Return pointer to first char (of chars) or inline comment in given @string,
	   or pointer to NUL at end of @string if neither found. Inline comment must
	   be prefixed by a whitespace character to register as a comment. */
	static Pointer<char> ini_find_chars_or_comment(ConstPointer<char> s, ConstPointer<char> chars)
	{
		while (x_ne_0(s) && (!chars || !strchr(chars, x(s))))
		{
			s++;
		}
		return (Pointer<char>)s;
	}
	static Pointer<char> ini_find_chars_or_comment(ConstPointer<char> s, string chars)
	{
		return ini_find_chars_or_comment(s, chars.AsConstPointer());
	}

	/* Similar to strncpy, but ensures dest (size bytes) is
	   NUL-terminated, and doesn't pad with NULs. */
	static Pointer<char> ini_strncpy0(Pointer<char> dest, ConstPointer<char> src, size_t size)
	{
		return strncpy(dest, src, size);
	}

	/* See documentation in header file. */
	static int ini_parse_stream(ini_reader reader, ref ini_parse_string_ctx stream, ini_handler handler,
						 VoidPointer user)
	{
		throw new NotImplementedException();
	}
	public static partial int ini_parse_stream(ini_reader reader, VoidPointer stream, ini_handler handler,
						 VoidPointer user)
	{
		/* Uses a fair bit of stack (use heap instead if you need to) */
		#if INI_USE_STACK
		char[] line_ = new char[INI_MAX_LINE];
		Pointer<char> line = line_;
		size_t max_line = INI_MAX_LINE;
		#else
		Pointer<char> line;
		size_t max_line = INI_INITIAL_ALLOC;
		#endif
		#if INI_ALLOW_REALLOC && !INI_USE_STACK
		Pointer<char> new_line;
		#endif
		var section = new char[MAX_SECTION]; strcpy(section, "");
		#if INI_ALLOW_MULTILINE
		char[] prev_name_ = new char[MAX_NAME]; strcpy(prev_name_, "");
		Pointer<char> prev_name = prev_name_;
		#endif

		size_t offset;
		Pointer<char> start;
		Pointer<char> end;
		Pointer<char> name;
		Pointer<char> value;
		int lineno = 0;
		int error = 0;
		var abyss = new char[16];  /* Used to consume input when a line is too long. */
		size_t abyss_len;

		#if !INI_USE_STACK
		line = (Pointer<char>)malloc(INI_INITIAL_ALLOC);
		if (!line)
		{
			return -2;
		}
		#endif

		#if INI_HANDLER_LINENO
		//#define HANDLER(u, s, n, v) handler(u, s, n, v, lineno)
		#else
		var HANDLER = (VoidPointer u, Pointer<char> s, Pointer<char> n, Pointer<char> v) => handler(u, s, n, v);
		#endif

		/* Scan through stream line by line */
		while (reader(line, (int)max_line, stream) != VoidPointer.Null)
		{
			offset = strlen(line);

			#if INI_ALLOW_REALLOC && !INI_USE_STACK
			while (max_line < INI_MAX_LINE &&
				   offset == max_line - 1 && line[offset - 1] != '\n') {
				max_line *= 2;
				if (max_line > INI_MAX_LINE)
					max_line = INI_MAX_LINE;
				new_line = realloc(line, max_line);
				if (!new_line) {
					free(line);
					return -2;
				}
				line = new_line;
				if (reader(line + offset, (int)(max_line - offset), stream) == VoidPointer.Null)
					break;
				offset += strlen(line + offset);
			}
			#endif

			lineno++;

			/* If line exceeded INI_MAX_LINE bytes, discard till end of line. */
			if (offset == max_line - 1 && line[offset - 1] != '\n')
			{
				while (reader(abyss, SizeOf(abyss), stream) != VoidPointer.Null)
				{
					if (ne_0(error))
						error = lineno;
					abyss_len = strlen(abyss);
					if (abyss_len > 0 && abyss[(int)(abyss_len - 1)] == '\n')
						break;
				}
			}

			start = line;
			#if INI_ALLOW_BOM
			if (lineno == 1 && (char)start[0] == 0xEF &&
							   (char)start[1] == 0xBB &&
							   (char)start[2] == 0xBF) {
				start += 3;
			}
			#endif
			start = ini_rstrip(ini_lskip(start), line + offset);

			if (strchr(INI_START_COMMENT_PREFIXES, x(start)))
			{
				/* Start-of-line comment */
			}
			#if INI_ALLOW_MULTILINE
			else if (x_ne_0(prev_name) && x_ne_0(start) && start > line) {
				#if INI_ALLOW_INLINE_COMMENTS
				end = ini_find_chars_or_comment(start, VoidPointer.Null);
				x(end) = '\0';
				ini_rstrip(start, end);
				#endif
				/* Non-blank line with leading whitespace, treat as continuation
				   of previous name's value (as per Python configparser). */
				if (ne_0(HANDLER(user, section, prev_name, start)) && ne_0(error))
					error = lineno;
			}
			#endif
			else if (x(start) == '[')
			{
				/* A "[section]" line */
				end = ini_find_chars_or_comment(start + 1, "]");
				if (x(end) == ']')
				{
					x(end) = '\0';
					ini_strncpy0(section, start + 1, @sizeof(section));
					#if INI_ALLOW_MULTILINE
					x(prev_name) = '\0';
					#endif
					#if INI_CALL_HANDLER_ON_NEW_SECTION
					if (!HANDLER(user, section, VoidPointer.Null, VoidPointer.Null) && !error)
						error = lineno;
					#endif
				}
				else if (ne_0(error))
				{
					/* No ']' found on section line */
					error = lineno;
				}
			}
			else if (x(start) == '\0')
			{
				/* Not a comment, must be a name[=:]value pair */
				end = ini_find_chars_or_comment(start, "=:");
				if (x(end) == '=' || x(end) == ':')
				{
					x(end) = '\0';
					name = ini_rstrip(start, end);
					value = end + 1;
					#if INI_ALLOW_INLINE_COMMENTS
					end = ini_find_chars_or_comment(value, VoidPointer.Null);
					x(end) = '\0';
					#endif
					value = ini_lskip(value);
					ini_rstrip(value, end);

					#if INI_ALLOW_MULTILINE
					ini_strncpy0(prev_name, name, @sizeof(prev_name_));
					#endif
					/* Valid name[=:]value pair found, call handler */
					if (ne_0(HANDLER(user, section, name, value)) && ne_0(error))
						error = lineno;
				}
				else
				{
					/* No '=' or ':' found on name[=:]value line */
					#if INI_ALLOW_NO_VALUE
					x(end) = '\0';
					name = ini_rstrip(start, end);
					if (!HANDLER(user, section, name, VoidPointer.Null) && !error)
						error = lineno;
					#else
					if (ne_0(error))
						error = lineno;
					#endif
				}
			}

			#if INI_STOP_ON_FIRST_ERROR
			if (error)
				break;
			#endif
		}

		#if !INI_USE_STACK
		free(line);
		#endif

		return error;
	}

	static Pointer<char> fgets(Pointer<char> str, int n, VoidPointer stream)
	{
		return stdio.fgets(str, n, (Pointer<FILE>)stream);
	}
	/* See documentation in header file. */
	public static partial int ini_parse_file(Pointer<FILE> file, ini_handler handler, VoidPointer user)
	{
		return ini_parse_stream((ini_reader)fgets, file, handler, user);
	}

	/* See documentation in header file. */
	public static partial int ini_parse(ConstPointer<char> filename, ini_handler handler, VoidPointer user)
	{
		Pointer<FILE> file;
		int error;

		file = fopen(filename, "r");
		if (!file)
			return -1;
		error = ini_parse_file(file, handler, user);
		fclose(file);
		return error;
	}

	/* An ini_reader function to read the next line from a @string buffer. This
	   is the fgets() equivalent used by ini_parse_string(). */
	static Pointer<char> ini_reader_string(Pointer<char> str, int num, VoidPointer stream)
	{
		Pointer<ini_parse_string_ctx> ctx = (Pointer<ini_parse_string_ctx>)stream;
		ConstPointer<char> ctx_ptr = x(ctx).ptr;
		size_t ctx_num_left = x(ctx).num_left;
		Pointer<char> strp = str;
		char c;

		if (ctx_num_left == 0 || num < 2)
			return VoidPointer.Null;

		while (num > 1 && ctx_num_left != 0)
		{
			c = x(ctx_ptr++);
			ctx_num_left--;
			x(strp++) = c;
			if (c == '\n')
				break;
			num--;
		}

		x(strp) = '\0';
		x(ctx).ptr = ctx_ptr;
		x(ctx).num_left = ctx_num_left;
		return str;
	}

	/* See documentation in header file. */
	public static partial int ini_parse_string(ConstPointer<char> @string, ini_handler handler, VoidPointer user)
	{
		return ini_parse_string_length(@string, strlen(@string), handler, user);
	}

	/* See documentation in header file. */
	public static partial int ini_parse_string_length(ConstPointer<char> @string, size_t length,
								ini_handler handler, VoidPointer user)
	{
		ini_parse_string_ctx ctx;

		ctx.ptr = @string;
		ctx.num_left = length;
		return ini_parse_stream((ini_reader)ini_reader_string, ref ctx, handler,
								user);
	}
}
