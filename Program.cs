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

void test(string formula)
{
    try
    {
        Console.WriteLine($"INPUT: {formula}");
        var tokens = Split(formula);
        Console.WriteLine(string.Join(' ', tokens));
        var rpn = Analyze(tokens);
        Console.WriteLine(string.Join(' ', rpn));
    }
    catch (Exception)
    {
        Console.WriteLine("error...");
    }
    Console.WriteLine();
}

test("3 + 4 * 2 / ( 1 - 5 ) ** 2 ** 3");
test("3+4*2/(1-5)**2**3");
test("D(f - b * c + d, ~e, g)");
test("1.5*2.22");
test("sqrt(2*3)");
test("-sqrt(2)");

for (; ; );
