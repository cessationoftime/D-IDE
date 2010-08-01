﻿using System;
using System.Collections.Generic;
using System.Text;

namespace D_Parser
{
    public class DVariable : DNode
    {
        public DExpression Initializer; // Variable

        public DVariable()
            :base(FieldType.Variable)
        {

        }

        public override string ToString()
        {
            return base.ToString()+(Initializer!=null?(" = "+Initializer.ToString()):"");
        }
    }
    /*
    public class DDelegate : DMethod
    {
        public new DelegateDeclaration Type;

        public DDelegate()
        {
            fieldtype = FieldType.Delegate;
        }
    }*/

    public class DMethod : DNode
    {
        public List<DNode> Parameters = new List<DNode>();

        public DMethod()
            : base(FieldType.Function)
        {

        }
    }

    public class DClassLike : DNode
    {
        public List<TypeDeclaration> BaseClasses=new List<TypeDeclaration>();

        public DClassLike()
            : base(FieldType.Class)
        {

        }
    }

    public class DEnum : DNode
    {
        public TypeDeclaration EnumBaseType;

        public DEnum()
            : base(FieldType.Enum)
        {

        }
    }

    public class DEnumValue : DVariable
    {
        public DEnumValue()
        {
            fieldtype = FieldType.EnumValue;
        }
    }
}
