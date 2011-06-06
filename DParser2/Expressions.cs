﻿using System;
using System.Collections.Generic;
using System.Text;
using D_Parser.Core;

namespace D_Parser
{
	public class DExpressionDecl : AbstractTypeDeclaration
	{
		public IExpression Expression;

		public DExpressionDecl() { }

		public DExpressionDecl(IExpression dExpression)
		{
			this.Expression = dExpression;
		}

		public override string ToString(bool IncludeBase)
		{
			return (IncludeBase&& InnerDeclaration != null ? InnerDeclaration.ToString() : "") + Expression.ToString();
		}
	}

	public delegate INode[] ResolveTypeHandler(string identifier);

	public interface IExpression
	{
		CodeLocation Location { get; }
		CodeLocation EndLocation { get; }

		ITypeDeclaration ExpressionTypeRepresentation{get;}
		//bool IsConstant { get; }
		// bool EvaluateExpressionValue(out ParserError[] semanticErrors);
	}

	public class ExpressionHelper
	{
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
	}

	public abstract class OperatorBasedExpression : IExpression
	{
		public virtual IExpression LeftOperand { get; set; }
		public virtual IExpression RightOperand { get; set; }
		public int OperatorToken { get; protected set; }

		public override string ToString()
		{
			return LeftOperand.ToString() + DTokens.GetTokenString(OperatorToken) + (RightOperand != null ? RightOperand.ToString() : "");
		}

		public CodeLocation Location
		{
			get { return LeftOperand.Location; }
		}

		public CodeLocation EndLocation
		{
			get { return RightOperand.EndLocation; }
		}

		/*public virtual bool IsConstant
		{
			get { return LeftOperand.IsConstant && RightOperand.IsConstant; }
		}

		public virtual object EvaluatedConstValue
		{
			get { return null; }
		}*/


		public abstract ITypeDeclaration ExpressionTypeRepresentation
		{
			get;
		}
	}

	public class Expression : IExpression, IEnumerable<IExpression>
	{
		public IList<IExpression> Expressions = new List<IExpression>();

		public void Add(IExpression ex)
		{
			Expressions.Add(ex);
		}

		public IEnumerator<IExpression> GetEnumerator()
		{
			return Expressions.GetEnumerator();
		}

		public override string ToString()
		{
			var s = "";
			foreach (var ex in Expressions)
				s += ex.ToString() + ",";
			return s.TrimEnd(',');
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return Expressions.GetEnumerator();
		}

		public CodeLocation Location
		{
			get { return Expressions[0].Location; }
		}

		public CodeLocation EndLocation
		{
			get { return Expressions[Expressions.Count].EndLocation; }
		}

		/*public bool IsConstant
		{
			get
			{
				foreach (var e in Expressions)
					if (!e.IsConstant)
						return false;
				return true;
			}
		}

		/// <summary>
		/// Will return the const value of the first expression only
		/// </summary>
		public object EvaluatedConstValue
		{
			get { return Expressions[0].EvaluatedConstValue; }
		}

		/// <summary>
		/// Will return all values
		/// </summary>
		public object[] EvaluatedConstValues
		{
			get
			{
				var l = new List<object>(Expressions.Count);
				foreach (var e in Expressions)
					l.Add(e.EvaluatedConstValue);

				return l.ToArray();
			}
		}*/


		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return null; }
		}
	}

	public class AssignExpression : OperatorBasedExpression
	{
		public AssignExpression(int opToken) { OperatorToken = opToken; }
		/*
		public override bool IsConstant
		{
			get
			{
				return false; // An assign expression cannot be constant at all..
			}
		}*/

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return RightOperand.ExpressionTypeRepresentation; }
		}
	}

	public class ConditionalExpression : IExpression
	{
		public IExpression OrOrExpression { get; set; }

		public IExpression TrueCaseExpression { get; set; }
		public IExpression FalseCaseExpression { get; set; }

		public override string ToString()
		{
			return this.OrOrExpression.ToString() + "?" + TrueCaseExpression.ToString() + FalseCaseExpression.ToString();
		}

		public CodeLocation Location
		{
			get { return OrOrExpression.Location; }
		}

		public CodeLocation EndLocation
		{
			get { return FalseCaseExpression.EndLocation; }
		}

		//public bool IsConstant{	get { return OrOrExpression.IsConstant && TrueCaseExpression.IsConstant && FalseCaseExpression.IsConstant; }	}
		/*
		public object EvaluatedConstValue
		{
			get {
				var o = OrOrExpression.EvaluatedConstValue;
				return ExpressionHelper.ToBool(o)?TrueCaseExpression.EvaluatedConstValue : FalseCaseExpression.EvaluatedConstValue;
			}
		}*/


		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return TrueCaseExpression.ExpressionTypeRepresentation; }
		}
	}

	public class OrOrExpression : OperatorBasedExpression
	{
		public OrOrExpression() { OperatorToken = DTokens.LogicalOr; }
		/*
		public override object EvaluatedConstValue
		{
			get
			{
				return ExpressionHelper.ToBool(LeftOperand.EvaluatedConstValue) || ExpressionHelper.ToBool(RightOperand.EvaluatedConstValue);
			}
		}*/

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DTokenDeclaration(DTokens.Bool); }
		}
	}

	public class AndAndExpression : OperatorBasedExpression
	{
		public AndAndExpression() { OperatorToken = DTokens.LogicalAnd; }
		/*
		public override object EvaluatedConstValue
		{
			get
			{
				return ExpressionHelper.ToBool(LeftOperand.EvaluatedConstValue) && ExpressionHelper.ToBool(RightOperand.EvaluatedConstValue);
			}
		}*/

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DTokenDeclaration(DTokens.Bool); }
		}
	}

	public class XorExpression : OperatorBasedExpression
	{
		public XorExpression() { OperatorToken = DTokens.Xor; }
		/*
		public override object EvaluatedConstValue
		{
			get
			{
				return ExpressionHelper.ToBool(LeftOperand.EvaluatedConstValue) ^ ExpressionHelper.ToBool(RightOperand.EvaluatedConstValue);
			}
		}*/

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return LeftOperand.ExpressionTypeRepresentation; }
		}
	}

	public class OrExpression : OperatorBasedExpression
	{
		public OrExpression() { OperatorToken = DTokens.BitwiseOr; }
		/*
		public override object EvaluatedConstValue
		{
			get
			{
				return ExpressionHelper.ToLong(LeftOperand.EvaluatedConstValue) | ExpressionHelper.ToLong(RightOperand.EvaluatedConstValue);
			}
		}*/

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return LeftOperand.ExpressionTypeRepresentation; }
		}
	}

	public class AndExpression : OperatorBasedExpression
	{
		public AndExpression() { OperatorToken = DTokens.BitwiseAnd; }

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return LeftOperand.ExpressionTypeRepresentation; }
		}
	}

	public class EqualExpression : OperatorBasedExpression
	{
		public EqualExpression(bool isUnEqual) { OperatorToken = isUnEqual ? DTokens.NotEqual : DTokens.Equal; }

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DTokenDeclaration(DTokens.Bool); }
		}
	}

	public class IdendityExpression : OperatorBasedExpression
	{
		public bool Not;

		public IdendityExpression(bool notIs) { Not = notIs; OperatorToken = DTokens.Is; }

		public override string ToString()
		{
			return LeftOperand.ToString() + (Not ? " !" : " ") + "is " + RightOperand.ToString();
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DTokenDeclaration(DTokens.Bool); }
		}
	}

	public class RelExpression : OperatorBasedExpression
	{
		public RelExpression(int relationalOperator) { OperatorToken = relationalOperator; }

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DTokenDeclaration(DTokens.Bool); }
		}
	}

	public class InExpression : OperatorBasedExpression
	{
		public bool Not;

		public InExpression(bool notIn) { Not = notIn; OperatorToken = DTokens.In; }

		public override string ToString()
		{
			return LeftOperand.ToString() + (Not ? " !" : " ") + "in " + RightOperand.ToString();
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DTokenDeclaration(DTokens.Bool); }
		}
	}

	public class ShiftExpression : OperatorBasedExpression
	{
		public ShiftExpression(int shiftOperator) { OperatorToken = shiftOperator; }

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return LeftOperand.ExpressionTypeRepresentation; }
		}
	}

	public class AddExpression : OperatorBasedExpression
	{
		public AddExpression(bool isMinus) { OperatorToken = isMinus ? DTokens.Minus : DTokens.Plus; }

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return LeftOperand.ExpressionTypeRepresentation; }
		}
	}

	public class MulExpression : OperatorBasedExpression
	{
		public MulExpression(int mulOperator) { OperatorToken = mulOperator; }

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return LeftOperand.ExpressionTypeRepresentation; }
		}
	}

	public class CatExpression : OperatorBasedExpression
	{
		public CatExpression() { OperatorToken = DTokens.Tilde; }

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get {
				var lot = LeftOperand.ExpressionTypeRepresentation;

				if (lot is ArrayDecl)
					return lot;
				else
					return new ArrayDecl() { InnerDeclaration=lot};
			}
		}
	}

	public interface UnaryExpression : IExpression { }

	public class PowExpression : OperatorBasedExpression, UnaryExpression
	{
		public PowExpression() { OperatorToken = DTokens.Pow; }

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return LeftOperand.ExpressionTypeRepresentation; }
		}
	}

	public abstract class SimpleUnaryExpression : UnaryExpression
	{
		public abstract int ForeToken { get; }
		public IExpression UnaryExpression { get; set; }

		public override string ToString()
		{
			return DTokens.GetTokenString(ForeToken) + UnaryExpression.ToString();
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get { return UnaryExpression.EndLocation; }
		}


		public abstract ITypeDeclaration ExpressionTypeRepresentation
		{
			get;
		}
	}

	public class UnaryExpression_And : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.BitwiseAnd; }
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DExpressionDecl(this) { InnerDeclaration = UnaryExpression.ExpressionTypeRepresentation }; }
		}
	}

	public class UnaryExpression_Increment : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Increment; }
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return UnaryExpression.ExpressionTypeRepresentation; }
		}
	}

	public class UnaryExpression_Decrement : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Decrement; }
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return UnaryExpression.ExpressionTypeRepresentation; }
		}
	}

	public class UnaryExpression_Mul : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Times; }
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DExpressionDecl(this) {InnerDeclaration = UnaryExpression.ExpressionTypeRepresentation }; }
		}
	}

	public class UnaryExpression_Add : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Plus; }
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return UnaryExpression.ExpressionTypeRepresentation; }
		}
	}

	public class UnaryExpression_Sub : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Minus; }
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return UnaryExpression.ExpressionTypeRepresentation; }
		}
	}

	public class UnaryExpression_Not : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Not; }
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return UnaryExpression.ExpressionTypeRepresentation; }
		}
	}

	/// <summary>
	/// Bitwise negation operation:
	/// 
	/// int a=56;
	/// int b=~a;
	/// 
	/// b will be -57;
	/// </summary>
	public class UnaryExpression_Cat : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Tilde; }
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return UnaryExpression.ExpressionTypeRepresentation; }
		}
	}

	/// <summary>
	/// (Type).Identifier
	/// </summary>
	public class UnaryExpression_Type : UnaryExpression
	{
		public ITypeDeclaration Type { get; set; }
		public string AccessIdentifier { get; set; }

		public override string ToString()
		{
			return "(" + Type.ToString() + ")." + AccessIdentifier;
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}

		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DExpressionDecl(this); }
		}
	}


	/// <summary>
	/// NewExpression:
	///		NewArguments Type [ AssignExpression ]
	///		NewArguments Type ( ArgumentList )
	///		NewArguments Type
	/// </summary>
	public class NewExpression : UnaryExpression
	{
		public ITypeDeclaration Type { get; set; }
		public IExpression[] NewArguments { get; set; }
		public IExpression[] Arguments { get; set; }

		/// <summary>
		/// true if new myType[10]; instead of new myType(1,"asdf"); has been used
		/// </summary>
		public bool IsArrayArgument { get; set; }

		public override string ToString()
		{
			var ret = "new";

			if (NewArguments != null)
			{
				ret += "(";
				foreach (var e in NewArguments)
					ret += e.ToString() + ",";
				ret = ret.TrimEnd(',') + ")";
			}

			ret += " " + Type.ToString();

			ret += IsArrayArgument ? '[' : '(';
			foreach (var e in Arguments)
				ret += e.ToString() + ",";

			ret = ret.TrimEnd(',') + (IsArrayArgument ? ']' : ')');

			return ret;
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}

		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return Type; }
		}
	}

	/// <summary>
	/// NewArguments ClassArguments BaseClasslist { DeclDefs } 
	/// new ParenArgumentList_opt class ParenArgumentList_opt SuperClass_opt InterfaceClasses_opt ClassBody
	/// </summary>
	public class AnonymousClassExpression : UnaryExpression
	{
		public IExpression[] NewArguments { get; set; }
		public DClassLike AnonymousClass { get; set; }

		public IExpression[] ClassArguments { get; set; }

		public override string ToString()
		{
			var ret = "new";

			if (NewArguments != null)
			{
				ret += "(";
				foreach (var e in NewArguments)
					ret += e.ToString() + ",";
				ret = ret.TrimEnd(',') + ")";
			}

			ret += " class";

			if (ClassArguments != null)
			{
				ret += '(';
				foreach (var e in ClassArguments)
					ret += e.ToString() + ",";

				ret = ret.TrimEnd(',') + ")";
			}

			if (AnonymousClass != null && AnonymousClass.BaseClasses != null)
			{
				ret += ":";

				foreach (var t in AnonymousClass.BaseClasses)
					ret += t.ToString() + ",";

				ret = ret.TrimEnd(',');
			}

			ret += " {...}";

			return ret;
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}

		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DExpressionDecl(this); }
		}
	}

	public class DeleteExpression : SimpleUnaryExpression
	{
		public override int ForeToken
		{
			get { return DTokens.Delete; }
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return null; }
		}
	}

	/// <summary>
	/// CastExpression:
	///		cast ( Type ) UnaryExpression
	///		cast ( CastParam ) UnaryExpression
	/// </summary>
	public class CastExpression : UnaryExpression
	{
		public bool IsTypeCast
		{
			get { return Type != null; }
		}
		public IExpression UnaryExpression;

		public ITypeDeclaration Type { get; set; }
		public int[] CastParamTokens { get; set; } //TODO: Still unused

		public override string ToString()
		{
			var ret = "cast(";

			if (IsTypeCast)
				ret += Type.ToString();
			else
			{
				foreach (var tk in CastParamTokens)
					ret += DTokens.GetTokenString(tk) + " ";
				ret = ret.TrimEnd(' ');
			}

			ret += ") " + UnaryExpression.ToString();

			return ret;
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}

		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get 
			{ 
				if(IsTypeCast)
					return Type;

				if (UnaryExpression != null)
					return UnaryExpression.ExpressionTypeRepresentation;

				return null;
			}
		}
	}

	public abstract class PostfixExpression : IExpression
	{
		public IExpression PostfixForeExpression { get; set; }

		public CodeLocation Location
		{
			get { return PostfixForeExpression.Location; }
		}

		public abstract CodeLocation EndLocation { get; set; }


		public abstract ITypeDeclaration ExpressionTypeRepresentation
		{
			get;
		}
	}

	/// <summary>
	/// PostfixExpression . Identifier
	/// PostfixExpression . TemplateInstance
	/// PostfixExpression . NewExpression
	/// </summary>
	public class PostfixExpression_Access : PostfixExpression
	{
		public IExpression NewExpression;
		public ITypeDeclaration TemplateOrIdentifier;

		public override string ToString()
		{
			return PostfixForeExpression.ToString() + "." + (TemplateOrIdentifier != null ? TemplateOrIdentifier.ToString() : NewExpression.ToString());
		}

		public override CodeLocation EndLocation
		{
			get;
			set;
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get {
				var t = TemplateOrIdentifier;

				if (t==null && NewExpression!=null)
					return NewExpression.ExpressionTypeRepresentation;

				if (t == null)
					return null;

				t.InnerDeclaration = PostfixForeExpression.ExpressionTypeRepresentation;
				return t;
			}
		}
	}

	public class PostfixExpression_Increment : PostfixExpression
	{
		public override string ToString()
		{
			return PostfixForeExpression.ToString() + "++";
		}

		public override CodeLocation EndLocation
		{
			get;
			set;
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return PostfixForeExpression.ExpressionTypeRepresentation; }
		}
	}

	public class PostfixExpression_Decrement : PostfixExpression
	{
		public override string ToString()
		{
			return PostfixForeExpression.ToString() + "--";
		}

		public override CodeLocation EndLocation
		{
			get;
			set;
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return PostfixForeExpression.ExpressionTypeRepresentation; }
		}
	}

	/// <summary>
	/// PostfixExpression ( )
	/// PostfixExpression ( ArgumentList )
	/// </summary>
	public class PostfixExpression_MethodCall : PostfixExpression
	{
		public IExpression[] Arguments;

		public override string ToString()
		{
			var ret = PostfixForeExpression.ToString() + "(";

			if (Arguments != null)
				foreach (var a in Arguments)
					ret += a.ToString() + ",";

			return ret.TrimEnd(',') + ")";
		}

		public override CodeLocation EndLocation
		{
			get;
			set;
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return PostfixForeExpression.ExpressionTypeRepresentation; }
		}
	}

	/// <summary>
	/// IndexExpression:
	///		PostfixExpression [ ArgumentList ]
	/// </summary>
	public class PostfixExpression_Index : PostfixExpression
	{
		public IExpression[] Arguments;

		public override string ToString()
		{
			var ret = (PostfixForeExpression != null ? PostfixForeExpression.ToString() : "") + "[";

			if (Arguments != null)
				foreach (var a in Arguments)
					ret += a.ToString() + ",";

			return ret.TrimEnd(',') + "]";
		}

		public override CodeLocation EndLocation
		{
			get;
			set;
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DExpressionDecl(this) { InnerDeclaration=PostfixForeExpression.ExpressionTypeRepresentation}; }
		}
	}


	/// <summary>
	/// SliceExpression:
	///		PostfixExpression [ ]
	///		PostfixExpression [ AssignExpression .. AssignExpression ]
	/// </summary>
	public class PostfixExpression_Slice : PostfixExpression
	{
		public IExpression FromExpression;
		public IExpression ToExpression;

		public override string ToString()
		{
			var ret = PostfixForeExpression.ToString() + "[";

			if (FromExpression != null)
				ret += FromExpression.ToString();

			if (FromExpression != null && ToExpression != null)
				ret += "..";

			if (ToExpression != null)
				ret += ToExpression.ToString();

			return ret + "]";
		}

		public override CodeLocation EndLocation
		{
			get;
			set;
		}

		public override ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DExpressionDecl(this) { InnerDeclaration = PostfixForeExpression.ExpressionTypeRepresentation }; }
		}
	}

	public interface PrimaryExpression : IExpression { }



	/// <summary>
	/// Identifier as well as literal primary expression
	/// </summary>
	public class IdentifierExpression : PrimaryExpression
	{
		public bool IsIdentifier { get { return Value is string && LiteralFormat==LiteralFormat.None; } }

		public object Value = "";
		public LiteralFormat LiteralFormat=LiteralFormat.None;

		public IdentifierExpression() { }
		public IdentifierExpression(object Val) { Value = Val; }

		public override string ToString()
		{
			return
				(IsIdentifier ?
					Value as string :
					((Value == null) ?
						string.Empty :
						Value.ToString()));
		}


		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}


		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DExpressionDecl(this); }
		}
	}

	public class TokenExpression : PrimaryExpression
	{
		public int Token;

		public TokenExpression() { }
		public TokenExpression(int T) { Token = T; }

		public override string ToString()
		{
			return DParser.GetTokenString(Token);
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}


		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DTokenDeclaration(Token); }
		}
	}

	/// <summary>
	/// TemplateInstance
	/// BasicType . Identifier
	/// </summary>
	public class TypeDeclarationExpression : PrimaryExpression
	{
		public bool IsTemplateDeclaration
		{
			get { return Declaration is TemplateDecl; }
		}

		public ITypeDeclaration Declaration;

		public TypeDeclarationExpression() { }
		public TypeDeclarationExpression(ITypeDeclaration td) { Declaration = td; }

		public override string ToString()
		{
			return Declaration != null ? Declaration.ToString() : "";
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}


		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return Declaration; }
		}
	}

	/// <summary>
	/// auto arr= [1,2,3,4,5,6];
	/// </summary>
	public class ArrayLiteralExpression : PrimaryExpression
	{
		public ArrayLiteralExpression()
		{
			Expressions = new List<IExpression>();
		}

		public virtual IEnumerable<IExpression> Expressions { get; set; }

		public override string ToString()
		{
			var s = "[";
			foreach (var expr in Expressions)
				s += expr.ToString() + ", ";
			s = s.TrimEnd(' ', ',') + "]";
			return s;
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}


		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DExpressionDecl(this); }
		}
	}

	public class AssocArrayExpression : PrimaryExpression
	{
		public IDictionary<IExpression, IExpression> KeyValuePairs = new Dictionary<IExpression, IExpression>();

		public override string ToString()
		{
			var s = "[";
			foreach (var expr in KeyValuePairs)
				s += expr.Key.ToString() + ":" + expr.Value.ToString() + ", ";
			s = s.TrimEnd(' ', ',') + "]";
			return s;
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}

		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new ArrayDecl(); }
		}
	}

	public class FunctionLiteral : PrimaryExpression
	{
		public int LiteralToken = DTokens.Delegate;

		public DMethod AnonymousMethod = new DMethod();

		public FunctionLiteral() { }
		public FunctionLiteral(int InitialLiteral) { LiteralToken = InitialLiteral; }

		public override string ToString()
		{
			return DTokens.GetTokenString(LiteralToken) + " " + AnonymousMethod.ToString();
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}

		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DelegateDeclaration() { IsFunction=LiteralToken==DTokens.Function, ReturnType=AnonymousMethod.Type, Parameters=AnonymousMethod.Parameters}; }
		}
	}

	public class AssertExpression : PrimaryExpression
	{
		public IExpression[] AssignExpressions;

		public override string ToString()
		{
			var ret = "assert(";

			foreach (var e in AssignExpressions)
				ret += e.ToString() + ",";

			return ret.TrimEnd(',') + ")";
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}


		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DTokenDeclaration(DTokens.Bool); }
		}
	}

	public class MixinExpression : PrimaryExpression
	{
		public IExpression AssignExpression;

		public override string ToString()
		{
			return "mixin(" + AssignExpression.ToString() + ")";
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}

		//TODO: How to get this resolved?
		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return null; }
		}
	}

	public class ImportExpression : PrimaryExpression
	{
		public IExpression AssignExpression;

		public override string ToString()
		{
			return "import(" + AssignExpression.ToString() + ")";
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}


		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DExpressionDecl(new IdentifierExpression(""){LiteralFormat=LiteralFormat.StringLiteral}); }
		}
	}

	public class TypeidExpression : PrimaryExpression
	{
		public ITypeDeclaration Type;
		public IExpression Expression;

		public override string ToString()
		{
			return "typeid(" + (Type != null ? Type.ToString() : Expression.ToString()) + ")";
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}


		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new IdentifierDeclaration("TypeInfo") { InnerDeclaration=new IdentifierDeclaration("object")}; }
		}
	}

	public class IsExpression : PrimaryExpression
	{
		public ITypeDeclaration Type;
		public string Identifier;

		/// <summary>
		/// True if Type == TypeSpecialization instead of Type : TypeSpecialization
		/// </summary>
		public bool EqualityTest;

		public ITypeDeclaration TypeSpecialization;
		public int TypeSpecializationToken;

		public ITemplateParameter[] TemplateParameterList;

		public override string ToString()
		{
			var ret = "is(" + Type.ToString();

			ret += Identifier + (EqualityTest ? "==" : ":");

			ret += TypeSpecialization != null ? TypeSpecialization.ToString() : DTokens.GetTokenString(TypeSpecializationToken);

			if (TemplateParameterList != null)
			{
				ret += ",";
				foreach (var p in TemplateParameterList)
					ret += p.ToString() + ",";
			}

			return ret.TrimEnd(' ', ',') + ")";
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}


		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DTokenDeclaration(DTokens.Bool); }
		}
	}

	public class TraitsExpression : PrimaryExpression
	{
		public string Keyword;

		public IEnumerable<TraitsArgument> Arguments;

		public override string ToString()
		{
			var ret = "__traits(" + Keyword;

			if (Arguments != null)
				foreach (var a in Arguments)
					ret += "," + a.ToString();

			return ret + ")";
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}

		//TODO: Get all the returned value types in detail
		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return Keyword.StartsWith("is")||Keyword.StartsWith("has")?new DTokenDeclaration(DTokens.Bool):new IdentifierDeclaration("object"); }
		}
	}

	public class TraitsArgument
	{
		public ITypeDeclaration Type;
		public IExpression AssignExpression;

		public override string ToString()
		{
			return Type != null ? Type.ToString() : AssignExpression.ToString();
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}
	}

	/// <summary>
	/// ( Expression )
	/// </summary>
	public class SurroundingParenthesesExpression : PrimaryExpression
	{
		public IExpression Expression;

		public override string ToString()
		{
			return "(" + Expression.ToString() + ")";
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}


		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return Expression.ExpressionTypeRepresentation; }
		}
	}



	#region Template parameters

	public interface ITemplateParameter
	{
		string Name { get; }
	}

	public class TemplateParameterNode : AbstractNode
	{
		public readonly ITemplateParameter TemplateParameter;

		public TemplateParameterNode(ITemplateParameter param)
		{
			TemplateParameter = param;

			Name = param.Name;
		}

		public override string ToString()
		{
			return TemplateParameter.ToString();
		}

		public override string ToString(bool Attributes, bool IncludePath)
		{
			return (GetNodePath(this, false) + "." + ToString()).TrimEnd('.');
		}
	}

	public class TemplateTypeParameter : ITemplateParameter
	{
		public string Name { get; set; }

		public ITypeDeclaration Specialization;
		public ITypeDeclaration Default;

		public override string ToString()
		{
			var ret = Name;

			if (Specialization != null)
				ret += ":" + Specialization.ToString();

			if (Default != null)
				ret += "=" + Default.ToString();

			return ret;
		}
	}

	public class TemplateThisParameter : ITemplateParameter
	{
		public string Name { get { return FollowParameter.Name; } }

		public ITemplateParameter FollowParameter;

		public override string ToString()
		{
			return "this" + (FollowParameter != null ? (" " + FollowParameter.ToString()) : "");
		}
	}

	public class TemplateValueParameter : ITemplateParameter
	{
		public string Name { get; set; }
		public ITypeDeclaration Type;

		public IExpression SpecializationExpression;
		public IExpression DefaultExpression;

		public override string ToString()
		{
			return Type.ToString() + " " + Name/*+ (SpecializationExpression!=null?(":"+SpecializationExpression.ToString()):"")+
				(DefaultExpression!=null?("="+DefaultExpression.ToString()):"")*/;
		}
	}

	public class TemplateAliasParameter : TemplateValueParameter
	{
		public ITypeDeclaration SpecializationType;
		public ITypeDeclaration DefaultType;

		public override string ToString()
		{
			return "alias " + base.ToString();
		}
	}

	public class TemplateTupleParameter : ITemplateParameter
	{
		public string Name { get; set; }

		public override string ToString()
		{
			return Name + " ...";
		}
	}

	#endregion

	#region Initializers

	public interface DInitializer : IExpression { }

	public class VoidInitializer : TokenExpression, DInitializer
	{
		public VoidInitializer() : base(DTokens.Void) { }
	}

	public class ArrayInitializer : ArrayLiteralExpression, DInitializer
	{
		public ArrayMemberInitializer[] ArrayMemberInitializations;

		public override IEnumerable<IExpression> Expressions
		{
			get
			{
				foreach (var ami in ArrayMemberInitializations)
					yield return ami.Left;
			}
			set { }
		}

		public override string ToString()
		{
			var ret = "[";

			if (ArrayMemberInitializations != null)
				foreach (var i in ArrayMemberInitializations)
					ret += i.ToString() + ",";

			return ret.TrimEnd(',') + "]";
		}
	}

	public class ArrayMemberInitializer
	{
		public IExpression Left;
		public IExpression Specialization;

		public override string ToString()
		{
			return Left.ToString() + (Specialization != null ? (":" + Specialization.ToString()) : "");
		}
	}

	public class StructInitializer : DInitializer
	{
		public StructMemberInitializer[] StructMemberInitializers;

		public override string ToString()
		{
			var ret = "{";

			if (StructMemberInitializers != null)
				foreach (var i in StructMemberInitializers)
					ret += i.ToString() + ",";

			return ret.TrimEnd(',') + "}";
		}

		public CodeLocation Location
		{
			get;
			set;
		}

		public CodeLocation EndLocation
		{
			get;
			set;
		}


		public ITypeDeclaration ExpressionTypeRepresentation
		{
			get { return new DExpressionDecl(this); }
		}
	}

	public class StructMemberInitializer
	{
		public string MemberName = string.Empty;
		public IExpression Specialization;

		public override string ToString()
		{
			return (!string.IsNullOrEmpty(MemberName) ? (MemberName + ":") : "") + Specialization.ToString();
		}
	}

	#endregion
}
