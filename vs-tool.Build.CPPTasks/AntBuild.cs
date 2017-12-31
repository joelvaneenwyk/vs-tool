// ***********************************************************************************************
// (c) 2012 Gavin Pugh http://www.gavpugh.com/ - Released under the open-source zlib license
// ***********************************************************************************************

// Apache Ant, Apk Building Task.

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Resources;

using Microsoft.Build.Framework;
using Microsoft.Build.CPPTasks;
using Microsoft.Build.Utilities;

namespace vs.tool.Build.CPPTasks
{
	public class AntBuild : TrackedVCToolTask
	{
		private const string BUILD_LIB_PATH = "libs";
		private const string BUILD_BIN_PATH = "bin";

		private string m_toolFileName;
		private string m_inputSoPath;
		private string m_armEabiSoPath;
		private string m_antOpts;

		private AntBuildParser m_parser = new AntBuildParser();

		public bool BuildingInIDE { get; set; }
		public string JVMHeapInitial { get; set; }
		public string JVMHeapMaximum { get; set; }

		[Required]
		public bool IgnoreJavaOpts { get; set; }

		[Required]
		public string AntBuildPath { get; set; }

		[Required]
		public string AntAndroidSdkPath { get; set; }

		[Required]
		public string AntJavaHomePath { get; set; }

		[Required]
		public string AntBuildType { get; set; }

		[Required]
		public string AntLibraryName { get; set; }

		[Required]
		public string GCCToolPath { get; set; }

		[Required]
		public string ApkLibsPath { get; set; }

		[Required]
		public virtual ITaskItem[] Sources { get; set; }

		[Output]
		public virtual string OutputFile { get; set; }

		[Output]
		public string ApkName { get; set; }

		[Output]
		public string ActivityName { get; set; }

		[Output]
		public string PackageName { get; set; }

		public AntBuild()
			: base(new ResourceManager("vs.tool.Build.CPPTasks.Properties.Resources", Assembly.GetExecutingAssembly()))
		{

		}

		protected override bool ValidateParameters()
		{
		    this.m_toolFileName = Path.GetFileNameWithoutExtension(this.ToolName);

			if ( !this.m_parser.Parse(this.AntBuildPath, this.AntBuildType, this.Log, true ) )
			{
				return false;
			}

		    this.ActivityName = this.m_parser.ActivityName;
		    this.ApkName = this.m_parser.ApkName;
		    this.PackageName = this.m_parser.PackageName;
		    this.OutputFile = this.m_parser.OutputFile;

			// Only one .so library should be input to this task
			if (this.Sources.Length > 1 )
			{
			    this.Log.LogError("More than one .so library being built!");
				return false;
			}

		    this.m_inputSoPath = Path.GetFullPath(this.Sources[0].GetMetadata("FullPath"));

			// Copy the .so file into the correct place
		    this.m_armEabiSoPath = Path.GetFullPath(this.AntBuildPath + "\\" + BUILD_LIB_PATH + "\\" + this.ApkLibsPath + "\\" + this.AntLibraryName + ".so");

		    this.m_antOpts = string.Empty;
			if (this.JVMHeapInitial != null && this.JVMHeapInitial.Length > 0)
			{
			    this.m_antOpts += "-Xms" + this.JVMHeapInitial + "m";
			}
			if (this.JVMHeapMaximum != null && this.JVMHeapMaximum.Length > 0)
			{
				if (this.m_antOpts.Length > 0 )
				{
				    this.m_antOpts += " ";
				}
			    this.m_antOpts += "-Xmx" + this.JVMHeapMaximum + "m";
			}

			return base.ValidateParameters();
		}

		protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
		{
			// Copy over the .so file to the correct directory in the build structure
			Directory.CreateDirectory(this.AntBuildPath + "\\" + BUILD_LIB_PATH + "\\" + this.ApkLibsPath);
			File.Copy(this.m_inputSoPath, this.m_armEabiSoPath, true);

			// Create local properties file from Android SDK Path
		    this.WriteLocalProperties();

			// List of environment variables
			List<String> envList = new List<String>();

			// Set JAVA_HOME for the ant build
		    this.SetEnvVar( envList, "JAVA_HOME", this.AntJavaHomePath );

			// Set ANT_OPTS, if appropriate
			if (this.m_antOpts.Length > 0 )
			{
			    this.SetEnvVar( envList, "ANT_OPTS", this.m_antOpts );
			}

			// Ignore JAVA_OPTS?
			if (this.IgnoreJavaOpts )
			{
			    this.SetEnvVar( envList, "JAVA_OPTS", "" );
			}

			// Set environment variables
			this.EnvironmentVariables = envList.ToArray();

		    this.Log.LogMessage( MessageImportance.High, "{0} {1}", pathToTool, commandLineCommands );            

			return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
		}

		private void SetEnvVar( List<String> envList, string var, string setting )
		{
		    this.Log.LogMessage( MessageImportance.High, "Envvar: {0} is set to '{1}'", var, setting );
			envList.Add( var + "=" + setting );
			Environment.SetEnvironmentVariable( var, setting, EnvironmentVariableTarget.Process );
		}

		private void WriteLocalProperties()
		{
			string localPropsFile = Path.GetFullPath(this.AntBuildPath + "\\local.properties");

			// Need double backslashes for this path
			string sdkPath = Path.GetFullPath(this.AntAndroidSdkPath).Replace( "\\", "\\\\" );

			string fileContents = Properties.Resources.localproperties_ant_file;
			fileContents = fileContents.Replace("{SDKDIR}", sdkPath);
			
			using (StreamWriter outfile = new StreamWriter(localPropsFile))
			{
				outfile.Write(fileContents);
			}
		}

		protected override void RemoveTaskSpecificInputs(CanonicalTrackedInputFiles compactInputs)
		{
			// This is necessary because the VC tracker gets confused by the intermingling of reading and writing by the support apps

			foreach (KeyValuePair<string, Dictionary<string, string>> pair in compactInputs.DependencyTable)
			{
				List<string> delFiles = new List<string>();

				foreach (KeyValuePair<string, string> depFile in pair.Value)
				{
					// Remove the -unaligned.apk file, it shouldn't be in the input list
					if ( depFile.Key.ToLowerInvariant().EndsWith( "-unaligned.apk" ) )
					{
						delFiles.Add(depFile.Key);
					}
					// Same deal with build.prop
					if ( depFile.Key.ToLowerInvariant().EndsWith( "build.prop" ) )
					{
						delFiles.Add( depFile.Key );
					}
				}

				// Do deletions
				foreach (string delFile in delFiles)
				{
					pair.Value.Remove(delFile);
				}

				// Add the two .so files to the inputs
				if ( pair.Value.ContainsKey(this.m_inputSoPath.ToUpperInvariant() ) == false )
				{
					pair.Value.Add(this.m_inputSoPath.ToUpperInvariant(), null );
				}
				if ( pair.Value.ContainsKey(this.m_armEabiSoPath.ToUpperInvariant() ) == false )
				{
					pair.Value.Add(this.m_armEabiSoPath.ToUpperInvariant(), null );
				}
			}
		}

		protected override void RemoveTaskSpecificOutputs(CanonicalTrackedOutputFiles compactOutputs)
		{
			// Find each non-apk output, and delete it
			// This is necessary because the VC tracker gets confused by the intermingling of reading and writing by the support apps

			foreach (KeyValuePair<string, Dictionary<string, DateTime>> pair in compactOutputs.DependencyTable)
			{
				List<string> delFiles = new List<string>();

				foreach (KeyValuePair<string, DateTime> depFile in pair.Value)
				{
					// Remove all non-apk files from the output list
					if (depFile.Key.ToLowerInvariant().EndsWith(".apk") == false)
					{
						delFiles.Add(depFile.Key);
					}
					// But *do* remove the -unaligned.apk from the list.
					if ( depFile.Key.ToLowerInvariant().EndsWith( "-unaligned.apk" ) )
					{
						delFiles.Add( depFile.Key );
					}
				}

				// Do deletions
				foreach (string delFile in delFiles)
				{
					pair.Value.Remove(delFile);
				}
			}
		}

		public override bool AttributeFileTracking
		{
			get
			{
				return true;
			}
		}

		protected override string GetWorkingDirectory()
		{
			return this.AntBuildPath;
		}

		protected override string GenerateCommandLineCommands()
		{
			// Simply 'debug', or 'release'.
			return this.AntBuildType.ToLower();
		}

#if !VS2010DLL && !VS2015DLL && !VS2017DLL
        protected override string GenerateResponseFileCommands(VCToolTask.CommandLineFormat format)
		{
			return string.Empty;
		}
#endif

		protected override string GenerateResponseFileCommands()
		{
			return string.Empty;
		}

		protected override bool MaintainCompositeRootingMarkers
		{
			get
			{
				return true;
			}
		}

		public virtual string PlatformToolset
		{
			get
			{
				return "Ant";
			}
		}

		protected override Encoding ResponseFileEncoding
		{
			get
			{
				return Encoding.ASCII;
			}
		}

		protected override string ToolName
		{
			get
			{
				return this.GCCToolPath;
			}
		}

		protected override ITaskItem[] TrackedInputFiles
		{
			get
			{
				return this.Sources;
			}
		}

		protected override string TrackerIntermediateDirectory
		{
			get
			{
				if (this.TrackerLogDirectory != null)
				{
					return this.TrackerLogDirectory;
				}
				return string.Empty;
			}
		}

		public virtual string TrackerLogDirectory
		{
			get
			{
				if (this.IsPropertySet("TrackerLogDirectory"))
				{
					return this.ActiveToolSwitches["TrackerLogDirectory"].Value;
				}
				return null;
			}
			set
			{
				this.ActiveToolSwitches.Remove("TrackerLogDirectory");
				ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Directory)
				{
					DisplayName = "Tracker Log Directory",
					Description = "Tracker log directory.",
					ArgumentRelationList = new ArrayList(),
					Value = EnsureTrailingSlash(value)
				};
				this.ActiveToolSwitches.Add("TrackerLogDirectory", switch2);
				this.AddActiveSwitchToolValue(switch2);
			}
		}

		protected override string CommandTLogName
		{
			get
			{
				return (this.m_toolFileName + ".command.1.tlog");
			}
		}

		protected override string[] ReadTLogNames
		{
			get
			{
				return new string[] { 
					"cmd-java-zipalign.read.*.tlog", 
					"cmd-java-zipalign.*.read.*.tlog",
					"cmd-java-aapt.read.*.tlog", 
					"cmd-java-aapt.*.read.*.tlog",
					"cmd.read.*.tlog", 
					"cmd.*.read.*.tlog",
					"cmd-java.read.*.tlog", 
					"cmd-java.*.read.*.tlog",
					"java.read.*.tlog", 
					"java.*.read.*.tlog",
				};
			}
		}

		protected override string[] WriteTLogNames
		{
			get
			{
				return new string[] { 
					"cmd-java-zipalign.write.*.tlog", 
					"cmd-java-zipalign.*.write.*.tlog",
					"cmd-java-aapt.write.*.tlog", 
					"cmd-java-aapt.*.write.*.tlog",
					"cmd.write.*.tlog", 
					"cmd.*.write.*.tlog",
					"cmd-java.write.*.tlog", 
					"cmd-java.*.write.*.tlog",
					"java.write.*.tlog", 
					"java.*.write.*.tlog",
				};
			}
		}


	}


}
