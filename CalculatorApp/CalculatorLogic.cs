using System;
using System.Collections.Generic;
using System.Globalization;

namespace CalculatorApp
{
    public static class CalculatorLogic
    {
        // Valuta un’espressione contenente  + - * / ^ ( ) √
        public static bool Evaluate(string expr, out double result)
        {
            try
            {
                var tokens = Tokenize(expr);
                var rpn = ToRpn(tokens);
                result = EvaluateRpn(rpn);
                return true;
            }
            catch
            {
                result = double.NaN;
                return false;
            }
        }

        // Tokenizzazione
        private static List<string> Tokenize(string expr)
        {
            var tokens = new List<string>();
            int i = 0;

            while (i < expr.Length)
            {
                char c = expr[i];

                if (char.IsDigit(c) || c == '.')
                {
                    string number = "";
                    while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.'))
                    {
                        number += expr[i];
                        i++;
                    }
                    tokens.Add(number);
                    continue;
                }

                if ("+-*/^()√".Contains(c))
                {
                    tokens.Add(c.ToString());
                    i++;
                    continue;
                }

                // ignora spazi
                i++;
            }

            return tokens;
        }

        // Shunting-yard → Reverse Polish Notation
        private static Queue<string> ToRpn(List<string> tokens)
        {
            var output = new Queue<string>();
            var ops = new Stack<string>();

            int Prec(string op) => op switch
            {
                "√" => 4,
                "^" => 3,
                "*" or "/" => 2,
                "+" or "-" => 1,
                _ => 0
            };

            bool RightAssoc(string op) => op is "^" or "√";

            foreach (var token in tokens)
            {
                if (double.TryParse(token, out _))
                {
                    output.Enqueue(token);
                    continue;
                }

                if (token == "√")
                {
                    ops.Push(token);
                    continue;
                }

                if ("+-*/^".Contains(token))
                {
                    while (ops.Count > 0 && Prec(ops.Peek()) > 0 &&
                           (Prec(ops.Peek()) > Prec(token) ||
                           (Prec(ops.Peek()) == Prec(token) && !RightAssoc(token))))
                    {
                        output.Enqueue(ops.Pop());
                    }
                    ops.Push(token);
                    continue;
                }

                if (token == "(")
                {
                    ops.Push(token);
                    continue;
                }

                if (token == ")")
                {
                    while (ops.Count > 0 && ops.Peek() != "(")
                        output.Enqueue(ops.Pop());
                    if (ops.Count == 0) throw new Exception("Parentesi errata");
                    ops.Pop(); // elimina '('
                }
            }

            while (ops.Count > 0)
            {
                if (ops.Peek() is "(" or ")") throw new Exception("Parentesi errata");
                output.Enqueue(ops.Pop());
            }

            return output;
        }

        // Valutazione RPN
        private static double EvaluateRpn(Queue<string> rpn)
        {
            var stack = new Stack<double>();

            while (rpn.Count > 0)
            {
                string token = rpn.Dequeue();

                if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                {
                    stack.Push(num);
                    continue;
                }

                if (token == "√")
                {
                    double a = stack.Pop();
                    stack.Push(Math.Sqrt(a));
                    continue;
                }

                // operatori binari
                double b = stack.Pop();
                double a2 = stack.Pop();

                stack.Push(token switch
                {
                    "+" => a2 + b,
                    "-" => a2 - b,
                    "*" => a2 * b,
                    "/" => b != 0 ? a2 / b : throw new DivideByZeroException(),
                    "^" => Math.Pow(a2, b),
                    _ => throw new InvalidOperationException()
                });
            }

            if (stack.Count != 1) throw new Exception("Espressione non valida");
            return stack.Pop();
        }
    }
}