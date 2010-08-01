﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace D_Parser
{
    /// <summary>
    /// Parser for D Code
    /// </summary>
    public partial class DParser
    {
        #region Modules
        // http://www.digitalmars.com/d/2.0/module.html

        /// <summary>
        /// Module entry point
        /// </summary>
        void Root()
        {
            Step();

            // Only one module declaration possible possible!
            if (LA(Module))
                ModuleDeclaration();

            // Now only declarations or other statements are allowed!
            while (!IsEOF)
            {
                DeclDef(ref doc);
            }
        }

        void DeclDef(ref DNode par)
        {
            //AttributeSpecifier
            if (IsAttributeSpecifier())
                AttributeSpecifier();

            //ImportDeclaration
            else if (LA(Import))
                ImportDeclaration();

            //EnumDeclaration
            else if (LA(Enum))
                EnumDeclaration();

            //ClassDeclaration
            else if (LA(Class))
                ClassDeclaration();

            //InterfaceDeclaration
            else if (LA(Interface))
                InterfaceDeclaration();

            //AggregateDeclaration
            else if (LA(Struct) || LA(Union))
                AggregateDeclaration();

            //Declaration
            else if (IsDeclaration())
                Declaration(ref par);

            //Constructor
            else if (LA(This))
                Constructor(ref par);

            //Destructor
            else if (LA(Tilde) && LA(This))
                Destructor(ref par);

            //Invariant
            //UnitTest
            //StaticConstructor
            //StaticDestructor
            //SharedStaticConstructor
            //SharedStaticDestructor
            //ConditionalDeclaration
            //StaticAssert
            //TemplateDeclaration
            //TemplateMixin

            //MixinDeclaration
            else if (LA(Mixin))
                MixinDeclaration();

            //;
            else if (LA(Semicolon))
                Step();

            // else:
            else
                SynErr(t.Kind, "Declaration expected");
        }

        void ModuleDeclaration()
        {
            Expect(Module);
            Document.module = ModuleFullyQualifiedName();
            Expect(Semicolon);
        }

        string ModuleFullyQualifiedName()
        {
            Expect(Identifier);
            string ret = t.Value;

            while (la.Kind == Dot)
            {
                Step();
                Expect(Identifier);

                ret += "." + t.Value;
            }
            return ret;
        }

        void ImportDeclaration()
        {
            Expect(Import);

            _Import();

            // ImportBindings
            if (LA(Colon))
            {
                ImportBind();
                while (LA(Comma))
                {
                    Step();
                    ImportBind();
                }
            }
            else
                while (LA(Comma))
                {
                    Step();
                    _Import();
                }

            Expect(Semicolon);
        }

        void _Import()
        {
            string imp = "";

            // ModuleAliasIdentifier
            if (PK(Assign))
            {
                Expect(Identifier);
                string ModuleAliasIdentifier = t.Value;
                Step();
            }

            imp = ModuleFullyQualifiedName();
            import.Add(imp);
        }

        void ImportBind()
        {
            Expect(Identifier);
            string imbBind = t.Value;
            string imbBindDef = null;

            if (LA(Assign))
            {
                Step();
                imbBindDef = t.Value;
            }
        }


        void MixinDeclaration()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Declarations
        // http://www.digitalmars.com/d/2.0/declaration.html

        bool IsDeclaration()
        {
            return LA(Alias) || IsStorageClass || IsBasicType();
        }

        void Declaration(ref DNode par)
        {
            if (LA(Alias))
            {
                Step();
            }
            Decl(ref par);
        }

        void Decl(ref DNode par)
        {
            // Enum possible storage class attributes
            List<int> storAttr = new List<int>();
            while (IsStorageClass)
            {
                Step();
                storAttr.Add(t.Kind);
            }

            // Autodeclaration
            if (storAttr.Count > 0 && PK(Identifier) && Peek().Kind == DTokens.Assign)
            {
                Step();
                DNode n = new DVariable();
                n.Type = new DTokenDeclaration(t.Kind);
                n.TypeToken = t.Kind;
                Expect(Identifier);
                n.name = t.Value;
                Expect(Assign);
                (n as DVariable).Initializer = AssignExpression();
                Expect(Semicolon);
                par.Add(n);
                return;
            }

            // Declarators
            TypeDeclaration ttd = BasicType();
            DNode firstNode = Declarator(false);

            if (firstNode.Type == null)
                firstNode.Type = ttd;
            else
                firstNode.Type.MostBasic.Base = ttd;


            // BasicType Declarators ;
            bool ExpectFunctionBody = !(LA(Assign) || LA(Comma) || LA(Semicolon));
            if (!ExpectFunctionBody)
            {
                // DeclaratorInitializer
                if (LA(Assign))
                    (firstNode as DVariable).Initializer = Initializer();

                par.Add(firstNode);

                // DeclaratorIdentifierList
                while (LA(Comma))
                {
                    Step();
                    Expect(Identifier);

                    DVariable otherNode = new DVariable();
                    otherNode.Assign(firstNode);
                    otherNode.name = t.Value;

                    if (LA(Assign))
                        otherNode.Initializer = Initializer();

                    par.Add(otherNode);
                }

                Expect(Semicolon);
            }

            // BasicType Declarator FunctionBody
            else
            {
                FunctionBody(ref firstNode);
                par.Add(firstNode);
            }
        }

        bool IsBasicType()
        {
            return BasicTypes[la.Kind] || LA(Typeof) || MemberFunctionAttribute[la.Kind] || (LA(Dot) && PK(Identifier)) || LA(Identifier);
        }

        TypeDeclaration BasicType()
        {
            TypeDeclaration td = null;
            if (BasicTypes[la.Kind])
            {
                Step();
                return new DTokenDeclaration(t.Kind);
            }

            if (MemberFunctionAttribute[la.Kind])
            {
                Step();
                MemberFunctionAttributeDecl md = new MemberFunctionAttributeDecl(t.Kind);
                Expect(OpenParenthesis);
                md.Base = Type();
                Expect(CloseParenthesis);
                return md;
            }

            if (LA(Typeof))
            {
                Step();
                Expect(OpenParenthesis);
                MemberFunctionAttributeDecl md = new MemberFunctionAttributeDecl(Typeof);
                td = md;
                if (LA(Return))
                    md.Base = new DTokenDeclaration(Return);
                else
                    md.Base = Expression();
                Expect(CloseParenthesis);

                if (!LA(Dot)) return md;
            }

            if (LA(Dot))
                Step();

            if (td == null)
                td = IdentifierList();
            else
                td.Base = IdentifierList();

            return td;
        }

        bool IsBasicType2()
        {
            return LA(Times) || LA(OpenSquareBracket) || LA(Delegate) || LA(Function);
        }

        TypeDeclaration BasicType2()
        {
            // *
            if (LA(Times))
            {
                Step();
                return new PointerDecl();
            }

            // [ ... ]
            else if (LA(OpenSquareBracket))
            {
                Step();
                // [ ]
                if (LA(CloseSquareBracket)) { Step(); return new ClampDecl(); }

                TypeDeclaration ret = null;

                // [ Type ]
                if (IsBasicType())
                    ret = Type();
                else
                {
                    ret = new DExpressionDecl(AssignExpression());

                    // [ AssignExpression .. AssignExpression ]
                    if (LA(Dot))
                    {
                        Step();
                        Expect(Dot);

                        //TODO: do something with the 2nd expression here
                        AssignExpression();
                    }
                }

                Expect(CloseSquareBracket);
                return ret;
            }

            // delegate | function
            else if (LA(Delegate) || LA(Function))
            {
                Step();
                TypeDeclaration td = null;
                DelegateDeclaration dd = new DelegateDeclaration();
                dd.IsFunction = t.Kind == Function;

                dd.Parameters = Parameters();
                td = dd;
                //TODO: add attributes to declaration
                while (FunctionAttribute[la.Kind])
                {
                    Step();
                    td = new DTokenDeclaration(t.Kind, td);
                }
                return td;
            }
            else
                SynErr(Identifier);
            return null;
        }

        /// <summary>
        /// Parses a type declarator
        /// </summary>
        /// <returns>A dummy node that contains the return type, the variable name and possible parameters of a function declaration</returns>
        DNode Declarator(bool IsParam)
        {
            DNode ret = new DVariable();
            TypeDeclaration ttd = null;

            while (IsBasicType2())
            {
                if (ret.Type == null) ret.Type = BasicType2();
                else { ttd = BasicType2(); ttd.Base = ret.Type; ret.Type =ttd; }
            }
            /*
             * Add some syntax possibilities here
             * like in 
             * int (x);
             * or in
             * int(*foo);
             */
            if (LA(OpenParenthesis))
            {
                Step();
                ClampDecl cd= new ClampDecl(ret.Type,ClampDecl.ClampType.Round);
                ret.Type = cd;

                /* 
                 * Parse all basictype2's that are following the initial '('
                 */
                while (IsBasicType2())
                {
                    ttd = BasicType2();

                    if (cd.KeyType == null) cd.KeyType = ttd;
                    else
                    {
                        ttd.Base = cd.KeyType;
                        cd.KeyType = ttd;
                    }
                }

                /*
                 * Here can be an identifier with some optional DeclaratorSuffixes
                 */
                if (!LA(CloseParenthesis))
                {
                    if (IsParam && !LA(Identifier))
                    {
                        /* If this Declarator is a parameter of a function, don't expect anything here
                         * exept a '*' that means that here's an anonymous function pointer
                         */
                        if (!T(Times))
                            SynErr(Times);
                    }
                    else
                    {
                        Expect(Identifier);
                        ret.name = t.Value;

                        /*
                         * Just here suffixes can follow!
                         */
                        if (!LA(CloseParenthesis))
                        {
                            List<DNode> _unused = null;
                            ttd = DeclaratorSuffixes(out _unused, out _unused);

                            if (cd.KeyType == null) cd.KeyType = ttd;
                            else
                            {
                                ttd.Base = cd.KeyType;
                                cd.KeyType = ttd;
                            }
                        }
                    }
                }
                ret.Type = cd;
                Expect(CloseParenthesis);
            }
            else
            {
                if (IsParam && !LA(Identifier))
                    return ret;

                Expect(Identifier);
                ret.name = t.Value;
            }

            if (IsDeclaratorSuffix)
            {
                // DeclaratorSuffixes
                List<DNode> _Parameters;
                ttd = DeclaratorSuffixes(out ret.TemplateParameters, out _Parameters);
                if (ttd != null)
                {
                    ttd.Base = ret.Type;
                    ret.Type = ttd;
                }

                if (_Parameters != null)
                {
                    DMethod dm = new DMethod();
                    dm.Assign(ret);
                    dm.Parameters = _Parameters;
                    ret = dm;
                }
            }

            return ret;
        }

        bool IsDeclaratorSuffix
        {
            get { return LA(OpenSquareBracket) || LA(OpenParenthesis); }
        }

        /// <summary>
        /// Note:
        /// http://www.digitalmars.com/d/2.0/declaration.html#DeclaratorSuffix
        /// The definition of a sequence of declarator suffixes is buggy here! Theoretically template parameters can be declared without a surrounding ( and )!
        /// Also, more than one parameter sequences are possible!
        /// 
        /// TemplateParameterList[opt] Parameters MemberFunctionAttributes[opt]
        /// </summary>
        TypeDeclaration DeclaratorSuffixes(out List<DNode> TemplateParameters, out List<DNode> _Parameters)
        {
            TypeDeclaration td = null;
            TemplateParameters = new List<DNode>();
            _Parameters = null;

            while (LA(OpenSquareBracket))
            {
                Step();
                ClampDecl ad = new ClampDecl(td);
                if (!LA(CloseSquareBracket))
                {
                    if (IsAssignExpression())
                        ad.KeyType = new DExpressionDecl(AssignExpression());
                    else
                        ad.KeyType = Type();
                }
                Expect(CloseSquareBracket);
                ad.ValueType = td;
                td = ad;
            }

            if (LA(OpenParenthesis))
            {
                if (IsTemplateParameterList())
                {
                    Step();
                    TemplateParameters = TemplateParameterList();
                    Expect(CloseParenthesis);
                }
                _Parameters = Parameters();

                //TODO: MemberFunctionAttributes -- add them to the declaration
                while (MemberFunctionAttribute[la.Kind])
                {
                    Step();
                }
            }
            return td;
        }

        TypeDeclaration IdentifierList()
        {
            TypeDeclaration td = null;

            if (!LA(Identifier))
                SynErr(Identifier);

            // Template instancing
            if (PK(Not))
                td = TemplateInstance();

            // Identifier
            else
            {
                Step();
                td = new NormalDeclaration(t.Value);
            }

            // IdentifierList
            while (LA(Dot))
            {
                Step();
                DotCombinedDeclaration dcd = new DotCombinedDeclaration(td);
                // Template instancing
                if (PK(Not))
                    dcd.AccessedMember = TemplateInstance();
                // Identifier
                else
                    dcd.AccessedMember = new NormalDeclaration(t.Value);
                td = dcd;
            }

            return td;
        }

        bool IsStorageClass
        {
            get
            {
                return LA(Abstract) ||
            LA(Auto) ||
            LA(Const) ||
            LA(Deprecated) ||
            LA(Extern) ||
            LA(Final) ||
            LA(Immutable) ||
            LA(InOut) ||
            LA(Shared) ||
        LA(Nothrow) ||
            LA(Override) ||
        LA(Pure) ||
            LA(Scope) ||
            LA(Static) ||
            LA(Synchronized);
            }
        }

        TypeDeclaration Type()
        {
            TypeDeclaration td = BasicType();

            if (IsDeclarator2())
            {
                TypeDeclaration ttd = Declarator2();
                ttd.Base = td;
                td = ttd;
            }

            return td;
        }

        bool IsDeclarator2()
        {
            return IsBasicType2() || LA(OpenParenthesis);
        }

        /// <summary>
        /// http://www.digitalmars.com/d/2.0/declaration.html#Declarator2
        /// The next bug: Following the definition strictly, this function would end up in an endless loop of requesting another Declarator2
        /// 
        /// So here I think that a Declarator2 only consists of a couple of BasicType2's and some DeclaratorSuffixes
        /// </summary>
        /// <returns></returns>
        TypeDeclaration Declarator2()
        {
            TypeDeclaration td = null;
            if (LA(OpenParenthesis))
            {
                Step();
                td = Declarator2();
                Expect(CloseParenthesis);

                // DeclaratorSuffixes
                if (LA(OpenSquareBracket))
                {
                    List<DNode> _unused = null, _unused2=null;
                    DeclaratorSuffixes(out _unused,out _unused2);
                }
                return td;
            }

            while (IsBasicType2())
            {
                TypeDeclaration ttd= BasicType2();
                ttd.Base = td;
                td = ttd;
            }

            return null;
        }

        /// <summary>
        /// Parse parameters
        /// </summary>
        List<DNode> Parameters()
        {
            List<DNode> ret = new List<DNode>();
            Expect(OpenParenthesis);

            // Empty parameter list
            if (LA(CloseParenthesis))
            {
                Step();
                return ret;
            }

            if (!IsTripleDot())
                ret.Add(Parameter());

            while (LA(Comma))
            {
                Step();
                if (IsTripleDot())
                    break;
                ret.Add(Parameter());
            }

            /*
             * There can be only one '...' in every parameter list
             */
            if (IsTripleDot())
            {
                // If it had not a comma, add a VarArgDecl to the last parameter
                bool HadComma = T(Comma);

                Step();
                Step();
                Step();

                if (!HadComma && ret.Count > 0)
                {
                    ret[ret.Count - 1].Type = new VarArgDecl(ret[ret.Count - 1].Type);
                }
                else
                {
                    DVariable dv = new DVariable();
                    dv.Type = new VarArgDecl();
                    ret.Add(dv);
                }
            }

            Expect(CloseParenthesis);
            return ret;
        }

        bool IsTripleDot()
        {
            return LA(Dot) && PK(Dot) && Peek().Kind == Dot;
        }

        private DNode Parameter()
        {
            //TODO: Handle this
            if (IsInOut())
            {
                Step();
            }

            TypeDeclaration td =null;
            /*
             * A basictype is possible(!), not required
             */
            if (IsBasicType() && (!LA(Identifier) || (!PK(Identifier) && lexer.CurrentPeekToken.Kind!=OpenParenthesis)))
            {
                td = BasicType();
            }

            DNode ret = Declarator(true);
            if (ret.Type == null) 
                ret.Type = td;
            else
                ret.Type.Base = td;

            // DefaultInitializerExpression
            if (LA(Assign))
            {
                DExpression defInit = null;
                if (LA(Identifier) && (la.Value == "__FILE__" || la.Value == "__LINE__"))
                    defInit = new IdentExpression(la.Value);
                else
                    defInit = AssignExpression();

                if (ret is DVariable)
                    (ret as DVariable).Initializer = defInit;
            }

            return ret;
        }

        bool IsInOut()
        {
            return LA(In) || LA(Out) || LA(Ref) || LA(Lazy);
        }


        private DExpression Initializer()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Attributes
        bool IsAttributeSpecifier()
        {
            return (LA(Extern) || LA(Align) || LA(Pragma) || LA(Deprecated) || IsProtectionAttribute()
                || LA(Static) || LA(Final) || LA(Override) || LA(Abstract) || LA(Const) || LA(Auto) || LA(Scope) || LA(__gshared) || LA(Shared) || LA(Immutable) || LA(InOut)
                || LA(DisabledAttribute));
        }

        bool IsProtectionAttribute()
        {
            return LA(Public) || LA(Private) || LA(Protected) || LA(Extern) || LA(Package);
        }

        private void AttributeSpecifier()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Expressions
        TypeDeclaration Expression()
        {
            throw new NotImplementedException();
        }

        bool IsAssignExpression()
        {
            return false;
        }

        DExpression AssignExpression()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Statements

        #endregion

        #region Structs & Unions
        private void AggregateDeclaration()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Classes
        private void ClassDeclaration()
        {
            throw new NotImplementedException();
        }

        void Constructor(ref DNode par)
        {

        }

        void Destructor(ref DNode par)
        {

        }
        #endregion

        #region Interfaces
        private void InterfaceDeclaration()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Enums
        private void EnumDeclaration()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Functions
        void FunctionBody(ref DNode par)
        {
            Expect(OpenCurlyBrace);

            Expect(CloseCurlyBrace);
        }
        #endregion

        #region Templates

        /// <summary>
        /// Be a bit lazy here with checking whether there're templates or not
        /// </summary>
        private bool IsTemplateParameterList()
        {
            lexer.StartPeek();
            int r = 0;
            while (r >= 0 && lexer.CurrentPeekToken.Kind != EOF)
            {
                if (lexer.CurrentPeekToken.Kind == OpenParenthesis) r++;
                else if (lexer.CurrentPeekToken.Kind == CloseParenthesis)
                {
                    r--;
                    if (r < 0 && Peek().Kind == OpenParenthesis)
                        return true;
                }
                Peek();
            }
            return false;
        }

        private List<DNode> TemplateParameterList()
        {
            throw new NotImplementedException();
        }

        private TypeDeclaration TemplateInstance()
        {
            throw new NotImplementedException();
        }
        #endregion
    }

}