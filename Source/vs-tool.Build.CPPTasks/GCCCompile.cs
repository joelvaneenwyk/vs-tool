﻿// ***********************************************************************************************
// (c) 2012 Gavin Pugh http://www.gavpugh.com/ - Released under the open-source zlib license
// ***********************************************************************************************

// GCC Code Compilation task. Switches are data-driven via PropXmlParse.
//
// Note that this inherits from 'CustomTrackedVCToolTask', rather than 'TrackedVCToolTask' because
// it re-implements the functionality of it's ExecuteTool() and wishes to drop down to to the base
// VCToolTask, rather than use what's in 'TrackedVCToolTask'.
//
// In retrospect, possibly just inheriting from VCToolTask instead is easier here?

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
    public class GCCCompile : CustomTrackedVCToolTask
    {
        private string m_toolfilename;
        private ITaskItem m_currentSourceItem;
        private PropXmlParse m_propXmlParse;

        public bool BuildingInIDE { get; set; }

        [Required]
        public string GCCToolPath { get; set; }

        [Required]
        public string PropertyXmlFile { get; set; }

        [Required]
        public string EchoCommandLines { get; set; }

        [Required]
        public ITaskItem[] Sources { get; set; }


        public GCCCompile()
            : base(new ResourceManager("vs.tool.Build.CPPTasks.Properties.Resources", Assembly.GetExecutingAssembly()))
        {

        }

        protected override ITaskItem[] AssignOutOfDateSources(ITaskItem[] sources)
        {
            this.Sources = sources;
            return sources;
        }

        protected override bool ComputeOutOfDateSources()
        {
            // Same as base class, other than the things commented out.
            // We're not using CustomTrackedVCToolTask's File Tracker, we're using our own. Hence these changes.

            if (this.TrackerIntermediateDirectory != null)
            {
                string trackerIntermediateDirectory = this.TrackerIntermediateDirectory;
            }
            if (this.MinimalRebuildFromTracking || this.TrackFileAccess)
            {
                this.AssignDefaultTLogPaths();
            }
            if (this.MinimalRebuildFromTracking && !this.ForcedRebuildRequired())
            {
                CanonicalTrackedOutputFiles outputs = new CanonicalTrackedOutputFiles(this, this.TLogWriteFiles);
                this.SourceDependencies = new CanonicalTrackedInputFiles(this, this.TLogReadFiles, this.Sources /*TrackedInputFiles*/, this.ExcludedInputPaths, outputs, /*this.UseMinimalRebuildOptimization*/ true, /*this.MaintainCompositeRootingMarkers*/ false);
                ITaskItem[] sourcesOutOfDateThroughTracking = this.SourceDependencies.ComputeSourcesNeedingCompilation(/*false*/);
                List<ITaskItem> sourcesWithChangedCommandLines = this.GenerateSourcesOutOfDateDueToCommandLine();
                this.SourcesCompiled = this.MergeOutOfDateSourceLists(sourcesOutOfDateThroughTracking, sourcesWithChangedCommandLines);
                if (this.SourcesCompiled.Length == 0)
                {
                    this.SkippedExecution = true;
                    return this.SkippedExecution;
                }
                this.SourcesCompiled = this.AssignOutOfDateSources(this.SourcesCompiled);
                this.SourceDependencies.RemoveEntriesForSource(this.SourcesCompiled);
                this.SourceDependencies.SaveTlog();
                outputs.RemoveEntriesForSource(this.SourcesCompiled);
                outputs.SaveTlog();
            }
            else
            {
                this.SourcesCompiled = this.TrackedInputFiles;
                if ((this.SourcesCompiled == null) || (this.SourcesCompiled.Length == 0))
                {
                    this.SkippedExecution = true;
                    return this.SkippedExecution;
                }
            }

            //			if (this.TrackFileAccess)
            //			{
            //				this.RootSource = FileTracker.FormatRootingMarker(this.SourcesCompiled);
            //			}

            this.SkippedExecution = false;
            return this.SkippedExecution;
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            if (!File.Exists(pathToTool))
            {
                this.Log.LogMessageFromText("Could not find GCC compiler: " + pathToTool, MessageImportance.High);
                return -1;
            }

            int retCode = 0;
            try
            {
                retCode = this.CompileWithGCC(pathToTool);
            }
            catch (Exception ex)
            {
                this.Log.LogWarning(ex.ToString());
            }
            finally
            {
                try
                {
                    // Update tlog files
                    CanonicalTrackedOutputFiles outputs = this.ConstructWriteTLog(this.SourcesCompiled);
                    this.ConstructReadTLog(this.SourcesCompiled, outputs);
                    this.ConstructCommandTLog(this.SourcesCompiled, null);
                }
                catch (Exception ex)
                {
                    this.Log.LogWarning(ex.ToString());
                }
            }

            return retCode;
        }

        protected override List<ITaskItem> GenerateSourcesOutOfDateDueToCommandLine()
        {
            // Original TrackedVCToolTask code - START
            IDictionary<string, string> dictionary = this.MapSourcesToCommandLines();
            List<ITaskItem> list = new List<ITaskItem>();
            if (dictionary.Count == 0)
            {
                foreach (ITaskItem item in this.TrackedInputFiles)
                {
                    list.Add(item);
                }
                return list;
            }
            // Original TrackedVCToolTask code - END

            // Check the command lines for each source file. We'll build up a list of those with changed command lines, or those
            // that didn't exist in the original command tlog file at all.
            StringBuilder cmdLine = new StringBuilder(Utils.EST_MAX_CMDLINE_LEN);
            foreach (ITaskItem sourceFile in this.Sources)
            {
                if (sourceFile == null)
                    continue;

                cmdLine.Length = 0;
                this.m_currentSourceItem = sourceFile;

                try
                {
                    cmdLine.Append(this.GenerateCommandLine());
                }
                catch
                {
                    // Not actually clear what causes this but it is safe to ignore
                }

                cmdLine.Append(" ");
                cmdLine.Append(sourceFile.GetMetadata("FullPath").ToUpperInvariant());

                string findCmdLine = null;
                if (dictionary.TryGetValue(FileTracker.FormatRootingMarker(sourceFile), out findCmdLine))
                {
                    if (findCmdLine == null || !cmdLine.ToString().Equals(findCmdLine, StringComparison.Ordinal))
                    {
                        list.Add(sourceFile);
                    }
                }
                else
                {
                    list.Add(sourceFile);
                }
            }

            return list;
        }

        protected override bool ValidateParameters()
        {
            this.m_propXmlParse = new PropXmlParse(this.PropertyXmlFile);

            this.m_toolfilename = Path.GetFileNameWithoutExtension(this.ToolName);

            return base.ValidateParameters();
        }

        private int CompileWithGCC(string pathToTool)
        {
            int retCode = 0;
            bool hitError = false;

            foreach (ITaskItem sourceFile in this.SourcesCompiled)
            {
                this.m_currentSourceItem = sourceFile;

                try
                {
                    // Command line we'll be using
                    string responseFileCommands = this.GenerateResponseFileCommands();
                    string logString = pathToTool + " " + responseFileCommands;

                    // Echo name of just the source file. Win32's CL does this.
                    this.Log.LogMessageFromText(Path.GetFileName(sourceFile.ToString()), MessageImportance.High);

                    // Echo full command line if we want it
                    if (this.EchoCommandLines == "true")
                    {
                        this.Log.LogMessageFromText(logString, MessageImportance.High);
                    }

                    // Create dummy .h file for precompiled headers. Units with the "use" setting can forcefully include it, which
                    // in turn makes GCC load up the .gch
                    string pchSetting = sourceFile.GetMetadata("PrecompiledHeader").ToLowerInvariant();
                    if (pchSetting == "create")
                    {
                        string pchOutputH = Path.GetFullPath(sourceFile.GetMetadata("PrecompiledHeaderOutputFile"));
                        using (StreamWriter writer = new StreamWriter(pchOutputH, false, Encoding.ASCII))
                        {
                            writer.WriteLine("#error \"Problem with precompiled headers. It's likely that the .gch file is not present, or you're using a combination of C and CPP files.\"");
                        }
                    }

                    // Execute the tool, on this source file, with the given commandline.
                    retCode = base.ExecuteTool(pathToTool, responseFileCommands, string.Empty);

                    if (retCode != 0)
                    {
                        // Bad run, bail out here
                        hitError = true;
                    }
                }
                catch (Exception ex)
                {
                    this.Log.LogWarning(ex.ToString());

                    // Exception caught, bail out here
                    hitError = true;
                    retCode = this.ExitCode;
                }

                if (hitError)
                {
                    break;
                }
            }
            return retCode;
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
            if (this.m_currentSourceItem != null)
            {
                string objectFile = Path.GetFullPath(this.m_currentSourceItem.GetMetadata("ObjectFileName"));
                if (string.IsNullOrEmpty(objectFile) || Path.GetFileName(objectFile) == string.Empty)
                {
                    this.Log.LogError("The ObjectFileName setting in the Visual Studio C/C++ - Output Files sheet is set to a directory:");
                    this.Log.LogError(objectFile);
                    this.Log.LogError("^ This should be set to a filename instead. Consider using the default of: $(IntDir)%(FileName).o");
                    return string.Empty;
                }

                string pchSetting = this.m_currentSourceItem.GetMetadata("PrecompiledHeader").ToLowerInvariant();
                if (pchSetting == "use")
                {
                    string pchOutputH = Path.GetFullPath(this.m_currentSourceItem.GetMetadata("PrecompiledHeaderOutputFile"));
                    templateStr.Append(" -include ");
                    templateStr.Append(Utils.PathSanitize(pchOutputH));
                    templateStr.Append(" ");
                }

                string sourcePath = Utils.PathSanitize(this.m_currentSourceItem.ToString());

                // -c = Compile the C/C++ file
                // -MD = Generate dependency .d file
                if (this.m_propXmlParse != null)
                    templateStr.Append(this.m_propXmlParse.ProcessProperties(this.m_currentSourceItem));
                templateStr.Append(" -c -MD ");
                templateStr.Append(sourcePath);

                // Remove rtti stuff from plain C builds. -Wall generates warnings otherwise.
                string compileAs = this.m_currentSourceItem.GetMetadata("CompileAs");
                if (compileAs != null && compileAs == "CompileAsC")
                {
                    templateStr.Replace("-fno-rtti", "");
                    templateStr.Replace("-frtti", "");
                }
            }

            return templateStr.ToString();
        }

        private CanonicalTrackedOutputFiles ConstructWriteTLog(ITaskItem[] upToDateSources)
        {
            // Remove any files we're about to compile from the log
            TaskItem item = new TaskItem(Path.Combine(this.TrackerIntermediateDirectory, this.WriteTLogNames[0]));
            CanonicalTrackedOutputFiles files = new CanonicalTrackedOutputFiles(new ITaskItem[] { item });
            if (this.Sources != null)
            {
                files.RemoveEntriesForSource(this.Sources);
            }

            // Add in the files we're compiling right now. Essentially just updating their output object filenames.
            foreach (ITaskItem sourceItem in upToDateSources)
            {
                string sourcePath = Path.GetFullPath(sourceItem.ItemSpec).ToUpperInvariant();
                string objectFile = Path.GetFullPath(sourceItem.GetMetadata("ObjectFileName")).ToUpperInvariant();
                files.AddComputedOutputForSourceRoot(sourcePath, objectFile);
            }

            // Save it out
            files.SaveTlog();

            // Pass onto the ReadTLog saving
            return files;
        }

        private void ConstructReadTLog(ITaskItem[] upToDateSources, CanonicalTrackedOutputFiles outputs)
        {
            string readTrackerPath = Path.GetFullPath(this.TrackerIntermediateDirectory + "\\" + this.ReadTLogNames[0]);

            // Rewrite out read log, with the sources we're *not* compiling right now.
            TaskItem readTrackerItem = new TaskItem(readTrackerPath);
            CanonicalTrackedInputFiles files = new CanonicalTrackedInputFiles(new ITaskItem[] { readTrackerItem }, this.Sources, outputs, false, false);
            files.RemoveEntriesForSource(this.Sources);
            files.SaveTlog();

            // Now append onto the read log the sources we're compiling. It'll parse the .d files for each compiled file, so we know the
            // dependency header files associated with it, these will be recorded in the logfile.
            using (StreamWriter writer = new StreamWriter(readTrackerPath, true, Encoding.Unicode))
            {
                foreach (ITaskItem sourceItem in upToDateSources)
                {
                    string itemSpec = sourceItem.ItemSpec;
                    string sourcePath = Path.GetFullPath(sourceItem.ItemSpec).ToUpperInvariant();
                    string objectFile = Path.GetFullPath(sourceItem.GetMetadata("ObjectFileName"));
                    string dotDFile = Path.GetFullPath(Path.GetDirectoryName(objectFile) + "\\" + Path.GetFileNameWithoutExtension(objectFile) + ".d");

                    string pchSetting = sourceItem.GetMetadata("PrecompiledHeader").ToLowerInvariant();

                    try
                    {
                        List<string> dependencies = new List<string>();

                        dependencies.Add("^" + sourcePath);

                        // don't add an entry if the object file wasn't built
                        if (!File.Exists(objectFile))
                            continue;

                        if (!File.Exists(dotDFile))
                        {
                            throw new Exception("File " + sourcePath + " is missing it's .d file: " + dotDFile);
                        }

                        DepFileParse depFileParse = new DepFileParse(dotDFile);

                        foreach (string dependentFile in depFileParse.DependentFiles)
                        {
                            if (dependentFile != sourcePath)
                            {
                                if (File.Exists(dependentFile) == false)
                                {
                                    throw new Exception("File " + sourcePath + " is missing dependent file: " + dependentFile);
                                }

                                dependencies.Add(dependentFile);
                            }
                        }

                        if (pchSetting == "use")
                        {
                            string pchOutputH = Path.GetFullPath(sourceItem.GetMetadata("PrecompiledHeaderOutputFile"));
                            string pchOutputGCH = pchOutputH + ".gch";
                            dependencies.Add(pchOutputH.ToUpperInvariant());
                            dependencies.Add(pchOutputGCH.ToUpperInvariant());
                        }

                        // Done with this .d file. So delete it
                        try
                        {
                            File.Delete(dotDFile);
                        }
                        catch
                        {
                            // Safe to ignore...
                        }

                        // Finally write out the file and its dependencies, must ensure success before doing this
                        foreach (var dep in dependencies)
                        {
                            writer.WriteLine(dep);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Log.LogError("Failed processing dependencies in: " + dotDFile);
                        this.Log.LogError(ex.ToString());
                    }
                }
            }
        }

        private void ConstructCommandTLog(ITaskItem[] upToDateSources, DependencyFilter inputFilter)
        {
            StringBuilder cmdLine = new StringBuilder(Utils.EST_MAX_CMDLINE_LEN);

            // This updates any newly built source files' command line in the tlog files. So the dep checker can see when we modify
            // compiler switches, that the command line changed and we have to rebuild the file.
            IDictionary<string, string> sourcesToCommandLines = this.MapSourcesToCommandLines();
            if (upToDateSources != null)
            {
                foreach (ITaskItem item in upToDateSources)
                {
                    this.m_currentSourceItem = item;

                    string sourcePath = item.GetMetadata("FullPath");
                    if ((inputFilter == null) || inputFilter(sourcePath))
                    {
                        // Add this newly built source files' command line into the tlog file
                        cmdLine.Length = 0;
                        cmdLine.Append(this.GenerateResponseFileCommands());
                        cmdLine.Append(" ");
                        cmdLine.Append(sourcePath.ToUpperInvariant());

                        sourcesToCommandLines[FileTracker.FormatRootingMarker(item)] = cmdLine.ToString();
                    }
                    else
                    {
                        sourcesToCommandLines.Remove(FileTracker.FormatRootingMarker(item));
                    }
                }
            }
            this.WriteSourcesToCommandLinesTable(sourcesToCommandLines);
        }

        private string CheckLineForWarningOrError(string[] parts, int errorIndex)
        {
            if (parts.Length > errorIndex)
            {
                // Assumes the following GCC output format for errors and warnings:
                // <relative file name>:<line number>: [error|warning]:<message>
                if (parts[errorIndex - 1].Equals(" error") || parts[errorIndex - 1].Equals(" warning"))
                {
                    string fullSourceFile = this.m_currentSourceItem.GetMetadata("FullPath");

                    // Reformat in a way VS knows how to handle
                    return String.Format("{0}({1}): {2}: {3}", fullSourceFile, parts[1], parts[errorIndex - 1],
                        string.Join(":", parts, errorIndex, parts.Length - errorIndex));
                }
            }

            return null;
        }

        // Called when compiler outputs a line
        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            base.LogEventsFromTextOutput(Utils.GCCOutputReplace(singleLine), messageImportance);
        }

        protected override string ToolName
        {
            get
            {
                return this.GCCToolPath;
            }
        }

        public string PlatformToolset
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
                    Description = "Tracker Log Directory.",
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
                return this.m_toolfilename + ".compile.command.1.tlog";
            }
        }

        protected override string[] ReadTLogNames
        {
            get
            {
                return new string[] { this.m_toolfilename + ".compile.read.1.tlog" };
            }
        }

        protected override string[] WriteTLogNames
        {
            get
            {
                return new string[] { this.m_toolfilename + ".compile.write.1.tlog" };
            }
        }
    }


}
