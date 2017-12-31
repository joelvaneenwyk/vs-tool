// ***********************************************************************************************
// (c) 2012 Gavin Pugh http://www.gavpugh.com/ - Released under the open-source zlib license
// ***********************************************************************************************

// Parser for .d gcc dependency files. Fed the .d file, it will output a list of filenames that
// the compiled module was dependent on.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace vs.tool.Build.CPPTasks
{
	public class DepFileParse
	{
		private const int EST_MAX_FILES = 128;

		private List<String> m_dependentFiles = new List<String>(EST_MAX_FILES);

		private StringBuilder m_concatPath = new StringBuilder(Utils.EST_MAX_PATH_LEN);
		private StringBuilder m_finalPath = new StringBuilder(Utils.EST_MAX_PATH_LEN);

		public List<String> DependentFiles 
		{ 
			get 
			{ 
				return this.m_dependentFiles; 
			}
		}

		public DepFileParse( string path )
		{
			using (StreamReader reader = new StreamReader(path, Encoding.ASCII))
			{
				string str = reader.ReadToEnd();
				reader.Close();

			    this.Parse(str);
			}
		}

		private bool IsReturn(char c)
		{
			return ((c == 13) || (c == 10));
		}

		private bool IsWhitespace(char c)
		{
			return (this.IsReturn(c) || (c == ' ') || (c == '\t'));
		}

		private bool IsSlash(char c)
		{
			return ((c == '\\') || (c == '/'));
		}

		private string FixPath( StringBuilder srcPath )
		{
		    this.m_finalPath.Length = 0;
			bool slash = false;

			// Replace any contigious slashes with just one slash. Make it a backslash too.
			for (int i = 0; i < srcPath.Length;i++ )
			{
				char c = srcPath[i];
				if (this.IsSlash(c))
				{
					if (slash == false)
					{
					    this.m_finalPath.Append('\\');
						slash = true;
					}
				}
				else
				{
					slash = false;
				    this.m_finalPath.Append(c);
				}
			}

			// Return an absolute path
			return Path.GetFullPath(this.m_finalPath.ToString());
		}

		private void Parse( string contents )
		{
			// Parses the output string into a list of header files

			// Format is:
			// main.o: main.cpp \
			//  C:/android-ndk-r5/platforms/android-9/arch-arm/usr/include/stdio.h \
			//  C:/android-ndk-r5/platforms/android-9/arch-arm/usr/include/sys/cdefs.h \
			//  C:/android-ndk-r5/platforms/android-9/arch-arm/usr/include/sys/cdefs_elf.h \
			//  C:/android-ndk-r5/platforms/android-9/arch-arm/usr/include/sys/_types.h \
			//  C:/android-ndk-r5/platforms/android-9/arch-arm/usr/include/machine/_types.h \

			// Files with spaces in look like this:
			//  C:\Program\ Files\ (x86)\ARM\Mali\ Developer\ Tools\OpenGL\ ES\ 1.1\ Emulator\ v1.0\include/GLES2/gl2ext.h \

			// Escaping seems to be just limited to spaces. As in the above path, the backslashes need to be treated normally otherwise.

			bool inQuotes = false;
			bool escaped = false;

			foreach (char c in contents)
			{
				bool terminated = false;

				if (escaped)
				{
					escaped = false;

					// Only escaping spaces.
					if ( c == ' ' )
					{
					    this.m_concatPath.Append(c);
						continue;
					}

					if (this.IsWhitespace( c ) && (this.m_concatPath.Length == 0 ) )
					{
						// Skip it, if we're already empty and it's just whitespace
						continue;
					}

				    this.m_concatPath.Append("\\");
				}

				if (c == '\\')
				{
					escaped = true;
					continue;
				}

				if (inQuotes)
				{
					if (c == '\"')
					{
						// Done with quotes. Save this out.
						inQuotes = false;
						terminated = true;
					}
					else
					{
						// In quotes, so anything goes
					    this.m_concatPath.Append(c);
					}
				}
				else
				{
					if (c == '\"')
					{
						// Start quotes?
						inQuotes = true;

					    this.m_concatPath.Length = 0;
					}
					else if (this.IsWhitespace(c))
					{
						// Whitespace. If we have anything, then save it out. Otherwise ignore it
						if (this.m_concatPath.Length > 0)
						{
							terminated = true;
						}
					}
					else
					{
						// Append to this path
					    this.m_concatPath.Append(c);
					}
				}

				if (terminated)
				{
					if (this.m_concatPath.Length > 1 )
					{
						// Ignore the path if it ends with ':', that's going to be the first object file line.
						if (this.m_concatPath[this.m_concatPath.Length - 1] != ':' )
						{
							string pathMade = this.FixPath(this.m_concatPath).ToUpperInvariant();
						    this.m_dependentFiles.Add(pathMade);
						}
					    this.m_concatPath.Length = 0;
					}
				}
			}
		}
	}
}
