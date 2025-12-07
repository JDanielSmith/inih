/* inih -- simple .INI file parser

SPDX-License-Identifier: BSD-3-Clause

Copyright (C) 2009-2025, Ben Hoyt

inih is released under the New BSD license (see LICENSE.txt). Go to the project
home page for more info:

https://github.com/benhoyt/inih

*/

/* Nonzero if ini_handler callback should accept lineno parameter. */
#undef INI_HANDLER_LINENO

using Crt;

namespace inih;
public static partial class ini
{
	/* Typedef for prototype of handler function.

	   Note that even though the value parameter has type "const char*", the user
	   may cast to "char*" and modify its content, as the value is not used again
	   after the call to ini_handler. This is not true of section and name --
	   those must not be modified.
	*/
#if INI_HANDLER_LINENO
	typedef int (*ini_handler)(void* user, const char* section,
                           const char* name, const char* value,
                           int lineno);
#else
	public delegate int ini_handler(VoidPointer user, ConstPointer<char> section,
						   ConstPointer<char> name, ConstPointer<char> value);
#endif

	/* Typedef for prototype of fgets-style reader function. */
	public delegate Pointer<char> ini_reader(Pointer<char> str, int num, VoidPointer stream);


	/* Parse given INI-style file. May have [section]s, name=value pairs
	   (whitespace stripped), and comments starting with ';' (semicolon). Section
	   is "" if name=value pair parsed before any section heading. name:value
	   pairs are also supported as a concession to Python's configparser.

	   For each name=value pair parsed, call handler function with given user
	   pointer as well as section, name, and value (data only valid for duration
	   of handler call). Handler should return nonzero on success, zero on error.

	   Returns 0 on success, line number of first error on parse error (doesn't
	   stop on first error), -1 on file open error, or -2 on memory allocation
	   error (only when INI_USE_STACK is zero).
	*/
	public static partial int ini_parse(ConstPointer<char> filename, ini_handler handler, VoidPointer user);

	/* Same as ini_parse(), but takes a FILE* instead of filename. This doesn't
	   close the file when it's finished -- the caller must do that. */
	public static partial int ini_parse_file(Pointer<FILE> file, ini_handler handler, VoidPointer user);

	/* Same as ini_parse(), but takes an ini_reader function pointer instead of
	   filename. Used for implementing custom or string-based I/O (see also
	   ini_parse_string). */
	public static partial int ini_parse_stream(ini_reader reader, VoidPointer stream, ini_handler handler,
						 VoidPointer user);

	/* Same as ini_parse(), but takes a zero-terminated string with the INI data
	   instead of a file. Useful for parsing INI data from a network socket or
	   which is already in memory. */
	public static partial int ini_parse_string(ConstPointer<char> @string, ini_handler handler, VoidPointer user);

	/* Same as ini_parse_string(), but takes a string and its length, avoiding
	   strlen(). Useful for parsing INI data from a network socket or which is
	   already in memory, or interfacing with C++ std::string_view. */
	public static partial int ini_parse_string_length(ConstPointer<char> @string, size_t length, ini_handler handler, VoidPointer user);


	/* Chars that begin a start-of-line comment. Per Python configparser, allow
	   both ; and # comments at the start of a line by default. */
	const string INI_START_COMMENT_PREFIXES = ";#";

	/* Nonzero to allow inline comments (with valid inline comment characters
	   specified by INI_INLINE_COMMENT_PREFIXES). Set to 0 to turn off and match
	   Python 3.2+ configparser behaviour. */
	const string INI_INLINE_COMMENT_PREFIXES = ";";

	/* Maximum line length for any line in INI file (stack or heap). Note that
	   this must be 3 more than the longest line (due to '\r', '\n', and '\0'). */
	const size_t INI_MAX_LINE = (size_t)200;

	/* Initial size in bytes for heap line buffer. Only applies if INI_USE_STACK
	   is zero. */
	const size_t INI_INITIAL_ALLOC = (size_t)200;
}


