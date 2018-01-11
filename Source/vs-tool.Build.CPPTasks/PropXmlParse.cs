// ***********************************************************************************************
// (c) 2012 Gavin Pugh http://www.gavpugh.com/ - Released under the open-source zlib license
// ***********************************************************************************************

// Parser for MsBuild .xml property files. There's possibly a nicer Microsoft way of doing this, but because
// there's zero documentation for any of this stuff, I couldn't find it. This class will take a given
// property-sheet xml file, and a set of metadata. It'll spit out the correct commandline switches based
// on the metadata.
// Why? Sure beats the copy and pasted per-switch code you have to do with the TrackedVCToolTask. I just
// wanted to keep one place maintained. It effectively also makes the code data-driven, so new switches
// can be added without recompiling the dll.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;

using Microsoft.Build.Framework;

namespace vs.tool.Build.CPPTasks
{
    class PropXmlParse
    {
        private Dictionary<string, Property> m_properties = new Dictionary<string, Property>();
        private string m_switchPrefix;

        public PropXmlParse(string path)
        {
            this.m_switchPrefix = null;

            if (!string.IsNullOrEmpty(path))
            {
                XmlTextReader reader = new XmlTextReader(path);

                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element: // The node is an element.
                            {
                                switch (reader.Name)
                                {
                                    case "Rule":
                                        this.m_switchPrefix = reader.GetAttribute("SwitchPrefix");
                                        break;

                                    case "StringListProperty":
                                        this.NewProperty(reader, new StringListProperty());
                                        break;
                                    case "StringProperty":
                                    case "IntProperty":
                                        this.NewProperty(reader, new StringProperty());
                                        break;
                                    case "BoolProperty":
                                        this.NewProperty(reader, new BoolProperty());
                                        break;
                                    case "EnumProperty":
                                        this.NewProperty(reader, new EnumProperty());
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
        }

        public string ProcessProperties(ITaskItem taskItem)
        {
            StringBuilder returnStr = new StringBuilder(Utils.EST_MAX_CMDLINE_LEN);

            foreach (string metaName in taskItem.MetadataNames)
            {
                string propValue = taskItem.GetMetadata(metaName);
                string processed = this.ProcessProperty(metaName, propValue).Trim();

                if (processed.Length > 0)
                {
                    returnStr.Append(processed);
                    returnStr.Append(" ");
                }
            }

            return returnStr.ToString().Trim();
        }

        private string ProcessProperty(string propName, string propVal)
        {
            Property prop;
            if (this.m_properties.TryGetValue(propName, out prop))
            {
                if (prop.Ignored)
                {
                    return string.Empty;
                }

                return prop.Process(propVal);
            }
            return string.Empty;
        }

        private void NewProperty(XmlTextReader xml, Property prop)
        {
            string name = xml.GetAttribute("Name");
            string switchPrefixOverride = xml.GetAttribute("SwitchPrefix");
            string separator = xml.GetAttribute("Separator");
            string includeInCmdLine = xml.GetAttribute("IncludeInCommandLine");
            string subType = xml.GetAttribute("Subtype");

            // Just need at least a valid name
            if (name != null)
            {
                // Choose correct switch prefix
                string prefix = this.m_switchPrefix;
                if (switchPrefixOverride != null)
                {
                    prefix = switchPrefixOverride;
                }
                if (prefix == null)
                {
                    prefix = string.Empty;
                }

                // Separator for string, int and stringlist properties
                if (separator == null)
                {
                    separator = string.Empty;
                }

                if (includeInCmdLine != null)
                {
                    if (includeInCmdLine.ToLower() == "false")
                    {
                        // Ignore the ones that aren't meant to be in the cmdline
                        return;
                    }
                }

                // Will quote fix for files or folder params
                bool shouldQuoteFix = false;
                if (subType != null)
                {
                    if ((subType.ToLower() == "file") || (subType.ToLower() == "folder"))
                    {
                        shouldQuoteFix = true;
                    }
                }

                prop.Setup(xml, prefix, separator, shouldQuoteFix);

                this.m_properties.Add(name, prop);
            }
        }

        public abstract class Property
        {
            public abstract string Process(string propVal);

            public bool Ignored
            {
                get { return this.m_ignored; }
            }

            public void Setup(XmlTextReader xml, string switchPrefix, string separator, bool quoteFix)
            {
                this.m_switchPrefix = switchPrefix;
                this.m_separator = separator;
                this.m_quoteFix = quoteFix;

                Debug.Assert(this.m_switchPrefix != null);
                Debug.Assert(this.m_separator != null);

                string switchValue = xml.GetAttribute("Switch");
                this.m_ignored = (switchValue == "ignore");

                this.SetupProperty(xml);
            }

            protected string FixString(string str)
            {
                if (this.m_quoteFix == false)
                {
                    // Just fix the slashes, no quoting
                    return Utils.PathFixSlashes(str);
                }
                else
                {
                    // Slash fixing AND possible quoting
                    return Utils.PathSanitize(str);
                }
            }

            protected abstract void SetupProperty(XmlTextReader xml);

            protected string m_switchPrefix;
            protected string m_separator;
            protected bool m_quoteFix;
            protected bool m_ignored;
        }

        public class EnumProperty : Property
        {
            public override string Process(string propVal)
            {
                string found;
                if (this.m_switches.TryGetValue(propVal, out found))
                {
                    return found;
                }

                return string.Empty;
            }

            protected override void SetupProperty(XmlTextReader xml)
            {
                // switchString is just the prefix for enum properties
                int nestLevel = 1;

                while (xml.Read())
                {
                    switch (xml.NodeType)
                    {
                        case XmlNodeType.Element: // The node is an element.
                            {
                                if (xml.IsEmptyElement == false)
                                {
                                    nestLevel++;
                                }

                                if (xml.Name == "EnumValue")
                                {
                                    string switchVal = xml.GetAttribute("Switch");
                                    string nameStr = xml.GetAttribute("Name");

                                    if (nameStr != null)
                                    {
                                        if ((switchVal != null) && (switchVal != String.Empty))
                                        {
                                            this.m_switches.Add(nameStr, this.m_switchPrefix + switchVal);
                                        }
                                        else
                                        {
                                            this.m_switches.Add(nameStr, string.Empty);
                                        }
                                    }
                                }
                            }
                            break;

                        case XmlNodeType.EndElement: // The node is an element.
                            {
                                nestLevel--;

                                if (nestLevel == 0)
                                {
                                    return;
                                }
                            }
                            break;
                    }
                }
            }

            private Dictionary<string, string> m_switches = new Dictionary<string, string>();
        }

        public class BoolProperty : Property
        {
            public override string Process(string propVal)
            {
                if (propVal.ToLower() == "true")
                {
                    if (this.m_trueSwitch != null)
                    {
                        return this.m_switchPrefix + this.m_trueSwitch;
                    }
                }
                else if (propVal.ToLower() != "ignore")
                {
                    if (this.m_falseSwitch != null)
                    {
                        return this.m_switchPrefix + this.m_falseSwitch;
                    }
                }
                return string.Empty;
            }

            protected override void SetupProperty(XmlTextReader xml)
            {
                this.m_trueSwitch = xml.GetAttribute("Switch");
                this.m_falseSwitch = xml.GetAttribute("ReverseSwitch");
            }

            private string m_falseSwitch;
            private string m_trueSwitch;
        }

        public class StringProperty : Property
        {
            public override string Process(string propVal)
            {
                if (this.m_switch == null)
                {
                    return this.FixString(propVal);
                }

                if (propVal.Length > 0)
                {
                    // Ignore switches entirely if we don't have one
                    if (this.m_switch != null)
                    {
                        return this.m_switchPrefix + this.m_switch + this.m_separator + this.FixString(propVal);
                    }
                    else
                    {
                        return this.FixString(propVal);
                    }
                }

                return string.Empty;
            }

            protected override void SetupProperty(XmlTextReader xml)
            {
                this.m_switch = xml.GetAttribute("Switch");
            }

            private string m_switch;
        }

        public class StringListProperty : Property
        {
            public override string Process(string propVal)
            {
                StringBuilder sBuilder = new StringBuilder(1024);
                string[] strings = propVal.Split(';');

                foreach (string str in strings)
                {
                    if (str.Length > 0)
                    {
                        // Ignore switches entirely if we don't have one
                        if (this.m_switch != null)
                        {
                            sBuilder.Append(this.m_switchPrefix);
                            sBuilder.Append(this.m_switch);
                            sBuilder.Append(this.m_separator);
                        }

                        sBuilder.Append(this.FixString(str));
                        sBuilder.Append(" ");
                    }
                }

                return sBuilder.ToString();
            }

            protected override void SetupProperty(XmlTextReader xml)
            {
                this.m_switch = xml.GetAttribute("Switch");
            }

            private string m_switch;
        }
    }

}
