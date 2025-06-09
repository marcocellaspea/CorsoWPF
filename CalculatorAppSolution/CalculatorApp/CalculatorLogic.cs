using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq; // Necessario per .Contains() su stringhe

namespace CalculatorApp
{
  public static class CalculatorLogic
  {
    // Valuta un’espressione contenente + - * / ^ ( ) √, sin, cos, tan, asin, acos, atan, pi, e (nepero)
    // conversioni angolari e costanti.
    public static bool Evaluate(string expr, out double result)
    {
      try
      {
        var tokens = Tokenize(expr);
        var rpn = ToRpn(tokens);

        //calcola e valuta i token 
        result = EvaluateRpn(rpn);
        return true;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Errore: {ex.Message}"); 
        result = double.NaN;
        return false;
      }
    }

    // Tokenizzazione
    private static List<string> Tokenize(string expr)
    {
      var tokens = new List<string>();
      int i = 0;

      // Operatori binari e parentesi
      string operators = "+-*/^()√";
      // Funzioni e conversioni angolari (devono essere token singole)
      string[] functions = { "sin", "cos", "tan", "asin", "acos", "atan", "rad", "deg" };
      // Costanti
      string[] constants = { "pi", "e" };

      while (i < expr.Length)
      {
        char c = expr[i];

        // Numeri (inclusi decimali)
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

        // Operatori e parentesi
        if (operators.Contains(c))
        {
          tokens.Add(c.ToString());
          i++;
          continue;
        }

        // Funzioni o costanti (ricerca di token multi-carattere)
        if (char.IsLetter(c))
        {
          string word = "";
          while (i < expr.Length && char.IsLetter(expr[i]))
          {
            word += expr[i];
            i++;
          }

          if (functions.Contains(word.ToLower())) // Converti in minuscolo per confronto case-insensitive
          {
            tokens.Add(word.ToLower());
          }
          else if (constants.Contains(word.ToLower()))
          {
            tokens.Add(word.ToLower());
          }
          else
          {
            throw new ArgumentException($"Token sconosciuto: {word}");
          }
          continue;
        }

        // Ignora spazi
        if (char.IsWhiteSpace(c))
        {
          i++;
          continue;
        }

        // Se arriviamo qui, c'è un carattere non riconosciuto
        throw new ArgumentException($"Carattere non valido nell'espressione: '{c}'");
      }

      return tokens;
    }

    // Shunting-yard → Reverse Polish Notation
    private static Queue<string> ToRpn(List<string> tokens)
    {
      var output = new Queue<string>();
      var ops = new Stack<string>();

      // Le funzioni hanno la precedenza più alta, poi la radice, l'elevamento a potenza, ecc.
      int Prec(string op) => op switch
      {
        "sin" or "cos" or "tan" or "asin" or "acos" or "atan" or "rad" or "deg" => 5, // Funzioni
        "√" => 4,
        "^" => 3,
        "*" or "/" => 2,
        "+" or "-" => 1,
        _ => 0
      };

      bool RightAssoc(string op) => op is "^" or "√";

      // Funzioni unarie che precedono l'argomento (es. sin(90))
      string[] unaryFunctions = { "sin", "cos", "tan", "asin", "acos", "atan", "sqrt" }; // sqrt è √

      foreach (var token in tokens)
      {
        // Se è un numero o una costante
        if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out _) || token == "pi" || token == "e")
        {
          output.Enqueue(token);
          continue;
        }

        // Se è una funzione
        if (unaryFunctions.Contains(token) || token == "rad" || token == "deg")
        {
          ops.Push(token);
          continue;
        }

        // Se è un operatore
        if ("+-*/^√".Contains(token))
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

        // Se è una parentesi aperta
        if (token == "(")
        {
          ops.Push(token);
          continue;
        }

        // Se è una parentesi chiusa
        if (token == ")")
        {
          while (ops.Count > 0 && ops.Peek() != "(")
            output.Enqueue(ops.Pop());
          if (ops.Count == 0) throw new Exception("Parentesi non bilanciata.");
          ops.Pop(); // Pop '('

          // Se c'è una funzione prima della parentesi, la aggiunge all'output
          if (ops.Count > 0 && unaryFunctions.Contains(ops.Peek()) || ops.Count > 0 && (ops.Peek() == "rad" || ops.Peek() == "deg"))
          {
            output.Enqueue(ops.Pop());
          }
        }
      }

      while (ops.Count > 0)
      {
        if (ops.Peek() is "(" or ")") throw new Exception("Parentesi non bilanciata.");
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

        // Numeri
        if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
        {
          stack.Push(num);
          continue;
        }

        // Costanti
        if (token == "pi")
        {
          stack.Push(Math.PI);
          continue;
        }
        if (token == "e")
        {
          stack.Push(Math.E);
          continue;
        }

        // Funzioni unarie (prendono un solo operando)
        if (token == "√") // Radice quadrata
        {
          if (stack.Count < 1) 
            throw new InvalidOperationException("Errore di sintassi per √.");
          double val = stack.Pop();
          stack.Push(Math.Sqrt(val));
          continue;
        }
        if (token == "sin") // Seno
        {
          if (stack.Count < 1)
            throw new InvalidOperationException("Errore di sintassi per sin.");
          double val = stack.Pop();
          stack.Push(Math.Sin(val));
          continue;
        }
        if (token == "cos") // Coseno
        {
          if (stack.Count < 1) 
            throw new InvalidOperationException("Errore di sintassi per cos.");
          double val = stack.Pop();
          stack.Push(Math.Cos(val));
          continue;
        }
        if (token == "tan") // Tangente
        {
          if (stack.Count < 1) 
            throw new InvalidOperationException("Errore di sintassi per tan.");
          double val = stack.Pop();
          stack.Push(Math.Tan(val));
          continue;
        }
        if (token == "asin") // Arcoseno (risultato in radianti)
        {
          if (stack.Count < 1) 
            throw new InvalidOperationException("Errore di sintassi per asin.");
          double val = stack.Pop();
          stack.Push(Math.Asin(val));
          continue;
        }
        if (token == "acos") // Arcocoseno (risultato in radianti)
        {
          if (stack.Count < 1) 
            throw new InvalidOperationException("Errore di sintassi per acos.");
          double val = stack.Pop();
          stack.Push(Math.Acos(val));
          continue;
        }
        if (token == "atan") // Arcocotangente (risultato in radianti)
        {
          if (stack.Count < 1) throw new InvalidOperationException("Errore di sintassi per atan.");
          double val = stack.Pop();
          stack.Push(Math.Atan(val));
          continue;
        }
        if (token == "deg") // Conversione da radianti a gradi
        {
          if (stack.Count < 1) 
            throw new InvalidOperationException("Errore di sintassi per deg.");
          double val = stack.Pop();
          stack.Push(val * (180.0 / Math.PI));
          continue;
        }
        if (token == "rad") // Conversione da gradi a radianti
        {
          if (stack.Count < 1) 
            throw new InvalidOperationException("Errore di sintassi per rad.");
          double val = stack.Pop();
          stack.Push(val * (Math.PI / 180.0));
          continue;
        }

        // Operatori binari (prendono due operandi)
        if (stack.Count < 2) 
          throw new InvalidOperationException("Errore di sintassi per operatori binari.");

        double b = stack.Pop();
        double a = stack.Pop();

        //gestisce operatori principali
        stack.Push(token switch
        {
          "+" => a + b,
          "-" => a - b,
          "*" => a * b,
          "/" => b != 0 ? a / b : throw new DivideByZeroException(),
          "^" => Math.Pow(a, b), 
          _ => throw new InvalidOperationException($"Operatore sconosciuto: {token}")
        });
      }

      if (stack.Count != 1) throw new Exception("Espressione non valida o sintassi errata.");
      return stack.Pop();
    }
  }
}