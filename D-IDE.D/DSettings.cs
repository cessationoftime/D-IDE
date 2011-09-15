﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.Xml;
using D_IDE.D.CodeCompletion;
using System.Threading;

namespace D_IDE.D
{
	public class DSettings
	{
		public static DSettings Instance=new DSettings();

		public DMDConfig dmd1 = new DMDConfig() { Version=DVersion.D1};
		public DMDConfig dmd2 = new DMDConfig() { Version=DVersion.D2};

		public DVersion DefaultDMDVersion = DVersion.D2;

		public DMDConfig DMDConfig()
		{
			return DMDConfig(DefaultDMDVersion);
		}

		public DMDConfig DMDConfig(DVersion v)
		{
			if (v == DVersion.D1)
				return dmd1;
			return dmd2;
		}

		public string cv2pdb_exe = "cv2pdb.exe";
		public bool UseCodeCompletion = true;
		public bool UseMethodInsight = true;
		public bool EnableMatchingBracketHighlighting = true;
		public bool UseSemanticHighlighting = true;
		public bool ShowSemanticErrors = true;

		/// <summary>
		/// If non-letter has been typed, the popup will close down and insert the selected item's completion text.
		/// If this value is false, the completion text will _not_ be inserted.
		/// In developing process, this flag will remain 'false' by default - because the completion still not brings 100%-suitable results.
		/// </summary>
		public bool ForceCodeCompetionPopupCommit = false;

		#region Saving&Loading
		public void Save(XmlWriter x)
		{
			x.WriteStartDocument();

			x.WriteStartElement("dsettings");

			x.WriteStartElement("cv2pdb");
			x.WriteCData(cv2pdb_exe);
			x.WriteEndElement();

			x.WriteStartElement("BracketHightlighting");
			x.WriteAttributeString("value", EnableMatchingBracketHighlighting.ToString().ToLower());
			x.WriteEndElement();

			x.WriteStartElement("UseCodeCompletion");
			x.WriteAttributeString("value",UseCodeCompletion.ToString().ToLower());
			x.WriteEndElement();

			x.WriteStartElement("UseMethodInsight");
			x.WriteAttributeString("value", UseMethodInsight.ToString().ToLower());
			x.WriteEndElement();

			x.WriteStartElement("ForceCodeCompetionPopupCommit");
			x.WriteAttributeString("value", ForceCodeCompetionPopupCommit. ToString().ToLower());
			x.WriteEndElement();

			x.WriteStartElement("UseSemanticHighlighting");
			x.WriteAttributeString("value", UseSemanticHighlighting.ToString().ToLower());
			x.WriteEndElement();

			x.WriteStartElement("ShowSemanticErrors");
			x.WriteAttributeString("value", ShowSemanticErrors.ToString().ToLower());
			x.WriteEndElement();

			dmd1.Save(x);
			dmd2.Save(x);

			x.WriteEndElement();
		}

		public void Load(XmlReader x)
		{
			while (x.Read())
			{
				switch (x.LocalName)
				{
					case "BracketHightlighting":
						if (x.MoveToAttribute("value"))
							EnableMatchingBracketHighlighting = x.ReadContentAsBoolean();
						break;

					case "UseMethodInsight":
						if (x.MoveToAttribute("value"))
							UseMethodInsight = x.ReadContentAsBoolean();
						break;

					case "UseCodeCompletion":
						if (x.MoveToAttribute("value"))
							UseCodeCompletion = x.ReadContentAsBoolean();
						break;

					case "ForceCodeCompetionPopupCommit":
						if (x.MoveToAttribute("value"))
							ForceCodeCompetionPopupCommit = x.ReadContentAsBoolean();
						break;

					case "UseSemanticHighlighting":
						if (x.MoveToAttribute("value"))
							UseSemanticHighlighting = x.ReadContentAsBoolean();
						break;

					case "ShowSemanticErrors":
						if (x.MoveToAttribute("value"))
							ShowSemanticErrors = x.ReadContentAsBoolean();
						break;

					case "cv2pdb":
						cv2pdb_exe = x.ReadString();
						break;

					case "dmd":
						var config = new DMDConfig();
						config.Load(x);

						if (config.Version == DVersion.D1)
							dmd1 = config;
						else
							dmd2 = config;
						break;
				}
			}
		}
		#endregion
	}

	public enum DVersion:int
	{
		D1=1,
		D2=2
	}

	public class DMDConfig
	{
		public class DBuildArguments
		{
			public bool IsDebug = false;

			public string SoureCompiler;
			public string Win32ExeLinker;
			public string ExeLinker;
			public string DllLinker;
			public string LibLinker;

			public void Load(XmlReader x)
			{
				if (x.LocalName != "buildarguments")
					return;

				if (x.MoveToAttribute("IsDebug"))
					bool.TryParse(x.GetAttribute("IsDebug"),out IsDebug);
				x.MoveToElement();

				var x2 = x.ReadSubtree();
				while (x2.Read())
				{
					switch (x2.LocalName)
					{
						case "sourcecompiler":
							SoureCompiler = x2.ReadString();
							break;
						case "win32linker":
							Win32ExeLinker = x2.ReadString();
							break;
						case "exelinker":
							ExeLinker = x2.ReadString();
							break;
						case "dlllinker":
							DllLinker = x2.ReadString();
							break;
						case "liblinker":
							LibLinker = x2.ReadString();
							break;
					}
				}
			}

			public void Save(XmlWriter x)
			{
				x.WriteStartElement("buildarguments");
				x.WriteAttributeString("IsDebug",IsDebug.ToString());

				x.WriteStartElement("sourcecompiler");
				x.WriteCData(SoureCompiler);
				x.WriteEndElement();

				x.WriteStartElement("win32linker");
				x.WriteCData(Win32ExeLinker);
				x.WriteEndElement();

				x.WriteStartElement("exelinker");
				x.WriteCData(ExeLinker);
				x.WriteEndElement();

				x.WriteStartElement("dlllinker");
				x.WriteCData(DllLinker);
				x.WriteEndElement();

				x.WriteStartElement("liblinker");
				x.WriteCData(LibLinker);
				x.WriteEndElement();

				x.WriteEndElement();
			}

			public void ApplyFrom(DBuildArguments other)
			{
				IsDebug = other.IsDebug;
				SoureCompiler = other.SoureCompiler;
				Win32ExeLinker = other.Win32ExeLinker;
				ExeLinker = other.ExeLinker;
				DllLinker = other.DllLinker;
				LibLinker = other.LibLinker;
			}
		}

		public DVersion Version = DVersion.D2;

		public readonly ASTStorage ASTCache = new ASTStorage();

		/// <summary>
		/// The "bin" directory of the dmd installation
		/// </summary>
		public string BaseDirectory = @"C:\dmd2\windows\bin";

		public string SoureCompiler = "dmd.exe";
		public string ExeLinker = "dmd.exe";
		public string Win32ExeLinker = "dmd.exe";
		public string DllLinker = "dmd.exe";
		public string LibLinker = "lib.exe";

		public List<string> DefaultLinkedLibraries = new List<string>();

		public DBuildArguments BuildArguments(bool IsDebug)
		{
			if (IsDebug)
				return DebugArgs;
			return ReleaseArgs;
		}

		public DBuildArguments DebugArgs=new DBuildArguments(){
			IsDebug=true,
			SoureCompiler = "-c \"$src\" -of\"$obj\" $importPaths -gc -debug",
			Win32ExeLinker = "$objs -L/su:windows -L/exet:nt -of\"$exe\" -gc -debug",
			ExeLinker = "$objs -of\"$exe\" -gc -debug",
			DllLinker = "$objs -L/IMPLIB:\"$lib\" -of\"$dll\" -gc -debug",
			LibLinker = "-c -n -of\"$lib\" $objs"
		};

		public DBuildArguments ReleaseArgs=new DBuildArguments(){
			SoureCompiler = "-c \"$src\" -of\"$obj\" $importPaths -release -O -inline",
			Win32ExeLinker = "$objs -L/su:windows -L/exet:nt -of\"$exe\" -release -O -inline",
			ExeLinker = "$objs -of\"$exe\" -release -O -inline",
			DllLinker = "$objs -L/IMPLIB:\"$lib\" -of\"$dll\" -release -O -inline",
			LibLinker = "-c -n \"$lib\" $objs"
		};

		public void Load(XmlReader x)
		{
			if (x.LocalName != "dmd")
				return;

			if(x.MoveToAttribute("version"))
				Version = (DVersion)Convert.ToInt32( x.GetAttribute("version"));
			x.MoveToElement();

			var x2 = x.ReadSubtree();
			while (x2.Read())
			{
				switch (x2.LocalName)
				{
					case "basedirectory":
						BaseDirectory = x2.ReadString();
						break;
					case "sourcecompiler":
						SoureCompiler = x2.ReadString();
						break;
					case "exelinker":
						ExeLinker = x2.ReadString();
						break;
					case "win32linker":
						Win32ExeLinker = x2.ReadString();
						break;
					case "dlllinker":
						DllLinker = x2.ReadString();
						break;
					case "liblinker":
						LibLinker = x2.ReadString();
						break;

					case "buildarguments":
						var args = new DBuildArguments();
						args.Load(x2);
						if (args.IsDebug)
							DebugArgs = args;
						else 
							ReleaseArgs = args;
						break;

					case "parsedDirectories":
						if (x2.IsEmptyElement)
							break;

						var st = x2.ReadSubtree();
						if(st!=null)
							while (st.Read())
							{
								if (st.LocalName == "dir")
								{
									var dir = st.ReadString();
									if(!string.IsNullOrWhiteSpace(dir))
										ASTCache.Add(dir,System.Diagnostics.Debugger.IsAttached);
								}
							}
						break;

					case "DefaultLibs":
						var xr2 = x2.ReadSubtree();
						while (xr2.Read())
						{
							if (xr2.LocalName == "lib")
								DefaultLinkedLibraries.Add(xr2.ReadString());
						}
						break;
				}
			}

			// After having loaded the directory paths, parse them asynchronously
			new Thread(() =>
			{
				Thread.CurrentThread.IsBackground = true;
				
				ASTCache.UpdateCache();

				// For debugging purposes dump all parse results (errors etc.) to a log file.
				try
				{
					ASTCache.WriteParseLog(IDEInterface.ConfigDirectory + "\\" + Version.ToString() + ".GlobalParseLog.log");
				}
				catch (Exception ex)
				{
					ErrorLogger.Log(ex,ErrorType.Warning,ErrorOrigin.System);
				}
			}).Start();
		}

		public void Save(XmlWriter x)
		{
			x.WriteStartElement("dmd");
			x.WriteAttributeString("version",((int)Version).ToString());

			if (!string.IsNullOrEmpty(BaseDirectory)){
				x.WriteStartElement("basedirectory");
				x.WriteCData(BaseDirectory);
				x.WriteEndElement();
			}
			if (!string.IsNullOrEmpty(SoureCompiler)){
			x.WriteStartElement("sourcecompiler");
			x.WriteCData(SoureCompiler);
			x.WriteEndElement();
			} 
			if (!string.IsNullOrEmpty(ExeLinker)){
				x.WriteStartElement("exelinker");
				x.WriteCData(ExeLinker);
				x.WriteEndElement();
			}
			if (!string.IsNullOrEmpty(Win32ExeLinker))
			{
				x.WriteStartElement("win32linker");
				x.WriteCData(Win32ExeLinker);
				x.WriteEndElement();
			}
			if (!string.IsNullOrEmpty(DllLinker))
			{
				x.WriteStartElement("dlllinker");
				x.WriteCData(DllLinker);
				x.WriteEndElement();
			}
			if (!string.IsNullOrEmpty(LibLinker))
			{
				x.WriteStartElement("liblinker");
				x.WriteCData(LibLinker);
				x.WriteEndElement();
			}

			x.WriteStartElement("parsedDirectories");
			foreach (var pdir in ASTCache)
			{
				x.WriteStartElement("dir");
				x.WriteCData(pdir.BaseDirectory);
				x.WriteEndElement();
			}
			x.WriteEndElement();

			x.WriteStartElement("DefaultLibs");
			foreach (var lib in DefaultLinkedLibraries)
			{
				x.WriteStartElement("lib");
				x.WriteCData(lib);
				x.WriteEndElement();
			}
			x.WriteEndElement();

			DebugArgs.Save(x);
			ReleaseArgs.Save(x);

			x.WriteEndElement();
		}
	}
}
