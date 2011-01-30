﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.IO;

namespace D_IDE
{
	partial class IDEManager
	{
		public class BuildManagement
		{
			/// <summary>
			/// Builds the current solution incrementally.
			/// If no solution present, the current module will be built to a standalone executable.
			/// </summary>
			public static bool Build()
			{
				if (CurrentSolution != null)
					return Build(CurrentSolution, true);

				return BuildSingle();
			}

			public static bool Build(Solution sln, bool Incrementally)
			{
				//TODO: Build from most independent to most dependent project
				//TODO: Enable incremental build

				foreach (var prj in sln)
				{
					if (!Build(prj, Incrementally))
						return false;
				}
				return true;
			}

			public static bool Build(Project Project, bool Incrementally)
			{
				Instance.MainWindow.ClearLog();

				bool isPrj = false;
				var lang = AbstractLanguageBinding.SearchBinding(Project.FileName, out isPrj);

				if (lang != null && isPrj && lang.CanBuild)
				{
					var br = ErrorManagement.LastBuildResult = lang.BuildSupport.BuildProject(Project);
					Project.LastBuildResult = br;

					if (br.Successful)
						Project.CopyCopyableOutputFiles();

					ErrorManagement.RefreshErrorList();

					return br.Successful;
				}
				return false;
			}

			public static bool BuildSingle()
			{
				string _u = null;
				return BuildSingle(out _u);
			}

			/// <summary>
			/// Builds currently edited document to single executable
			/// </summary>
			/// <returns></returns>
			public static bool BuildSingle(out string CreatedExecutable)
			{
				CreatedExecutable = null;
				if (Instance.CurrentEditor == null)
					return false;

				string file = Instance.CurrentEditor.AbsoluteFilePath;
				bool IsProject = false;
				var lang = AbstractLanguageBinding.SearchBinding(file, out IsProject);

				if (lang == null || IsProject || !lang.CanBuild)
					return false;

				Instance.MainWindow.ClearLog();

				var br = ErrorManagement.LastBuildResult = lang.BuildSupport.BuildStandAlone(file);
				CreatedExecutable = br.TargetFile;

				ErrorManagement.RefreshErrorList();
				return br.Successful;
			}

			public static void CleanUpOutput(Solution sln)
			{

			}

			public static void CleanUpOutput(Project Project)
			{

			}
		}
	}
}