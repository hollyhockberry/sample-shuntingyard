﻿// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;

List<List<(string op, bool leftAssoc, int arguments, Func<IEnumerable<decimal>, decimal> eval)>> Operators = new()
{
    new() {
        ("[~]", false, 1, (a) => ~(int)a.First()),
        ("[+]", false, 1, (a) => a.First()),
        ("[-]", false, 1, (a) => -a.First()),
    },
    new() {
        ("**", false, 2, (a) => (decimal)Math.Pow((double)a.First(), (double)a.Last()))
    },
    new() {
        ("*", true, 2, (a) => a.First() * a.Last()),
        ("/", true, 2, (a) => a.First() / a.Last()),
        ("%", true, 2, (a) => a.First() % a.Last())
    },
    new() {
        ("+", true, 2, (a) => a.First() + a.Last()),
        ("-", true, 2, (a) => a.First() - a.Last())
    },
};

Dictionary<string, (int arguments, Func<IEnumerable<decimal>, decimal> eval)> Functions = new()
{
    { "sqrt", (1, (a) => (decimal)Math.Sqrt((double)a.First())) },
    { "func", (0, (_) => 123) },
    { "D", (3, (a) => a.First() + a.Skip(1).Take(1).First() + a.Last()) }
};

IEnumerable<string> Split(string formula)
{
    var operators = Operators
         .SelectMany(e => e.Select(o => o.op))
         .Select(s => s.Replace("[", "").Replace("]", ""))
         .Distinct()
         .ToList();
    operators.AddRange("(),".Select(c => $"{c}"));
    string? token = null;
    foreach (var c in formula)
    {
        if (c == ' ')
        {
            if (token != null)
            {
                yield return token;
                token = null;
            }
        }
        else
        {
            if (token == null || operators.Any(o => o.StartsWith(token + c)))
            {
                token += c;
            }
            else
            {
                if (operators.Any(o => o.StartsWith(token)) || operators.Any(o => o.StartsWith(c)))
                {
                    yield return token;
                    token = null;
                }
                token += c;
            }
        }
    }
    if (token != null) yield return token;
}

IEnumerable<string> Parse(IEnumerable<string> tokens)
{
    var token_queue = new Queue<string>(tokens);
    var queue = new Queue<string>();

    expr();
    return queue;

    void enqueue(string? token)
    {
        if (token != null)
            queue.Enqueue(token);
    }

    string? pop() => token_queue?.Dequeue();
    string? top() => token_queue?.Count > 0 ? token_queue.First() : null;

    //<expr>    ::= <term> [ ('+'|'-') <term> ]*
    //<term>    ::= <factor> [ ('*'|'/') <factor> ]*
    //<factor>  ::= <factor2> | ('+'|'-'|'~') <factor2>
    //<factor2> ::= <item> | '(' <expr> ')' |
    //<item>    ::= <identifier> ["(" [<arglist>] ")"]
    //<arglist> ::= <expr> { "," <expr> }
    //<identifier> ::= [1-9] [0-9]* [.] [0-9]* | "0x" [0-9A-Fa-f]* | [A-Za-z_] [A-Za-z0-9_]*

    void expr()
    {
        term();
        string[] operators = new string[] { "+", "-" };
        while (operators.Contains(top()))
        {
            enqueue(pop());
            term();
        }
    }

    void term()
    {
        factor();
        string[] operators = new string[] { "**", "*", "/", "%" };
        while (operators.Contains(top()))
        {
            enqueue(pop());
            factor();
        }
    }

    void factor()
    {
        string[] operators = new string[] { "+", "-", "~" };
        if (operators.Contains(top()))
        {
            enqueue($"[{pop()}]");
            factor2();
        }
        else
        {
            factor2();
        }
    }

    void factor2()
    {
        if (top() == "(")
        {
            enqueue(pop());
            expr();
            enqueue(pop());
        }
        else
        {
            item();
        }
    }

    void item()
    {
        identifier();
        if (top() == "(")
        {
            enqueue(pop());
            while (top() != ")")
            {
                arglist();
            }
            enqueue(pop());
        }
    }

    void arglist()
    {
        expr();
        while (top() == ",")
        {
            enqueue(pop());
            expr();
        }
    }

    void identifier()
    {
        var token = pop();
        if (token == null)
            throw new Exception();

        if (!Regex.IsMatch(token, "^[0-9]*\\.?[0-9]+$") &&
            !Regex.IsMatch(token, "^0x[0-9A-Fa-f]+$") &&
            !Regex.IsMatch(token, "^[A-Za-z_][0-9A-Za-z_]*$"))
        {
            throw new Exception($"illegal identifier '{token}'");
        }

        enqueue(token);
    }
}

IEnumerable<string> Analyze(IEnumerable<string> tokens)
{
    var operators = Operators
        .Select((o, i) => (o, i))
        .SelectMany(e => e.o.Select(o => (o.op, e.i, o.leftAssoc)))
        .ToDictionary(i => i.op, i => (priority: i.i, i.leftAssoc));

    bool? isOperator(string t) => operators?.ContainsKey(t);
    bool? isFunction(string t) => Functions.ContainsKey(t);

    var queue = new Queue<string>();
    var stack = new Stack<string>();

    foreach (var t in tokens)
    {
        if (isFunction(t) == true)
        {
            stack.Push(t);
        }
        else if (t == ",")
        {
            if (!stack.Contains("("))
                throw new Exception("illegal comma");

            while (stack.First() != "(")
            {
                queue.Enqueue(stack.Pop());
            }
        }
        else if (isOperator(t) == true)
        {
            while (stack.Count > 0)
            {
                var top = stack.First();
                if (isOperator(top) != true) break;

                var op1 = operators[t];
                var op2 = operators[top];

                if (op1.leftAssoc)
                {
                    if (op1.priority < op2.priority) break;
                }
                else
                {
                    if (op1.priority <= op2.priority) break;
                }
                queue.Enqueue(stack.Pop());
            }
            stack.Push(t);
        }
        else if (t == "(")
        {
            stack.Push(t);
        }
        else if (t == ")")
        {
            if (!stack.Contains("("))
                throw new Exception("No corresponding brackets");

            while (true)
            {
                var o = stack.Pop();
                if (o == "(") break;
                queue.Enqueue(o);
            }
            if (isFunction(stack.First()) == true)
            {
                queue.Enqueue(stack.Pop());
            }
        }
        else
        {
            queue.Enqueue(t);
        }
    }
    foreach (var o in stack)
        queue.Enqueue(o);

    return queue;
}

Dictionary<string, decimal> Variables = new()
{
    { "pi", (decimal)Math.PI }
};

decimal Evaluate(IEnumerable<string> rpn)
{
    decimal ToDecimal(string s)
    {
        try
        {
            return Variables.ContainsKey(s) ? Variables[s] : decimal.Parse(s);
        }
        catch
        {
            throw new Exception($"Invalid variable '{s}'");
        }
    }

    var operators = Operators
        .SelectMany(o => o.Select(e => (e.op, e.arguments, e.eval)))
        .ToDictionary(o => o.op, o => (o.arguments, o.eval))
        .Concat(Functions)
        .ToDictionary(o => o.Key, o => (o.Value.arguments, o.Value.eval));

    var stack = new Stack<string>();
    foreach (var i in rpn)
    {
        if (operators.ContainsKey(i))
        {
            var op = operators[i];
            if (stack.Count < op.arguments)
            {
                throw new Exception("Missing arguments.");
            }
            var args = Enumerable.Range(0, op.arguments)
                .Select(_ => ToDecimal(stack.Pop()))
                .Reverse()
                .ToArray();

            var v = op.eval?.Invoke(args);
            stack.Push($"{v}");
            //Console.WriteLine($"{i}: {string.Join(",", args.Select(o => $"{o}"))} => {v}");
        }
        else
        {
            stack.Push(i);
        }
    }
    return ToDecimal(stack.Pop());
}

decimal calculete(string formula)
{
    var part = formula.Split('=');
    var tokens = part.Length switch
    {
        1 => Split(part[0]),
        2 => Split(part[1]),
        _ => throw new Exception()
    };

    var parsed = Parse(tokens);
    var rpn = Analyze(parsed);
    var v = Evaluate(rpn);

    if (part.Length == 2)
    {
        Variables[part[0].Trim()] = v;
    }
    return v;
}

void test(string formula)
{
    try
    {
        Console.WriteLine($"INPUT: {formula}");
        var v = calculete(formula);
        Console.WriteLine($"OUTPUT: {v}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"error: {ex.Message}");
    }
    Console.WriteLine();
}

test("sqrt(3 + 4 * 2 / ( 1 - 5 ) ** 2 ** 3)");
test("func()");
test("3 + 2");
test("2");
test("-3 + 2");
test("-3 + -2");
test("+3 + +2");
test("~3 + ~2");
test("3 + 4 * 2 / ( 1 - 5 ) ** 2 ** 3");
test("3+4*2/(1-5)**2**3");
test("D(1 - 2 * 3 + 4, ~5, 6)");
test("1.5*2.22");
test("sqrt(2*3)");
test("1-sqrt(2)");
test("-sqrt(2)");
test("-1 - 1");
test("-1 - -1");
test("pi * 5 ** 2");
test("x = 1 + 2");
test("y = 2 * x + 1");
test("x + y");
test("x + z");

for (; ; );
