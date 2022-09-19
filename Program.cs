// See https://aka.ms/new-console-template for more information

List<List<(string op, bool leftAssoc)>> Operators = new()
{
    new() { ("~", false) },
    new() { ("**", false) },
    new() { ("*", true), ("/", true), ("%", true) },
    new() { ("+", true), ("-", true) },
};

string[] Functions = new string[]
{
    "sqrt", "D"
};

IEnumerable<string> Split(string formula)
{
    var operators = Operators
         .SelectMany(e => e.Select(o => o.op))
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

IEnumerable<string> Analyze(IEnumerable<string> tokens)
{
    var operators = Operators
        .Select((o, i) => (o, i))
        .SelectMany(e => e.o.Select(o => (o.op, e.i, o.leftAssoc)))
        .ToDictionary(i => i.op, i => (priority: i.i, i.leftAssoc));

    bool? isOperator(string t) => operators?.ContainsKey(t);
    bool? isFunction(string t) => Functions.Contains(t);

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
                throw new Exception();

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
                throw new Exception();

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

decimal Evaluate(IEnumerable<string> rpn)
{
    Dictionary<string, (int arguments, Func<IEnumerable<decimal>, decimal> eval)> operators = new()
    {
        { "~", (1, (a) => ~(int)a.First()) },
        { "**", (2, (a) => (decimal)Math.Pow((double)a.First(), (double)a.Last())) },
        { "*", (2, (a) => a.First() * a.Last()) },
        { "/", (2, (a) => a.First() / a.Last()) },
        { "%", (2, (a) => a.First() % a.Last()) },
        { "+", (2, (a) => a.First() + a.Last()) },
        { "-", (2, (a) => a.First() - a.Last()) },
        { "sqrt", (1, (a) => (decimal)Math.Sqrt((double)a.First())) },
        { "D", (3, (a) => a.First() + a.Skip(1).Take(1).First() + a.Last()) },
    };

    var stack = new Stack<string>();
    foreach (var i in rpn)
    {
        if (operators.ContainsKey(i))
        {
            var op = operators[i];
            if (stack.Count < op.arguments)
            {
                throw new Exception();
            }
            var args = Enumerable.Range(0, op.arguments)
                .Select(_ => decimal.Parse(stack.Pop()))
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
    return decimal.Parse(stack.Pop());
}

void test(string formula)
{
    try
    {
        Console.WriteLine($"INPUT: {formula}");
        var tokens = Split(formula);
        //Console.WriteLine(string.Join(' ', tokens));
        var rpn = Analyze(tokens);
        //Console.WriteLine(string.Join(' ', rpn));
        var v = Evaluate(rpn);
        Console.WriteLine($"value = {v}");
    }
    catch (Exception)
    {
        Console.WriteLine("error...");
    }
    Console.WriteLine();
}

test("3 + 4 * 2 / ( 1 - 5 ) ** 2 ** 3");
test("3+4*2/(1-5)**2**3");
test("D(1 - 2 * 3 + 4, ~5, 6)");
test("1.5*2.22");
test("sqrt(2*3)");
test("1-sqrt(2)");
test("-sqrt(2)");   // error
test("-1 - 1");     // error
test("-1 - -1");    // error

for (; ; );
