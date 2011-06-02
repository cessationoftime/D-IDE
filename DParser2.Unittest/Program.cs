﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using HighPrecisionTimer;
using System.Threading;
using D_Parser;
using D_Parser.Core;
using D_Parser.Resolver;

namespace ParserTests
{
    class Program
    {
        public static string curFile = "";
        public static void Main(string[] args)
        {
			var code = @"= (myType.aliasedType*).";

			ParseTests.TestExpressionStartFinder(code);

            Console.Read();
            return;
        }
    }
}
