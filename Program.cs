// See https://aka.ms/new-console-template for more information

List<List<(string op, bool leftAssoc, int arguments, Func<IEnumerable<decimal>, decimal> eval)>> Operators = new()
{
    new() {
        ("~", false, 1, (a) => ~(int)a.First())
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
    { "D", (3, (a) => a.First() + a.Skip(1).Take(1).First() + a.Last()) }
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

Dictionary<string, decimal> Variables = new()
{
    { "pi", (decimal)Math.PI }
};

decimal Evaluate(IEnumerable<string> rpn)
{
    decimal ToDecimal(string s)
        => Variables.ContainsKey(s) ? Variables[s] : decimal.Parse(s);

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
                throw new Exception();
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

void test(string formula)
{
    try
    {
        Console.WriteLine($"INPUT: {formula}");
        var part = formula.Split('=');
        var tokens = part.Length switch
        {
            1 => Split(part[0]),
            2 => Split(part[1]),
            _ => throw new Exception()
        };

        Console.WriteLine(string.Join(' ', tokens));
        var rpn = Analyze(tokens);
        Console.WriteLine(string.Join(' ', rpn));
        var v = Evaluate(rpn);
        Console.WriteLine($"value = {v}");

        if (part.Length == 2)
        {
            Variables[part[0].Trim()] = v;
            Console.WriteLine($"store {v} to {part[0]}");
        }
    }
    {
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
test("pi * 5 ** 2");
test("x = 1 + 2");
test("y = 2 * x + 1");
test("x + y");
test("x + z");

for (; ; );
