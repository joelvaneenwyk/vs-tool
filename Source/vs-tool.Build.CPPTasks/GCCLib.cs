// ***********************************************************************************************
// (c) 2012 Gavin Pugh http://www.gavpugh.com/ - Released under the open-source zlib license
// ***********************************************************************************************

// GCC Static library Building task. No supported switches currently.

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
    public class GCCLib : TrackedVCToolTask
    {
        private string m_toolFileName;
        private PropXmlParse m_propXmlParse;

        public bool BuildingInIDE { get; set; }

        [Required]
        public string GCCToolPath { get; set; }

        [Required]
        public string PropertyXmlFile { get; set; }

        [Required]
        public virtual string OutputFile { get; set; }

        [Required]
        public string EchoCommandLines { get; set; }

        [Required]
        public virtual ITaskItem[] Sources { get; set; }


        public GCCLib()
            : base(new ResourceManager("vs.tool.Build.CPPTasks.Properties.Resources", Assembly.GetExecutingAssembly()))
        {
        }

        protected override bool ValidateParameters()
        {
            if (this.m_propXmlParse != null)
                return true;

            this.m_propXmlParse = new PropXmlParse(this.PropertyXmlFile);

            if (!string.IsNullOrEmpty(this.ToolName))
                this.m_toolFileName = Path.GetFileNameWithoutExtension(this.ToolName);

            return base.ValidateParameters();
        }

#if !VS2010DLL && !VS2015DLL && !VS2017DLL
        protected override string GenerateResponseFileCommands(VCToolTask.CommandLineFormat format)
        {
            return GenerateResponseFileCommands();
        }
#endif

        protected override string GenerateResponseFileCommands()
        {
            StringBuilder builder = new StringBuilder(Utils.EST_MAX_CMDLINE_LEN);

            ValidateParameters();
            string result = "";
            if (this.Sources != null && this.Sources.Length > 0)
                result = this.m_propXmlParse.ProcessProperties(this.Sources[0]);
            if (string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(this.OutputFile))
            {
                builder.Append("rcs " + Utils.PathSanitize(this.OutputFile) + " ");
            }

            if (this.Sources != null)
            {
                foreach (ITaskItem item in this.Sources)
                {
                    if (item != null)
                    {
                        builder.Append(Utils.PathSanitize(item.ToString()) + " ");
                    }
                }
            }

            if (!string.IsNullOrEmpty(result))
            {
                builder.Append(result);
            }

            return builder.ToString();
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            int returnValue = 0;

            try
            {
                if (this.EchoCommandLines == "true")
                {
                    this.Log.LogMessage(MessageImportance.High, pathToTool + " " + responseFileCommands);
                }

                returnValue = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
            }
            catch
            {
                // We sometimes get the following callstack, but it seems like you can safely ignore this.
                //
                //  System.NullReferenceException: Object reference not set to an instance of an object.
                //  at Microsoft.Build.CPPTasks.VCToolTask.GenerateResponseFileCommandsExceptSwitches(String[] switchesToRemove, CommandLineFormat format, EscapeFormat escapeFormat)
                //  at Microsoft.Build.CPPTasks.VCToolTask.GenerateResponseFileCommands(CommandLineFormat format, EscapeFormat escapeFormat)
                //  at Microsoft.Build.CPPTasks.VCToolTask.GenerateCommandLine(CommandLineFormat format, EscapeFormat escapeFormat)
                //  at Microsoft.Build.CPPTasks.TrackedVCToolTask.PostExecuteTool(Int32 exitCode)
                //  at Microsoft.Build.CPPTasks.TrackedVCToolTask.ExecuteTool(String pathToTool, String responseFileCommands, String commandLineCommands)
                //  at vs.tool.Build.CPPTasks.GCCLib.ExecuteTool(String pathToTool, String responseFileCommands, String commandLineCommands) in D:\Perforce\2018_1_HTML5\ThirdParty\sdks\win32\vs - tool\Source\vs - tool.Build.CPPTasks\GCCLib.cs:line 112
            }

            return returnValue;
        }

        protected override void RemoveTaskSpecificOutputs(CanonicalTrackedOutputFiles compactOutputs)
        {
            // Incremental builds, for whatever reason leave the .a lib output file out of this file.
            // This clears the list (which either is empty, or already has it), and puts it back in.

            foreach (KeyValuePair<string, Dictionary<string, DateTime>> pair in compactOutputs.DependencyTable)
            {
                pair.Value.Clear();
                pair.Value.Add(Path.GetFullPath(this.OutputFile).ToUpperInvariant(), DateTime.Now);
            }
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
                return "GCC";
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
                ValidateParameters();
                return (this.m_toolFileName + ".command.1.tlog");
            }
        }

        protected override string[] ReadTLogNames
        {
            get
            {
                ValidateParameters();
                return new[] { (this.m_toolFileName + ".read.*.tlog"), (this.m_toolFileName + ".*.read.*.tlog") };
            }
        }

        protected override string[] WriteTLogNames
        {
            get
            {
                ValidateParameters();
                return new[] { (this.m_toolFileName + ".write.*.tlog"), (this.m_toolFileName + ".*.write.*.tlog") };
            }
        }
    }


}
