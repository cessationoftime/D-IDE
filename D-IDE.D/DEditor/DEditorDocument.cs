﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using D_IDE.Core;
using D_IDE.Core.Controls;
using D_IDE.Core.Controls.Editor;
using D_IDE.D.DEditor;
using D_Parser.Completion;
using D_Parser.Dom;
using D_Parser.Dom.Statements;
using D_Parser.Misc;
using D_Parser.Parser;
using D_Parser.Resolver;
using D_Parser.Resolver.TypeResolution;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using D_Parser.Refactoring;

namespace D_IDE.D
{
	public class DEditorDocument : EditorDocument, IEditorData
	{
		#region Properties
		ComboBox lookup_Types;
		ComboBox lookup_Members;
		ToolTip editorToolTip = new ToolTip();
		DIndentationStrategy indentationStrategy;
		FoldingManager foldingManager;

		/// <summary>
		/// Parse duration (in milliseconds) of the last code analysis
		/// </summary>
		public double ParseTime
		{
			get;
			protected set;
		}

		DModule _unboundTree;
		public DModule SyntaxTree
		{
			get
			{
				var prj = Project as DProject;
				if (prj != null)
					return prj.ParsedModules.GetModule(ProposedModuleName) as DModule;

				return _unboundTree;
			}
			set
			{
				if (value != null)
				{
					value.FileName = AbsoluteFilePath;
					value.ModuleName = ProposedModuleName;
				}

				var prj = Project as DProject;
				if (prj != null && !prj.ParsedModules.IsParsing)
				{
					var oldAst = SyntaxTree;
					if (oldAst != null)
					{
						// Enable incremental update of the ufcs cache -- speed boost!
						prj.ParsedModules.UfcsCache.RemoveModuleItems(oldAst);
						prj.ParsedModules.Remove(oldAst.ModuleName);
						oldAst = null;
					}

					if (value != null)
					{
						prj.ParsedModules.AddOrUpdate(value);
						prj.ParsedModules.UfcsCache.CacheModuleMethods(value, new ResolverContextStack(ParseCache, new ResolverContext { ScopedBlock = value }));
					}
				}

				_unboundTree = value;
			}
		}

		public ParseCacheList ParseCache
		{
			get
			{
				var prj = Project as DProject;

				if (prj == null)
					return ParseCacheList.Create(CompilerConfiguration.ASTCache);

				return ParseCacheList.Create(prj.ParsedModules, prj.CompilerConfiguration.ASTCache);
			}
		}

		public CompletionOptions Options
		{
			get { 
				return DSettings.Instance.CompletionOptions; 
			}
		}

		/// <summary>
		/// Variable that indicates if document is parsed currently.
		/// </summary>
		public bool IsParsing { get; protected set; }

		/// <summary>
		/// Variable that is used for the parser loop to recognize user interaction.
		/// So, if the user typed a character, this will be set to true, whereas it later, after the text has become parsed, will be reset
		/// </summary>
		bool KeysTyped = false;
		Thread parseThread = null;
		//bool CanRefreshSemanticHighlightings = false;

		bool isUpdatingLookupDropdowns = false;

		List<string> foldedNodeNames = new List<string>();

		public string ProposedModuleName
		{
			get
			{
				if (HasProject)
					return Path.ChangeExtension(RelativeFilePath, null).Replace('\\', '.');
				else
					return Path.GetFileNameWithoutExtension(FileName);
			}
		}

		public IBlockNode lastSelectedBlock { get; private set; }
		IStatement lastSelectedStatement;

		DispatcherOperation typeLookupUpdateOperation = null;
		//DispatcherOperation showCompletionWindowOperation = null;
		DispatcherOperation parseOperation = null;

		public DMDConfig CompilerConfiguration
		{
			get
			{
				if (HasProject && Project is DProject)
					return (Project as DProject).CompilerConfiguration;
				return DSettings.Instance.DMDConfig();
			}
		}

		internal CompletionWindow completionWindow;
		OverloadInsightWindow insightWindow;
		#endregion

		public DEditorDocument()
		{
			Init();
		}

		public DEditorDocument(string file)
			: base(file)
		{
			Init();
		}

		void Init()
		{
			#region Setup type lookup dropdowns
			// Create a grid which is located at the very top of the editor document
			var stk = new Grid()
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Height = 24,
				VerticalAlignment = VerticalAlignment.Top
			};

			// Give it two columns that have an equal width
			stk.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.5, GridUnitType.Star) });
			stk.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.5, GridUnitType.Star) });

			// Move the editor away from the upper boundary
			Editor.Margin = new Thickness() { Top = stk.Height };

			MainEditorContainer.Children.Add(stk);

			lookup_Types = new ComboBox() { HorizontalAlignment = HorizontalAlignment.Stretch };
			lookup_Members = new ComboBox() { HorizontalAlignment = HorizontalAlignment.Stretch };

			lookup_Types.SelectionChanged += lookup_Types_SelectionChanged;
			lookup_Members.SelectionChanged += lookup_Types_SelectionChanged;

			stk.Children.Add(lookup_Types);
			stk.Children.Add(lookup_Members);

			#region Setup dropdown item template
			var lookupItemTemplate = lookup_Members.ItemTemplate = lookup_Types.ItemTemplate = new DataTemplate { DataType = typeof(DCompletionData) };

			var sp = new FrameworkElementFactory(typeof(StackPanel));
			sp.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
			sp.SetBinding(StackPanel.ToolTipProperty, new Binding("Description"));

			var iTemplate_Img = new FrameworkElementFactory(typeof(Image));
			iTemplate_Img.SetBinding(Image.SourceProperty, new Binding("Image"));
			iTemplate_Img.SetValue(Image.MarginProperty, new Thickness(1, 1, 4, 1));
			sp.AppendChild(iTemplate_Img);

			var iTemplate_Name = new FrameworkElementFactory(typeof(TextBlock));
			iTemplate_Name.SetBinding(TextBlock.TextProperty, new Binding("PureNodeString"));
			sp.AppendChild(iTemplate_Name);

			lookupItemTemplate.VisualTree = sp;
			#endregion

			// Important: Move the members-lookup to column 1
			lookup_Members.SetValue(Grid.ColumnProperty, 1);
			#endregion

			// Register CodeCompletion events
			Editor.TextArea.TextEntering += new System.Windows.Input.TextCompositionEventHandler(TextArea_TextEntering);
			Editor.TextArea.TextEntered += new System.Windows.Input.TextCompositionEventHandler(TextArea_TextEntered);
			Editor.Document.Changed += new EventHandler<ICSharpCode.AvalonEdit.Document.DocumentChangeEventArgs>(Document_Changed);
			Editor.TextArea.Caret.PositionChanged += new EventHandler(TextArea_SelectionChanged);
			Editor.MouseHover += new System.Windows.Input.MouseEventHandler(Editor_MouseHover);
			Editor.MouseHoverStopped += new System.Windows.Input.MouseEventHandler(Editor_MouseHoverStopped);

			Editor.TextArea.IndentationStrategy = indentationStrategy = new DIndentationStrategy(this);
			foldingManager = ICSharpCode.AvalonEdit.Folding.FoldingManager.Install(Editor.TextArea);

			#region Init context menu
			var cm = new ContextMenu();
			Editor.ContextMenu = cm;

			var cmi = new MenuItem() { Header = "Add import directive", ToolTip = "Add an import directive to the document if type cannot be resolved currently" };
			cmi.Click += ContextMenu_AddImportStatement_Click;
			cm.Items.Add(cmi);

			cmi = new MenuItem() { Header = "Go to definition", ToolTip = "Go to the definition that defined the currently hovered item" };
			cmi.Click += new System.Windows.RoutedEventHandler(ContextMenu_GotoDefinition_Click);
			cm.Items.Add(cmi);

			cmi = new MenuItem()
			{
				Header = "Toggle Breakpoint",
				ToolTip = "Toggle breakpoint on the currently selected line",
				Command = D_IDE.Core.Controls.IDEUICommands.ToggleBreakpoint
			};
			cm.Items.Add(cmi);

			cm.Items.Add(new Separator());

			cmi = new MenuItem()
			{
				Header = "Comment selection",
				ToolTip = "Comment out current selection. If nothing is selected, the current line will be commented only",
				Command = D_IDE.Core.Controls.IDEUICommands.CommentBlock
			};
			cm.Items.Add(cmi);

			cmi = new MenuItem()
			{
				Header = "Uncomment selection",
				ToolTip = "Uncomment current block. The nearest comment tags will be removed.",
				Command = D_IDE.Core.Controls.IDEUICommands.UncommentBlock
			};
			cm.Items.Add(cmi);

			cm.Items.Add(new Separator());

			cmi = new MenuItem() { Header = "Cut", Command = System.Windows.Input.ApplicationCommands.Cut };
			cm.Items.Add(cmi);

			cmi = new MenuItem() { Header = "Copy", Command = System.Windows.Input.ApplicationCommands.Copy };
			cm.Items.Add(cmi);

			cmi = new MenuItem() { Header = "Paste", Command = System.Windows.Input.ApplicationCommands.Paste };
			cm.Items.Add(cmi);
			#endregion

			//CommandBindings.Add(new CommandBinding(IDEUICommands.ReformatDoc,ReformatFileCmd));
			CommandBindings.Add(new CommandBinding(IDEUICommands.CommentBlock, CommentBlock));
			CommandBindings.Add(new CommandBinding(IDEUICommands.UncommentBlock, UncommentBlock));
			CommandBindings.Add(new CommandBinding(IDEUICommands.CtrlSpaceCompletion, CtrlSpaceCompletion));

			// Init parser loop
			parseThread = new Thread(ParserLoop) { IsBackground=true, Name="ParseLoop "+ProposedModuleName };
			parseThread.Start();
		}

		/*
		public override void Reload()
		{
			base.Reload();
			CanRefreshSemanticHighlightings = true;
		}*/

		public void UpdateFoldings()
		{
			if (foldingManager == null)
				return;

			foreach (var fs in foldingManager.AllFoldings)
				if (fs.IsFolded)
					foldedNodeNames.Add(fs.Tag as string);

			foldingManager.Clear();

			updateFoldingsInternal(SyntaxTree);
		}

		void updateFoldingsInternal(IBlockNode block)
		{
			if (block == null)
				return;

			if (!(block is IAbstractSyntaxTree) && !block.BlockStartLocation.IsEmpty && block.EndLocation > block.BlockStartLocation)
			{
				var startOff=Editor.Document.GetOffset(block.BlockStartLocation.Line, block.BlockStartLocation.Column);
				var endOff=Editor.Document.GetOffset(block.EndLocation.Line, block.EndLocation.Column);

				if (startOff < endOff)
				{
					var fn = foldingManager.CreateFolding(startOff, endOff);
					//fn.Title = (block as AbstractNode).ToString(false,false);
					var nn = fn.Tag = block.ToString();

					if (foldedNodeNames.Contains(nn))
						fn.IsFolded = true;
				}
			}

			if (block.Count > 0)
				foreach (var n in block)
					updateFoldingsInternal(n as IBlockNode);
		}

		void lookup_Types_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (isUpdatingLookupDropdowns || e.AddedItems.Count < 1)
				return;

			var completionData = e.AddedItems[0] as DCompletionData;

			if (completionData == null)
				return;

			Editor.TextArea.Caret.Position = new TextViewPosition(completionData.Node.Location.Line, completionData.Node.Location.Column);
			Editor.TextArea.Caret.BringCaretToView();
			Editor.Focus();
		}

		#region Code operations
		void CtrlSpaceCompletion(object s, ExecutedRoutedEventArgs e)
		{
			ShowCodeCompletionWindow(null);
		}

		void CommentBlock(object s, ExecutedRoutedEventArgs e)
		{
			if (false)
			{
				/*
				int cOff = Editor.CaretOffset;
				Editor.Text = Commenting.comment(Editor.Text, Editor.SelectionStart, Editor.SelectionStart + Editor.SelectionLength);
				Editor.CaretOffset = cOff;
				var loc = Editor.Document.GetLocation(cOff);
				Editor.ScrollTo(loc.Line, loc.Column);*/
			}
			else
			{
				if (Editor.SelectionLength < 1)
				{
					Editor.Document.Insert(Editor.Document.GetOffset(Editor.TextArea.Caret.Line, 0), "//");
				}
				else
				{
					Editor.Document.UndoStack.StartUndoGroup();

					var ctxt = CaretContextAnalyzer.GetTokenContext(Editor.Text, Editor.SelectionStart);
					
					if (ctxt!=TokenContext.BlockComment &&
						ctxt!= TokenContext.NestedComment)
					{
						Editor.Document.Insert(Editor.SelectionStart + Editor.SelectionLength, "*/");
						Editor.Document.Insert(Editor.SelectionStart, "/*");
					}
					else
					{
						Editor.Document.Insert(Editor.SelectionStart + Editor.SelectionLength, "+/");
						Editor.Document.Insert(Editor.SelectionStart, "/+");
					}

					Editor.SelectionLength -= 2;

					Editor.Document.UndoStack.EndUndoGroup();
				}
			}
		}

		void UncommentBlock(object s, ExecutedRoutedEventArgs e)
		{
			var CaretOffset = Editor.CaretOffset;

			if (CaretOffset < 2) 
				return;

			int commStart = CaretOffset;
			int commEnd = 0;
			var context = CaretContextAnalyzer.GetTokenContext(Editor.Text, CaretOffset, out commStart, out commEnd);

			if (commStart < 0)
				return;

			// Remove single-line comments
			if (context == TokenContext.LineComment)
			{
				int removedSlashCount = 0;

				while(Editor.Document.GetCharAt(commStart+removedSlashCount)=='/')
					removedSlashCount++;

				Editor.Document.Remove(commStart,removedSlashCount);
				return;
			}

			#region If no single-line comment was removed, delete multi-line comment block tags

			if (context != TokenContext.BlockComment &&
				context != TokenContext.NestedComment)
				return;

			if (commEnd < 0) 
				return;

			int removeCount_initialStarToken = 1;
			int removeCount_finalStarToken = 1;

			char starToken = context== TokenContext.NestedComment?'+':'*';

			// Find and strip all leading and trailing * (+ on nested comments)
			while (Editor.Document.GetCharAt(commStart + removeCount_initialStarToken) == starToken)
				removeCount_initialStarToken++;

			while (Editor.Document.GetCharAt(commEnd - 1 - removeCount_finalStarToken) == starToken)
				removeCount_finalStarToken++;

			Editor.Document.UndoStack.StartUndoGroup();

			Editor.Document.Remove(commEnd-removeCount_finalStarToken, removeCount_finalStarToken);
			Editor.Document.Remove(commStart, removeCount_initialStarToken);

			Editor.Document.UndoStack.EndUndoGroup();
			#endregion
		}

		void ContextMenu_GotoDefinition_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
				if (SyntaxTree == null)
					return;

				var rr = DResolver.ResolveType(this,
					new ResolverContextStack(ParseCache, new ResolverContext
					{
						ScopedBlock = lastSelectedBlock,
						ScopedStatement = lastSelectedStatement
					}) { ContextIndependentOptions = ResolutionOptions.ReturnMethodReferencesOnly },
					DResolver.AstReparseOptions.AlsoParseBeyondCaret | DResolver.AstReparseOptions.OnlyAssumeIdentifierList);

				AbstractType res = null;
				// If there are multiple types, show a list of those items
				if (rr != null && rr.Length > 1)
				{
					var dlg = new ListSelectionDialog();

					var l = new List<string>();
					int j = 0;
					foreach (var i in rr)
						l.Add("(" + (++j).ToString() + ") " + i.ToString()); // Bug: To make items unique (which is needed for the listbox to run properly), it's needed to add some kind of an identifier to the beginning of the string
					dlg.List.ItemsSource = l;

					dlg.List.SelectedIndex = 0;

					if (dlg.ShowDialog().Value)
					{
						res = rr[dlg.List.SelectedIndex];
					}
				}
				else if (rr.Length == 1)
					res = rr[0];
				else
				{
					MessageBox.Show("No symbol found!");
					return;
				}

				INode n = null;

				if (res is DSymbol)
					n = ((DSymbol)res).Definition;
				else
				{
					MessageBox.Show("Select valid symbol!");
					return;
				}

				var mod = n.NodeRoot as IAbstractSyntaxTree;
				if (mod == null)
					return;
				CoreManager.Instance.OpenFile(mod.FileName, n.Location.Line, n.Location.Column);
			}
			catch { }
		}

		void ContextMenu_AddImportStatement_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
				if (SyntaxTree == null)
					return;

				var rr = DResolver.ResolveType(this,
					new ResolverContextStack(ParseCache, new ResolverContext { 
						ScopedBlock = lastSelectedBlock, 
						ScopedStatement=lastSelectedStatement
					}) { ContextIndependentOptions = ResolutionOptions.ReturnMethodReferencesOnly },
					DResolver.AstReparseOptions.OnlyAssumeIdentifierList | DResolver.AstReparseOptions.AlsoParseBeyondCaret);

				AbstractType res = null;
				// If there are multiple types, show a list of those items
				if (rr != null && rr.Length > 1)
				{
					var dlg = new ListSelectionDialog();

					var l = new List<string>();
					int j = 0;
					foreach (var i in rr)
						l.Add("(" + (++j).ToString() + ") " + i.ToString()); // Bug: To make items unique (which is needed for the listbox to run properly), it's needed to add some kind of an identifier to the beginning of the string
					dlg.List.ItemsSource = l;

					dlg.List.SelectedIndex = 0;

					if (dlg.ShowDialog().Value)
					{
						res = rr[dlg.List.SelectedIndex];
					}
				}
				else if (rr.Length == 1)
					res = rr[0];
				else
				{
					MessageBox.Show("No symbol found!");
					return;
				}

				var n = DResolver.GetResultMember(res);

				if (n == null)
				{
					MessageBox.Show("Select valid symbol!");
					return;
				}

				var mod = n.NodeRoot as DModule;
				if (mod == null)
					return;

				if (mod == SyntaxTree)
				{
					MessageBox.Show("Symbol is part of the current module. No import required!");
					return;
				}

				bool alreadyAdded= false;

				foreach(var sstmt in mod.StaticStatements)
					if (sstmt is ImportStatement)
					{
						var impStmt=(ImportStatement)sstmt;

						foreach (var imp in impStmt.Imports)
							if (imp.ModuleIdentifier.ToString() == mod.ModuleName)
							{
								alreadyAdded = true;
								break;
							}

						if (impStmt.ImportBinding != null && impStmt.ImportBinding.Module.ModuleIdentifier.ToString() == mod.ModuleName)
							alreadyAdded = true;

						if (alreadyAdded)
							break;
					}

				if (alreadyAdded)
				{
					MessageBox.Show("Module " + mod.ModuleName + " already imported!");
					return;
				}

				var loc = DParser.FindLastImportStatementEndLocation(Editor.Text);
				Editor.Document.BeginUpdate();
				Editor.Document.Insert(Editor.Document.GetOffset(loc.Line + 1, 0), "import " + mod.ModuleName + ";\r\n");
				KeysTyped = true;
				Editor.Document.EndUpdate();
			}
			catch { }
		}
		#endregion

		void Document_Changed(object sender, ICSharpCode.AvalonEdit.Document.DocumentChangeEventArgs e)
		{
			KeysTyped = true;
			Modified = true;
		}

		#region Code Completion
		/// <summary>
		/// Parses the current document content
		/// </summary>
		public void Parse()
		{
			IsParsing = true;
			string code = "";

			Dispatcher.Invoke(new Action(() => code = Editor.Text));
			GC.Collect();
			DModule newAst = null;
			try
			{
				SyntaxTree = null;
				using (var sr = new StringReader(code))
					using (var parser = DParser.Create(sr))
					{
						var sw = new Stopwatch();
						code = null;

						sw.Restart();

						newAst = parser.Parse();

						sw.Stop();

						SyntaxTree = newAst;

						ParseTime = sw.Elapsed.TotalMilliseconds;
					}
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex, ErrorType.Warning, ErrorOrigin.Parser);
			}

			lastSelectedBlock = null;
			lastSelectedStatement = null;

			if (newAst != null)
			{
				newAst.FileName = AbsoluteFilePath;
				newAst.ModuleName = ProposedModuleName;
			}
			//TODO: Make semantic highlighting 1) faster and 2) redraw symbols immediately
			UpdateSemanticHighlighting(true);
			//CanRefreshSemanticHighlightings = false;

			UpdateTypeLookupData();
			
			if (parseOperation != null && parseOperation.Status != DispatcherOperationStatus.Completed)
				parseOperation.Abort();

			parseOperation = Dispatcher.BeginInvoke(new Action(() =>
			{
				try
				{
					if (GlobalProperties.Instance.ShowSpeedInfo)
						CoreManager.Instance.MainWindow.SecondLeftStatusText = 
							Math.Round((decimal)ParseTime, 3).ToString() + "ms (Parsing duration)";

					UpdateFoldings();
					CoreManager.ErrorManagement.RefreshErrorList();
					RefreshErrorHighlightings();
				}
				catch (Exception ex) { ErrorLogger.Log(ex, ErrorType.Warning, ErrorOrigin.System); }
			}));

			IsParsing = false;
		}

		public void ParserLoop()
		{
			// Initially parse the document
			Parse();
			bool HasBeenUpdatingParseCache = false;
			KeysTyped = false;

			while (true)
			{
				var cc = CompilerConfiguration;

				// While no keys were typed, do nothing
				while (!KeysTyped)
				{
					if (HasBeenUpdatingParseCache && !cc.ASTCache.IsParsing)
					{
						UpdateSemanticHighlightings(true); // Perhaps new errors were detected
						HasBeenUpdatingParseCache = false;
					}
					else if (cc.ASTCache.IsParsing)
						HasBeenUpdatingParseCache = true;

					Thread.Sleep(50);
				}

				// Reset keystyped state for waiting again
				KeysTyped = false;

				// If a key was typed, wait.
				Thread.Sleep(500);

				// If no other key was typed after waiting, parse the file
				if (KeysTyped)
					continue;

				// Prevent parsing it again; Assign 'false' to it before parsing the document, so if something was typed while parsing, it'll simply parse again
				KeysTyped = false;

				Parse();
			}
		}

		/// <summary>
		/// Updates all semantic code highlightings/errors in all open editors
		/// </summary>
		/// <param name="RefreshErrorList"></param>
		public static void UpdateSemanticHighlightings(bool RefreshErrorList = false)
		{
			IEnumerable<AbstractEditorDocument> editors = null;
			CoreManager.Instance.MainWindow.Dispatcher.Invoke(new Action(() =>
				editors=CoreManager.Instance.Editors
			));

			if(editors!=null)
				foreach (var ed in editors)
					if (ed is DEditorDocument)
						(ed as DEditorDocument).UpdateSemanticHighlighting(false);

			// Only refresh it once
			if(RefreshErrorList)
				CoreManager.Instance.MainWindow.Dispatcher.Invoke(new Action(() =>
					CoreManager.ErrorManagement.RefreshErrorList()
					));
		}

		public void UpdateSemanticHighlighting(bool RefreshErrorList = false)
		{
			if (!DSettings.Instance.UseSemanticHighlighting)
			{
				Util.ExecuteOnUIThread(() =>
				{
					foreach (var marker in MarkerStrategy.TextMarkers.ToArray())
						if (marker is CodeSymbolMarker)
							marker.Delete();
				},false);

				return;
			}

			if (SyntaxTree == null || CompilerConfiguration.ASTCache.IsParsing)
				return;

			var sw=new Stopwatch();
			sw.Start();

			var res = TypeReferenceFinder.Scan(SyntaxTree, ParseCache);

			sw.Stop();

			#region Step 3: Create/Update markers
			try
			{
				Dispatcher.Invoke(new Action<TypeReferencesResult, Stopwatch>
					((TypeReferencesResult results, Stopwatch highPrecTimer) =>
			{
				// Clear old markers
				foreach (var marker in MarkerStrategy.TextMarkers.ToArray())
					if (marker is CodeSymbolMarker)
						marker.Delete();

				int len=0;
				if (results.TypeMatches.Count != 0)
					foreach (var kv in results.TypeMatches)
						if (kv.Location.Line > 0)
						{
							var m = new CodeSymbolMarker(this, kv, DeepASTVisitor.ExtractIdLocation(kv, out len), len);
							MarkerStrategy.Add(m);

							m.Redraw();
						}
				
				SemanticErrors.Clear();
				/*
				if (results.UnresolvedIdentifiers.Count != 0 && DSettings.Instance.UseSemanticErrorHighlighting)
					foreach (var id in results.UnresolvedIdentifiers)
						if (id.Location.Line > 0)
						{
							SemanticErrors.Add(new DSemanticError
							{
								FileName=AbsoluteFilePath,
								IsSemantic = true,
								Message = id.ToString() + " couldn't get resolved",
								Location = DeepASTVisitor.ExtractIdLocation(id, out len),
								MarkerColor=Colors.Blue,
								Length = len
							});
						}
				*/
				if (RefreshErrorList)
					CoreManager.ErrorManagement.RefreshErrorList();

				if(GlobalProperties.Instance.ShowSpeedInfo)
					CoreManager.Instance.MainWindow.LeftStatusText =
						Math.Round((decimal)highPrecTimer.ElapsedMilliseconds, 2).ToString() +
						"ms (Semantic Highlighting)";
			}), DispatcherPriority.Background, res, sw);
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex, ErrorType.Warning, ErrorOrigin.System);
			}
			#endregion
		}

		public class CodeSymbolMarker : TextMarker
		{
			public readonly EditorDocument EditorDocument;
			public readonly ISyntaxRegion Id;
			INode rr;
			public INode ResolveResult
			{
				get { return rr; }
				set {
					rr = value;

					if (rr is IAbstractSyntaxTree)
						ForegroundColor = Colors.DarkRed;
					else
						ForegroundColor = Color.FromRgb(0x2b, 0x91, 0xaf);
				}
			}

			public CodeSymbolMarker(EditorDocument EditorDoc, ISyntaxRegion Id, int StartOffset, int Length)
				: base(EditorDoc.MarkerStrategy, StartOffset, Length)
			{
				this.EditorDocument = EditorDoc;
				this.Id = Id;
				Init();
			}
			public CodeSymbolMarker(EditorDocument EditorDoc, ISyntaxRegion Id, CodeLocation Location, int length)
				: this(EditorDoc, Id, EditorDoc.Editor.Document.GetOffset(Location.Line, Location.Column), length)
			{
			}

			void Init()
			{
				this.MarkerType = TextMarkerType.None;

				ForegroundColor = Color.FromRgb(0x2b, 0x91, 0xaf);
			}
		}

		readonly List<GenericError> SemanticErrors = new List<GenericError>();

		public override System.Collections.Generic.IEnumerable<GenericError> ParserErrors
		{
			get
			{
				if (SyntaxTree != null)
				{
					var l = new List<GenericError>(SyntaxTree.ParseErrors.Count);
					foreach (var pe in SyntaxTree.ParseErrors)
						l.Add(new DParseError(pe) { Project = HasProject ? Project : null, FileName = AbsoluteFilePath });
					l.AddRange(SemanticErrors);
					return l;
				}
				return null;
			}
		}

		public CodeLocation CaretLocation
		{
			get { return new CodeLocation(Editor.TextArea.Caret.Column, Editor.TextArea.Caret.Line); }
		}

		void _insertTypeDataInternal(IBlockNode Parent, ref DCompletionData selectedItem, List<DCompletionData> types)
		{
			if (Parent != null)
				foreach (var n in Parent)
				{
					var completionData = new DCompletionData(n);
					if (selectedItem == null && CaretLocation >= n.Location && CaretLocation <= n.EndLocation)
						selectedItem = completionData;
					types.Add(completionData);
				}
		}

		/// <summary>
		/// If different code block was selected, 
		/// update the list of items that are available in the current scope
		/// </summary>
		public void UpdateTypeLookupData()
		{
			try
			{
				// Update highlit bracket offsets
				if (DSettings.Instance.EnableMatchingBracketHighlighting)
					CurrentlyHighlitBrackets = DBracketSearcher.SearchBrackets(Editor.Document, Editor.CaretOffset, Editor.TextArea.Caret.Location);
				else
					CurrentlyHighlitBrackets = null;


				if (SyntaxTree == null)
				{
					lookup_Members.ItemsSource = lookup_Types.ItemsSource = null;
					return;
				}

				var curBlock = DResolver.SearchBlockAt(SyntaxTree, CaretLocation, out lastSelectedStatement);

				if (curBlock == null)
					curBlock = SyntaxTree;

				if (typeLookupUpdateOperation != null && typeLookupUpdateOperation.Status != DispatcherOperationStatus.Completed)
					typeLookupUpdateOperation.Abort();

				lastSelectedBlock = curBlock;

				typeLookupUpdateOperation = Dispatcher.BeginInvoke(new Action(() =>
				{
					try
					{
						#region Update the type & member selectors
						isUpdatingLookupDropdowns = true; // Temporarily disable SelectionChanged event handling

						// First fill the Types-Dropdown
						var types = new List<DCompletionData>();
						ICompletionData selectedItem = null;
						var l1 = new List<INode> { SyntaxTree };
						var l2 = new List<INode>();

						while (l1.Count > 0)
						{
							foreach (var n in l1)
							{
								// Show all type declarations of the current module
								if (n is DClassLike || n is DEnum)
								{
									var completionData = new DCompletionData(n);
									if (CaretLocation >= n.Location && CaretLocation <= n.EndLocation)
									{
										selectedItem = completionData;
										curBlock = n as IBlockNode;
									}
									types.Add(completionData);
								}

								if (n is IBlockNode)
								{
									var ch = (n as IBlockNode).Children;
									if (ch != null)
										l2.AddRange(ch);
								}
							}

							l1.Clear();
							l1.AddRange(l2);
							l2.Clear();
						}

						if (selectedItem == null && SyntaxTree != null)
							curBlock = SyntaxTree;

						// For better usability, pre-sort items
						try
						{
							types.Sort();
						}
						catch { }

						lookup_Types.ItemsSource = types;
						lookup_Types.SelectedItem = selectedItem;

						if (curBlock is IBlockNode)
						{
							selectedItem = null;
							// Fill the Members-Dropdown
							var members = new List<DCompletionData>();

							// Search a parent class to show all this one's members and to select that member where the caret currently is located
							var watchedParent = curBlock as IBlockNode;

							while (watchedParent != null && !(watchedParent is DClassLike || watchedParent is DEnum || watchedParent is IAbstractSyntaxTree))
								watchedParent = watchedParent.Parent as IBlockNode;

							if (watchedParent != null)
								lock(watchedParent)
									foreach (var n in watchedParent)
									{
										if (n == null)
											continue;

										var cData = new DCompletionData(n);
										if (selectedItem == null && cData.Node!=null && CaretLocation >= cData.Node.Location && CaretLocation < cData.Node.EndLocation)
											selectedItem = cData;
										members.Add(cData);
									}

							try
							{
								members.Sort();
							}
							catch { }

							lookup_Members.ItemsSource = members;
							lookup_Members.SelectedItem = selectedItem;
						}
						else
						{
							lookup_Members.ItemsSource = null;
							lookup_Members.SelectedItem = null;
						}

						isUpdatingLookupDropdowns = false;
						#endregion
					}
					catch (Exception ex) { ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Parser); }
				}), DispatcherPriority.Background);
			}
			catch (Exception ex) { ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Parser); }
		}

		void TextArea_SelectionChanged(object sender, EventArgs e)
		{
			UpdateTypeLookupData();
		}

		/// <summary>
		/// Key: Path of the accessed item
		/// </summary>
		public readonly Dictionary<string, string> LastSelectedCCItems = new Dictionary<string, string>();
		ICompletionData lastSelectedCompletionData = null;

		/// <summary>
		/// Needed for pre-selection when completion list becomes opened next time
		/// </summary>
		string lastCompletionListResultPath = "";

		void ShowCodeCompletionWindow(string EnteredText)
		{
			try
			{
				if ((EnteredText!=null && EnteredText.Length>0 && !(
					EnteredText=="@"||
					EnteredText=="(" ||
					EnteredText==" " || 
					AbstractCompletionProvider.IsIdentifierChar(EnteredText[0]) || 
					EnteredText[0] == '.')) || 
					!DCodeCompletionSupport.CanShowCompletionWindow(this) || 
					Editor.IsReadOnly)
					return;
				
				/*
				 * Note: Once we opened the completion list, it's not needed to care about a later refill of that list.
				 * The completionWindow will search the items that are partly typed into the editor automatically and on its own.
				 * - So there's just an initial filling required.
				 */

				if (completionWindow != null)
					return;
				/*
				if (showCompletionWindowOperation != null &&showCompletionWindowOperation.Status != DispatcherOperationStatus.Completed)
					showCompletionWindowOperation.Abort();
				*/

				var cData = new List<ICompletionData>();

				var sw = new Stopwatch();
				sw.Start();
				DCodeCompletionSupport.BuildCompletionData(
					this,
					cData,
					EnteredText,
					out lastCompletionListResultPath);
				sw.Stop();

				if(GlobalProperties.Instance.ShowSpeedInfo)
					CoreManager.Instance.MainWindow.ThirdStatusText = sw.ElapsedMilliseconds + "ms (Completion)";

				// If no data present, return
				if (cData.Count < 1)
				{
					completionWindow = null;
					return;
				}
				else
				{
					// Init completion window

					completionWindow = new CompletionWindow(Editor.TextArea);
					completionWindow.CompletionList.InsertionRequested += new EventHandler(CompletionList_InsertionRequested);
					//completionWindow.CloseAutomatically = true;

					//HACK: Fill in data directly instead of iterating through the data
					foreach (var e in cData)
						completionWindow.CompletionList.CompletionData.Add(e);
				}

				// Care about item pre-selection
				var selectedString = "";

				if (lastCompletionListResultPath != null &&
					!LastSelectedCCItems.TryGetValue(lastCompletionListResultPath, out selectedString))
						LastSelectedCCItems.Add(lastCompletionListResultPath, "");

				if (!string.IsNullOrEmpty(selectedString))
				{
					// Prevent hiding all items that are not named as 'selectedString' .. after having selected our item, reset the filter property
					completionWindow.CompletionList.IsFiltering = false;
					completionWindow.CompletionList.SelectItem(selectedString);
					completionWindow.CompletionList.IsFiltering = true;
				}
				else // Select first item by default
					completionWindow.CompletionList.SelectedItem = completionWindow.CompletionList.CompletionData[0];

				completionWindow.Closed += (object o, EventArgs _e) => { 
					// 'Backup' the selected completion data
					lastSelectedCompletionData = completionWindow.CompletionList.SelectedItem;
					completionWindow = null; // After the window closed, reset it to null
				};
				completionWindow.Show();
			}
			catch (Exception ex) { ErrorLogger.Log(ex); completionWindow = null; }
		}

		void CompletionList_InsertionRequested(object sender, EventArgs e)
		{
			// After item got inserted, overwrite last-selected-item string
			if (lastCompletionListResultPath != null && lastSelectedCompletionData!=null)
				LastSelectedCCItems[lastCompletionListResultPath] = lastSelectedCompletionData.Text;
		}

		public void CloseCompletionPopups()
		{
			if (completionWindow != null)
			{
				completionWindow.Close();
				completionWindow = null;
			}

			if (insightWindow != null)
			{
				insightWindow.Close();
				insightWindow = null;
			}
		}

		/// <summary>
		/// Shows the popup that displays the currently accessed function and its parameters
		/// </summary>
		/// <param name="EnteredText"></param>
		void ShowInsightWindow(string EnteredText)
		{
			if (!DSettings.Instance.UseMethodInsight ||
				(EnteredText == "," && insightWindow != null && insightWindow.IsVisible))
				return;

			try
			{
				var data = D_IDE.D.CodeCompletion.DMethodOverloadProvider.Create(this);

				if (data == null)
					return;

				insightWindow = new OverloadInsightWindow(Editor.TextArea);
				insightWindow.Provider = data;
				insightWindow.Closed += new EventHandler(insightWindow_Closed);

				var tt = new ToolTip();
				(insightWindow as Control).Background = tt.Background;

				insightWindow.Show();

				// Reposition the popup window to stick directly under the identifier expression
				if (data.ParameterData.MethodIdentifier is ISyntaxRegion)
				{
					var loc=((ISyntaxRegion)data.ParameterData.MethodIdentifier).Location;
					var visPos = Editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(loc.Line, loc.Column), ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineBottom);

					visPos = Editor.TextArea.TextView.PointToScreen(visPos);

					//insightWindow.Top = visPos.Y;
					insightWindow.Left = visPos.X;
				}
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }
		}

		void insightWindow_Closed(object sender, EventArgs e)
		{
			insightWindow = null;
		}

		public bool CanShowCodeCompletionPopup
		{
			get
			{
				return
					DSettings.Instance.UseCodeCompletion &&	
					SyntaxTree != null && 
					DCodeCompletionSupport.CanShowCompletionWindow(this);
			}
		}

		void TextArea_TextEntering(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			if (completionWindow != null)
			{
				// If entered key isn't part of the identifier anymore, close the completion window and insert the item text.
				if (!AbstractCompletionProvider.IsIdentifierChar(e.Text[0]))
					if (DSettings.Instance.ForceCodeCompetionPopupCommit)
						completionWindow.CompletionList.RequestInsertion(e);
					else
						completionWindow.Close();
			}

			// Return if there are parser errors - just to prevent crashes
			if (!CanShowCodeCompletionPopup)
				return;

			if (string.IsNullOrWhiteSpace(e.Text))
				return;

			// Note: Show completion window even before the first key has been processed by the editor!
			else if (e.Text=="@" || char.IsLetter(e.Text[0]) || e.Text=="_")
				ShowCodeCompletionWindow(e.Text);
		}

		void TextArea_TextEntered(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			// If typed a block-related char, update line indentation
			if (e.Text == "{" || e.Text == "}" || e.Text == ":")
			{
				int lastBegin;
				int lastEnd;
				var caretCtxt = CaretContextAnalyzer.GetTokenContext(ModuleCode, CaretOffset, out lastBegin, out lastEnd);

				if (lastBegin >= 0 && caretCtxt != TokenContext.None)
					return;

				indentationStrategy.UpdateIndentation(e.Text);
			}

			// Show the cc window after the dot has been inserted in the text because the cc win would overwrite it anyway
			else if ((e.Text == "." || e.Text==" ") && CanShowCodeCompletionPopup)
				ShowCodeCompletionWindow(e.Text);

			if (e.Text == "," || e.Text == "(" || e.Text == "!")
				ShowInsightWindow(e.Text);

			else if (e.Text == ")" && insightWindow != null && insightWindow.IsLoaded)
				insightWindow.Close();
		}
		#endregion

		#region Editor events

		#region Document ToolTips
		void Editor_MouseHoverStopped(object sender, System.Windows.Input.MouseEventArgs e)
		{
			editorToolTip.IsOpen = false;
		}

		void Editor_MouseHover(object sender, System.Windows.Input.MouseEventArgs e)
		{
			try
			{
				var edpos = e.GetPosition(Editor);
				var pos = Editor.GetPositionFromPoint(edpos);
				if (pos.HasValue)
				{
					int offset = Editor.Document.GetOffset(pos.Value.Line, pos.Value.Column);
					// Avoid showing a tooltip if the cursor is located after a line-end
					var vpos = Editor.TextArea.TextView.GetVisualPosition(
						new TextViewPosition(
							pos.Value.Line, 
							Editor.Document.GetLineByNumber(pos.Value.Line).TotalLength), 
						ICSharpCode.AvalonEdit.Rendering.VisualYPosition.LineMiddle);
					// Add TextView position to Editor-related point
					vpos = Editor.TextArea.TextView.TranslatePoint(vpos, Editor);

					var ttArgs = new ToolTipRequestArgs(edpos.X <= vpos.X, pos.Value);
					try
					{
						bool handled = false;
						//TODO: Show debuggee locals when debugging
						// Prefer showing error markers' error messages
						foreach (var tm in MarkerStrategy.TextMarkers)
							if (tm is ErrorMarker && tm.StartOffset <= offset && offset <= tm.EndOffset)
							{
								var em = tm as ErrorMarker;

								ttArgs.ToolTipContent = em.Error.Message;

								handled = true;
								break;
							}

						if (!handled)
						{
							var sw = new Stopwatch();
							sw.Start();

							DCodeCompletionSupport.BuildToolTip(this, ttArgs);

							sw.Stop();

							if (GlobalProperties.Instance.ShowSpeedInfo)
								CoreManager.Instance.MainWindow.ThirdStatusText = sw.Elapsed.TotalMilliseconds + "ms (Tooltip)";
						}
					}
					catch (Exception ex)
					{
						ErrorLogger.Log(ex);
						return;
					}

					// If no content present, close and return
					if (ttArgs.ToolTipContent == null)
					{
						editorToolTip.IsOpen = false;
						return;
					}

					editorToolTip.PlacementTarget = this; // required for property inheritance
					editorToolTip.Content = ttArgs.ToolTipContent;
					editorToolTip.IsOpen = true;
					e.Handled = true;
				}
			}
			catch { }
		}
		#endregion
		#endregion

		/// <summary>
		/// Reformats all code lines.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void ReformatFileCmd(object sender, ExecutedRoutedEventArgs e)
		{
			indentationStrategy.IndentLines(Editor.Document, 1, Editor.Document.LineCount);
		}

		public string ModuleCode
		{
			get
			{
				return Editor.Document.Text;
			}
			set
			{
				Editor.Document.Text = value;
			}
		}

		public int CaretOffset
		{
			get
			{
				return Editor.CaretOffset;
			}
			set
			{
				Editor.CaretOffset = value;
			}
		}
	}

	public class ToolTipRequestArgs
	{
		public ToolTipRequestArgs(bool isDoc, TextViewPosition pos)
		{
			InDocument = isDoc;
			Position = pos;
		}

		public bool InDocument { get; protected set; }
		public TextViewPosition Position { get; protected set; }
		public int Line { get { return Position.Line; } }
		public int Column { get { return Position.Column; } }
		public object ToolTipContent { get; set; }
	}
}
