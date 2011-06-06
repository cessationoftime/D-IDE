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
			string input="a!('a').b.c.d";

			ParseTests.TestTypeEval(input);
			Console.ReadKey();
			//ParseTests.TestExpression(code);
			//ParseTests.TestExpressionStartFinder(code);
			return;
			while (true)
			{
				input = Console.ReadLine();

				if (input == "q")
					return;

				var code = input;

				ParseTests.TestTypeEval(code);
			}
        }
    }
}
