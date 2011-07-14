﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using D_Parser.Parser;

namespace DCalculator
{
	/// <summary>
	/// Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			text_input.Text = "2+2";
		}

		public void Calc()
		{
			var expr = DParser.ParseExpression(text_input.Text);

			if (expr == null || !expr.IsConstant)
			{
				text_result.Text = "[Not a mathematical expression!]";
				return;
			}

			try
			{
				text_result.Text = expr.DecValue.ToString();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			Calc();
		}

		private void text_input_TextChanged(object sender, TextChangedEventArgs e)
		{
			Calc();
		}
	}
}
