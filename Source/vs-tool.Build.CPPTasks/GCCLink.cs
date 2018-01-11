// ***********************************************************************************************
// (c) 2012 Gavin Pugh http://www.gavpugh.com/ - Released under the open-source zlib license
// ***********************************************************************************************

// GCC Linker task. Switches are data-driven via PropXmlParse.

using System;
using System.Text;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Resources;

using Microsoft.Build.Framework;
using Microsoft.Build.CPPTasks;

namespace vs.tool.Build.CPPTasks
{
    public class GCCLink : TrackedVCToolTask
    {
        private string m_toolFileName;
        private PropXmlParse m_propXmlParse;

        public bool BuildingInIDE { get; set; }

        [Required]
        public string GCCToolPath { get; set; }

        [Required]
        public string PropertyXmlFile { get; set; }

        [Required]
        public string EchoCommandLines { get; set; }

        [Required]
        public virtual string OutputFile { get; set; }

        [Required]
        public virtual ITaskItem[] Sources { get; set; }


        public GCCLink()
            : base(new ResourceManager("vs.tool.Build.CPPTasks.Properties.Resources", Assembly.GetExecutingAssembly()))
        {

        }

        protected override bool ValidateParameters()
        {
            if (this.m_propXmlParse != null)
                return true;

            this.m_propXmlParse = new PropXmlParse(this.PropertyXmlFile);

            if (!string.IsNullOrEmpty(this.GCCToolPath))
                this.m_toolFileName = Path.GetFileNameWithoutExtension(this.GCCToolPath);

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
            StringBuilder templateStr = new StringBuilder(Utils.EST_MAX_CMDLINE_LEN);

            if (this.Sources != null && this.Sources.Length > 0)
            {
                foreach (ITaskItem sourceFile in this.Sources)
                {
                    if (sourceFile != null)
                    {
                        templateStr.Append(Utils.PathSanitize(sourceFile.GetMetadata("Identity")));
                        templateStr.Append(" ");
                    }
                }

                ValidateParameters();
                templateStr.Append(this.m_propXmlParse.ProcessProperties(this.Sources[0]));
            }

            return templateStr.ToString();
        }

        private void CleanUnusedTLogFiles()
        {
            // These tlog files are seemingly unused dep-wise, but cause problems when I add them to the proper TLog list
            // Incremental builds keep appending to them, so this keeps them from just growing and growing.
            if (!string.IsNullOrEmpty(this.TrackerLogDirectory) && !string.IsNullOrEmpty(this.m_toolFileName) &&
                Directory.Exists(this.TrackerLogDirectory))
            {
                string ignoreReadLogPath = Path.GetFullPath(this.TrackerLogDirectory + "\\" + this.m_toolFileName + ".read.1.tlog");
                string ignoreWriteLogPath = Path.GetFullPath(this.TrackerLogDirectory + "\\" + this.m_toolFileName + ".write.1.tlog");

                try
                {
                    File.Delete(ignoreReadLogPath);
                    File.Delete(ignoreWriteLogPath);
                }
                catch
                {
                    // Safe to ignore this...
                }
            }
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            this.CleanUnusedTLogFiles();

            if (this.EchoCommandLines == "true")
            {
                this.Log.LogMessage(MessageImportance.High, pathToTool + " " + responseFileCommands);
            }

            int returnValue = 0;

            try
            {
                returnValue = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
            }
            catch (Exception ex)
            {
                this.Log.LogWarning("ExecuteTool returned an exception.");
                this.Log.LogWarning(ex.ToString());
            }

            return returnValue;
        }

        // Called when linker outputs a line
        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            base.LogEventsFromTextOutput(Utils.GCCOutputReplace(singleLine), messageImportance);
        }

        protected override void PostProcessSwitchList()
        {

        }

        public override bool AttributeFileTracking
        {
            get
            {
                return true;
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
            set
            {
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
                return this.m_toolFileName + "-link.command.1.tlog";
            }
        }

        protected override string[] ReadTLogNames
        {
            get
            {
                return new[] {
                    this.m_toolFileName + "-collect2.read.*.tlog",
                    this.m_toolFileName + "-collect2.*.read.*.tlog",
                    this.m_toolFileName + "-collect2-ld.read.*.tlog",
                    this.m_toolFileName + "-collect2-ld.*.read.*.tlog"
                };
            }
        }

        protected override string[] WriteTLogNames
        {
            get
            {
                return new string[] {
                    this.m_toolFileName + "-collect2.write.*.tlog",
                    this.m_toolFileName + "-collect2.*.write.*.tlog",
                    this.m_toolFileName + "-collect2-ld.write.*.tlog",
                    this.m_toolFileName + "-collect2-ld.*.write.*.tlog"
                };
            }
        }
    }


}
