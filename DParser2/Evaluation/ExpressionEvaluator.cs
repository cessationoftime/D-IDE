﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_Parser.Dom.Expressions;
using D_Parser.Resolver;

namespace D_Parser.Evaluation
{
	public class ExpressionEvaluator
	{
		ResolverContextStack ctxt;

		private ExpressionEvaluator() { }

		/// <summary>
		/// Evaluates a constant expression.
		/// Returns null if expression is non-const.
		/// The returned object can either be boolean, long, double, string or char.
		/// 
		/// Used for resolving mixins, constraint checks and for resolving expressions debug-time(?)
		/// </summary>
		public static object Evaluate(IExpression expression, ResolverContextStack ctxt)
		{
			return new ExpressionEvaluator { ctxt = ctxt }.Evaluate(expression);
		}

		public object Evaluate(IExpression x)
		{
			return null;
		}

		public object Evaluate(OperatorBasedExpression x)
		{
			return null;
		}

		public object Evaluate(RelExpression x)
		{
			return null;
		}

		public object Evaluate(UnaryExpression x)
		{
			return null;
		}

		public object Evaluate(PrimaryExpression x)
		{
			return null;
		}

		#region Helpers
		public static bool ToBool(object value)
		{
			bool b = false;

			try
			{
				b = Convert.ToBoolean(value);
			}
			catch { }

			return b;
		}

		public static double ToDouble(object value)
		{
			double d = 0;

			try
			{
				d = Convert.ToDouble(value);
			}
			catch { }

			return d;
		}

		public static long ToLong(object value)
		{
			long d = 0;

			try
			{
				d = Convert.ToInt64(value);
			}
			catch { }

			return d;
		}
		#endregion
	}
}
