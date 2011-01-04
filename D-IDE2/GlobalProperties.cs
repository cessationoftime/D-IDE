﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
using Parser.Core;
using System.Xml;
using D_IDE.Core;

namespace D_IDE
{
	internal class GlobalProperties
	{
        

		/// <summary>
		/// A list of all globally loaded projects.
		/// Useful when
		/// </summary>
		public static List<Project> ProjectCache = new List<Project>();

        public const string MainSettingsFile = "D-IDE2.settings.xml";
        public const string LayoutFile = "D-IDE2.layout.xml";

        /// <summary>
        /// Globally initializes all settings and essential properties
        /// </summary>
        public static void Init()
        {
			try
			{
				Current = Load();
				if (Current == null)
					Current = new GlobalProperties();
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }
        }

		public static GlobalProperties Load()
		{
			return Load(Path.Combine(IDEInterface.ConfigDirectory, MainSettingsFile));
		}
		public static GlobalProperties Load(string fn)
		{
			if (!File.Exists(fn)) return null;

            try
            {
                Stream stream = File.Open(fn, FileMode.Open);

                XmlTextReader xr = new XmlTextReader(stream);
                var p = new GlobalProperties();

                while (xr.Read())// now 'settings' should be the current node
                {
                    if (xr.NodeType == XmlNodeType.Element)
                    {
                        switch (xr.LocalName)
                        {
                            default: break;
                                
                            case "codetemplates":
                                //CodeTemplate.Load(xr);
                                break;

                            case "recentprojects":
                                if (xr.IsEmptyElement) break;
                                while (xr.Read())
                                {
                                    if (xr.LocalName == "f")
                                    {
                                        try
                                        {
                                            p.LastProjects.Add(xr.ReadString());
                                        }
                                        catch { }
                                    }
                                    else break;
                                }
                                break;

                            case "recentfiles":
                                if (xr.IsEmptyElement) break;
                                while (xr.Read())
                                {
                                    if (xr.LocalName == "f")
                                    {
                                        try
                                        {
                                            p.LastFiles.Add(xr.ReadString());
                                        }
                                        catch { }
                                    }
                                    else break;
                                }
                                break;

                            case "lastopenedfiles":
                                if (xr.IsEmptyElement) break;
                                while (xr.Read())
                                {
                                    if (xr.LocalName == "f")
                                    {
                                        try
                                        {
                                            p.LastOpenFiles.Add(xr.ReadString());
                                        }
                                        catch { }
                                    }
                                    else break;
                                }
                                break;


                            case "openlastprj":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.OpenLastPrj = xr.Value == "1";
                                }
                                break;

                            case "openlastfiles":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.OpenLastFiles = xr.Value == "1";
                                }
                                break;

                            case "windowstate":
                                if (xr.MoveToAttribute("value"))
                                {
                                    try { p.lastFormState = (WindowState)Convert.ToInt32(xr.Value); }
                                    catch { }
                                }
                                break;

                            case "windowsize":
                                if (xr.MoveToAttribute("x"))
                                {
                                    try { p.lastFormSize.Width = Convert.ToInt32(xr.Value); }
                                    catch { }
                                }
                                if (xr.MoveToAttribute("y"))
                                {
                                    try { p.lastFormSize.Height = Convert.ToInt32(xr.Value); }
                                    catch { }
                                }
                                break;

                            case "retrievenews":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.RetrieveNews = xr.Value == "1";
                                }
                                break;

                            case "logbuildstatus":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.LogBuildProgress = xr.Value == "1";
                                }
                                break;

                            case "showbuildcommands":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.ShowBuildCommands = xr.Value == "1";
                                }
                                break;

                            case "externaldbg":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.UseExternalDebugger = xr.Value == "1";
                                }
                                break;

                            case "singleinstance":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.SingleInstance = xr.Value == "1";
                                }
                                break;

                            case "watchforupdates":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.WatchForUpdates = xr.Value == "1";
                                }
                                break;

                            case "defprjdir":
                                p.DefaultProjectDirectory = xr.ReadString();
                                break;

                            case "debugger":
                                if (xr.IsEmptyElement) break;
                                while (xr.Read())
                                {
                                    if (xr.LocalName == "bin")
                                    {
                                        p.exe_dbg = xr.ReadString();
                                    }
                                    else if (xr.LocalName == "args")
                                    {
                                        p.dbg_args = xr.ReadString();
                                    }
                                    else break;
                                }
                                break;

                            case "lastsearchdir":
                                p.lastSearchDir = xr.ReadString();
                                break;

                            case "verbosedbgoutput":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.VerboseDebugOutput = xr.Value == "1";
                                }
                                break;

                            case "skipunknowncode":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.SkipUnknownCode = xr.Value == "1";
                                }
                                break;

                            case "showdbgpanelswhendebugging":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.ShowDbgPanelsOnDebugging = xr.Value == "1";
                                }
                                break;

                            case "autosave":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.DoAutoSaveOnBuilding = xr.Value == "1";
                                }
                                break;

                            case "createpdb":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.CreatePDBOnBuild = xr.Value == "1";
                                }
                                break;

                            case "highlightings":
                                if (xr.IsEmptyElement) break;
                                while (xr.Read())
                                {
                                    if (xr.LocalName == "f")
                                    {
                                        try
                                        {
                                            string ext = xr.GetAttribute("ext");
                                            p.SyntaxHighlightingEntries.Add(ext, xr.ReadString());
                                        }
                                        catch { }
                                    }
                                    else break;
                                }
                                break;

                            case "shownewconsolewhenexecuting":
                                if (xr.MoveToAttribute("value"))
                                {
                                    p.ShowExternalConsoleWhenExecuting = xr.Value == "1";
                                }
                                break;
                        }
                    }
                }

                xr.Close();
				return p;

            }
            catch {  }
            return null;
		}

		public static void Save()
		{
			try
			{
				Save(Path.Combine(IDEInterface.ConfigDirectory, MainSettingsFile));
			}
			catch { }
		}
		public static void Save(string fn)
		{
			if (String.IsNullOrEmpty(fn)) return;

			var xw = new XmlTextWriter(fn, Encoding.UTF8);
			xw.WriteStartDocument();
			xw.WriteStartElement("settings");

			xw.WriteStartElement("recentprojects");
			foreach (string f in Current.LastProjects)
			{
				xw.WriteStartElement("f"); xw.WriteCData(f); xw.WriteEndElement();
			}
			xw.WriteEndElement();

			xw.WriteStartElement("recentfiles");
			foreach (string f in Current.LastFiles)
			{
				xw.WriteStartElement("f"); xw.WriteCData(f); xw.WriteEndElement();
			}
			xw.WriteEndElement();

			xw.WriteStartElement("lastopenedfiles");
			foreach (string f in Current.LastOpenFiles)
			{
				xw.WriteStartElement("f"); xw.WriteCData(f); xw.WriteEndElement();
			}
			xw.WriteEndElement();

			xw.WriteStartElement("openlastprj");
			xw.WriteAttributeString("value", Current.OpenLastPrj ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("openlastfiles");
			xw.WriteAttributeString("value", Current.OpenLastFiles ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("windowstate");
			xw.WriteAttributeString("value", ((int)Current.lastFormState).ToString());
			xw.WriteEndElement();

			xw.WriteStartElement("windowsize");
			xw.WriteAttributeString("x", Current.lastFormSize.Width.ToString());
			xw.WriteAttributeString("y", Current.lastFormSize.Height.ToString());
			xw.WriteEndElement();

			xw.WriteStartElement("retrievenews");
			xw.WriteAttributeString("value", Current.RetrieveNews ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("logbuildstatus");
			xw.WriteAttributeString("value", Current.LogBuildProgress ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("showbuildcommands");
			xw.WriteAttributeString("value", Current.ShowBuildCommands ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("externaldbg");
			xw.WriteAttributeString("value", Current.UseExternalDebugger ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("singleinstance");
			xw.WriteAttributeString("value", Current.SingleInstance ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("watchforupdates");
			xw.WriteAttributeString("value", Current.WatchForUpdates ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("defprjdir");
			xw.WriteCData(Current.DefaultProjectDirectory);
			xw.WriteEndElement();

			xw.WriteStartElement("debugger");
			xw.WriteStartElement("bin");
			xw.WriteCData(Current.exe_dbg);
			xw.WriteEndElement();
			xw.WriteStartElement("args");
			xw.WriteCData(Current.dbg_args);
			xw.WriteEndElement();
			xw.WriteEndElement();

			xw.WriteStartElement("lastsearchdir");
			xw.WriteCData(Current.lastSearchDir);
			xw.WriteEndElement();

			xw.WriteStartElement("verbosedbgoutput");
			xw.WriteAttributeString("value", Current.VerboseDebugOutput ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("skipunknowncode");
			xw.WriteAttributeString("value", Current.SkipUnknownCode ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("showdbgpanelswhendebugging");
			xw.WriteAttributeString("value", Current.ShowDbgPanelsOnDebugging ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("autosave");
			xw.WriteAttributeString("value", Current.DoAutoSaveOnBuilding ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("createpdb");
			xw.WriteAttributeString("value", Current.CreatePDBOnBuild ? "1" : "0");
			xw.WriteEndElement();

			xw.WriteStartElement("highlightings");
			foreach (string ext in Current.SyntaxHighlightingEntries.Keys)
			{
				if (String.IsNullOrEmpty(Current.SyntaxHighlightingEntries[ext])) continue;
				xw.WriteStartElement("f");
				xw.WriteAttributeString("ext", ext);
				xw.WriteCData(Current.SyntaxHighlightingEntries[ext]);
				xw.WriteEndElement();
			}
			xw.WriteEndElement();

            xw.WriteStartElement("shownewconsolewhenexecuting");
            xw.WriteAttributeString("value", Current.ShowExternalConsoleWhenExecuting ? "1" : "0");
            xw.WriteEndElement();

            //Code templates
            //CodeTemplate.Save(xw);

			xw.WriteEndDocument();
			xw.Close();
		}

		public static GlobalProperties Current=null;

		public List<string>
			LastProjects = new List<string>(),
			LastFiles = new List<string>(),
			LastOpenFiles = new List<string>();

		public bool OpenLastPrj = true;
		public bool OpenLastFiles = true;
		public WindowState lastFormState = WindowState.Maximized;
		public Point lastFormLocation;
		public Size lastFormSize;
		public Dictionary<string, string> SyntaxHighlightingEntries = new Dictionary<string, string>();
        public bool UseRibbonMenu = false;

		public bool LogBuildProgress = true;
		public bool ShowBuildCommands = true;
		public bool UseExternalDebugger = false;
		public bool DoAutoSaveOnBuilding = true;
		public bool CreatePDBOnBuild = true;
		public bool ShowDbgPanelsOnDebugging = false;

		#region Debugging
		public bool VerboseDebugOutput = false;
		public bool SkipUnknownCode = true;
        public bool ShowExternalConsoleWhenExecuting = true;
		#endregion

		public bool EnableFXFormsDesigner = false; // For those who want to experiment a little bit ;-)
		public bool RetrieveNews = true;
		public bool SingleInstance = true;
		public bool WatchForUpdates = false;
		public string DefaultProjectDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\D Projects";

		public string exe_dbg = "windbg.exe";
		public string dbg_args = "\"$exe\"";

		public string lastSearchDir = IDEUtil.ApplicationStartUpPath;
	}
}
