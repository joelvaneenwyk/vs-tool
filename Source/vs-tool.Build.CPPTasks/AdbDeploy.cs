// ***********************************************************************************************
// (c) 2012 Gavin Pugh http://www.gavpugh.com/ - Released under the open-source zlib license
// ***********************************************************************************************

// Apache Ant, Apk Building Task.

using System.Text;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Diagnostics;

using Microsoft.Build.Framework;
using Microsoft.Build.CPPTasks;
using Microsoft.Build.Utilities;

namespace vs.tool.Build.CPPTasks
{
	public class AdbDeploy : TrackedVCToolTask
	{
		public bool BuildingInIDE { get; set; }

		[Required]
		public string AntBuildPath { get; set; }

		[Required]
		public string AntBuildType { get; set; }

		[Required]
		public string AdbPath { get; set; }

		[Required]
		public string Params { get; set; }

		[Required]
		public string DeviceArgs { get; set; }

		public string GenerateCmdFilePath { get; set; }

		private AntBuildParser m_parser = new AntBuildParser();

		private string m_toolFileName;

		public AdbDeploy()
			: base(new ResourceManager("vs.tool.Build.CPPTasks.Properties.Resources", Assembly.GetExecutingAssembly()))
		{

		}

		private void WriteDebugRunCmdFile()
		{
			string destCmdFile = Path.GetFullPath(this.GenerateCmdFilePath);

			using (StreamWriter outfile = new StreamWriter(destCmdFile))
			{
				outfile.Write(string.Format("{0} {1} shell am start -n {2}/{3}\n", this.AdbPath, this.MakeStringReplacements(this.DeviceArgs), this.m_parser.PackageName, this.m_parser.ActivityName));
			}
		}

		protected override bool ValidateParameters()
		{
		    this.m_toolFileName = Path.GetFileNameWithoutExtension(this.ToolName);

			if ( !this.m_parser.Parse(this.AntBuildPath, this.AntBuildType, this.Log, false ) )
			{
				return false;
			}

			return base.ValidateParameters();
		}

		public override void Cancel()
		{
			Process.Start(this.AdbPath, "kill-server");

			base.Cancel();
		}

		public override bool Execute()
		{
			return base.Execute();
		}

		protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
		{
		    this.Log.LogMessage(MessageImportance.High, "{0} {1}", pathToTool, commandLineCommands);

			if ( (this.GenerateCmdFilePath != null ) && (this.GenerateCmdFilePath.Length > 0 ) )
			{
			    this.WriteDebugRunCmdFile();
			}

			if ( commandLineCommands.Contains( "wait-for-device" ) || commandLineCommands.Contains( "start-server" ) )
			{
				// Hack to spawn a process, instead of waiting on it
				Process.Start( pathToTool, commandLineCommands );
				return 0;
			}
			else
			{
				return base.ExecuteTool( pathToTool, responseFileCommands, commandLineCommands );
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

		private string MakeStringReplacements( string theString )
		{
			string paramCopy = theString;
			paramCopy = paramCopy.Replace("{PackageName}", this.m_parser.PackageName);
			paramCopy = paramCopy.Replace("{ApkPath}", "\"" + this.m_parser.OutputFile + "\"");
			paramCopy = paramCopy.Replace("{ActivityName}", this.m_parser.ActivityName);
			return paramCopy.Trim();
		}

		protected override string GenerateCommandLineCommands()
		{
			return (this.MakeStringReplacements(this.DeviceArgs) + " " + this.MakeStringReplacements(this.Params)).Trim();
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
				return "Adb";
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
				return this.AdbPath;
			}
		}

		protected override ITaskItem[] TrackedInputFiles
		{
			get
			{
				return new TaskItem[] { new TaskItem(this.m_parser.OutputFile) };
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
				return new string[] { (this.m_toolFileName + ".read.*.tlog"), (this.m_toolFileName + ".*.read.*.tlog") };
			}
		}

		protected override string[] WriteTLogNames
		{
			get
			{
				return new string[] { (this.m_toolFileName + ".write.*.tlog"), (this.m_toolFileName + ".*.write.*.tlog") };
			}
		}

	}


}
