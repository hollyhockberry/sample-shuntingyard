// See https://aka.ms/new-console-template for more information

IEnumerable<string> Split(string formula)
{
    var operators = new string[] { "~", "**", "*", "/", "%", "+", "-", "(", ")", "," };
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

void test(string formula)
{
    try
    {
        Console.WriteLine($"INPUT: {formula}");
        var tokens = Split(formula);
        Console.WriteLine(string.Join(' ', tokens));
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
