﻿using System;
using System.Windows;
using D_IDE.Core;

namespace D_IDE.Dialogs
{
	/// <summary>
	/// Interaktionslogik für SearchAndReplaceDlg.xaml
	/// </summary>
	public partial class SearchAndReplaceDlg : Window
	{
		public SearchAndReplaceDlg()
		{
			InitializeComponent();
			LoadSearchOptions();
		}

		/// <summary>
		/// If this window is floating and located above the caret position of the currently edited file, re-locate it
		/// </summary>
		public void SetWindowPositionNextToCurrentCaret()
		{
			/* 
			 * 1) Check if window is floating
			 * 2) Get screen position of the caret
			 * 3) Relocate the window
			 */

			// 1)
			if (!(IDEManager.Instance.CurrentEditor is EditorDocument))
				return;

			// 2)
			var ed = IDEManager.Instance.CurrentEditor as EditorDocument;
            if (!ed.Editor.IsVisible)
                return;

			var cr_relative=ed.Editor.TextArea.Caret.CalculateCaretRectangle();
			var cr_absolute = ed.Editor.PointToScreen(cr_relative.Location);

			// 3)
			var CaretRealtedToThis=PointFromScreen(cr_absolute);
			bool IsInWindow = (CaretRealtedToThis.X>=0 && CaretRealtedToThis.X < Width) || 
								(CaretRealtedToThis.Y>=0 && CaretRealtedToThis.Y < Height);

			if (IsInWindow)
			{
				Left = cr_absolute.X+100;

				if (Left + Width > SystemParameters.PrimaryScreenWidth)
					Left = cr_absolute.X - 100 - Width;
			}
		}

		public void LoadSearchOptions()
		{
			var fsm = IDEManager.FileSearchManagement.Instance;

			comboBox_InputString.Text = fsm.CurrentSearchStr;
			comboBox_ReplaceString.Text = fsm.CurrentReplaceStr;

			comboBox_InputString.ItemsSource = fsm.LastSearchStrings;
			comboBox_ReplaceString.ItemsSource = fsm.LastReplaceStrings;

			comboBox_SearchLocation.SelectedIndex = (int)fsm.CurrentSearchLocation;

            checkBox_EscapeSequences.IsChecked = fsm.SearchOptions.HasFlag(IDEManager.FileSearchManagement.SearchFlags.EscapeSequences);
			checkBox_CaseSensitive.IsChecked = fsm.SearchOptions.HasFlag(IDEManager.FileSearchManagement.SearchFlags.CaseSensitive);
			checkBox_SearchUpward.IsChecked = fsm.SearchOptions.HasFlag(IDEManager.FileSearchManagement.SearchFlags.Upward);
			checkBox_WordOnly.IsChecked = fsm.SearchOptions.HasFlag(IDEManager.FileSearchManagement.SearchFlags.FullWord);
            checkBox_WrapAround.IsChecked = fsm.SearchOptions.HasFlag(IDEManager.FileSearchManagement.SearchFlags.Wrap);

			var ed = IDEManager.Instance.CurrentEditor as EditorDocument;
			if (ed != null && ed.Editor.SelectionLength>0)
			{
				comboBox_InputString.Text = ed.Editor.SelectedText;
			}

            optionsChanged = true;
		}

		public void ApplySearchOptions()
        {
            var fsm = IDEManager.FileSearchManagement.Instance;

            fsm.CurrentSearchStr = comboBox_InputString.Text;
            fsm.CurrentReplaceStr = comboBox_ReplaceString.Text;

            if (optionsChanged)
            {
                fsm.CurrentSearchLocation = (IDEManager.FileSearchManagement.SearchLocations)comboBox_SearchLocation.SelectedIndex;

                fsm.SearchOptions = 0;

                if (checkBox_EscapeSequences.IsChecked.Value)
                    fsm.SearchOptions |= IDEManager.FileSearchManagement.SearchFlags.EscapeSequences;

                if (checkBox_CaseSensitive.IsChecked.Value)
                    fsm.SearchOptions |= IDEManager.FileSearchManagement.SearchFlags.CaseSensitive;

                if (checkBox_SearchUpward.IsChecked.Value)
                    fsm.SearchOptions |= IDEManager.FileSearchManagement.SearchFlags.Upward;

                if (checkBox_WordOnly.IsChecked.Value)
                    fsm.SearchOptions |= IDEManager.FileSearchManagement.SearchFlags.FullWord;

                if (checkBox_WrapAround.IsChecked.Value)
                    fsm.SearchOptions |= IDEManager.FileSearchManagement.SearchFlags.Wrap;

                fsm.ResetSearch();

                optionsChanged = false;
            }
		}

		bool PreCheck()
		{
			if (string.IsNullOrEmpty(comboBox_InputString.Text))
			{
				MessageBox.Show("Search string must not be empty!");
				return false;
			}

			return true;
		}

		public void DoFindNext()
		{
			if (!PreCheck())
				return;

			ApplySearchOptions();

            IDEManager.FileSearchManagement.Instance.FindNext();

			SetWindowPositionNextToCurrentCaret();
		}

		public void DoReplaceNext()
		{
			if (!PreCheck())
				return;

			ApplySearchOptions();

			IDEManager.FileSearchManagement.Instance.ReplaceNext();

			SetWindowPositionNextToCurrentCaret();
		}

		public void DoReplaceAll()
		{
			if (!PreCheck())
				return;

			ApplySearchOptions();

			IDEManager.FileSearchManagement.Instance.ReplaceAll();

			SetWindowPositionNextToCurrentCaret();
		}

		public void DoFindAll()
		{
			if (!PreCheck())
				return;

			ApplySearchOptions();

			IDEManager.FileSearchManagement.Instance.FindAll();
		}

		private void FindNext_Click(object sender, RoutedEventArgs e)
		{
			DoFindNext();
			button_FindNext.Focus();
		}

		private void Grid_Loaded(object sender, RoutedEventArgs e)
		{
			comboBox_InputString.Focus();
		}

		private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (IsVisible)
				SetWindowPositionNextToCurrentCaret();
			else
				ApplySearchOptions();
		}

		private void button_Replace_Click(object sender, RoutedEventArgs e)
		{
			DoReplaceNext();
			button_Replace.Focus();
		}

		private void button_ReplaceAll_Click(object sender, RoutedEventArgs e)
		{
			DoReplaceAll();
			button_ReplaceAll.Focus();
		}

		private void button_Swap1_Click(object sender, RoutedEventArgs e)
		{
			var s = comboBox_InputString.Text;
			comboBox_InputString.Text = comboBox_ReplaceString.Text;
			comboBox_ReplaceString.Text = s;
		}

		private void button_FindAll_Click(object sender, RoutedEventArgs e)
		{
			DoFindAll();
			button_FindAll.Focus();
		}

        bool optionsChanged;
        private void OnOptionsChange(object sender, EventArgs e)
        {
            optionsChanged = true;
        }

        private void OnResumeSearch(object sender, EventArgs e)
        {
            IDEManager.FileSearchManagement.Instance.SearchFromCaret();
        }
	}
}
