using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using YiJingFramework.Annotating.Zhouyi;
using YiJingFramework.Annotating.Zhouyi.Entities;
using YiJingFramework.EntityRelationships.MostAccepted.GuaDerivingExtensions;
using YiJingFramework.PrimitiveTypes;
using YiJingFramework.PrimitiveTypes.GuaWithFixedCount;

var current = DateTime.Now;

static DateOnly? ParseToDateTime(string s, string? format = null)
{
    format = format ?? "yyyyMMdd";
    return DateOnly.TryParseExact(s, format, out var result) ? result : null;
}

var date = args.Length switch
{
    1 => ParseToDateTime(args[0]),
    2 => ParseToDateTime(args[0], args[1]),
    _ => null
} ?? DateOnly.FromDateTime(current);

new Program(date).Run();

internal partial class Program
{
    private readonly DateOnly date;
    private readonly GuaHexagram hexagram;

    private readonly ZhouyiStore zhouyi;

    private static GuaHexagram GetHexagram(int seed)
    {
        static IEnumerable<Yinyang> RandomYinYangs(int seed)
        {
            Random random = new Random(seed);
            for (; ; )
                yield return (Yinyang)random.Next(0, 2);
        }
        return new GuaHexagram(RandomYinYangs(seed).Take(6));
    }

    internal Program(DateOnly date)
    {
        this.date = date;
        this.hexagram = GetHexagram(date.DayNumber);

        var storeFile = File.ReadAllText("./zhouyi.json");
        var store = ZhouyiStore.DeserializeFromJsonString(storeFile);
        Debug.Assert(store is not null);

        this.zhouyi = store;
    }

    private void Print(GuaHexagram hexagramPainting, string message = "")
    {
        ZhouyiHexagram hexagram = zhouyi.GetHexagram(hexagramPainting);

        var (upperPainting, lowerPainting) = hexagram.SplitToTrigrams();
        var upper = zhouyi.GetTrigram(upperPainting);
        var lower = zhouyi.GetTrigram(lowerPainting);

        Console.Clear();

        Console.WriteLine($"{this.date:yyyy年 M月 d日}   {message}");
        Console.WriteLine();

        if (upperPainting == lowerPainting)
            Console.WriteLine($"{hexagram.Name}為{upper.Nature}");
        else
            Console.WriteLine($"{upper.Nature}{lower.Nature}{hexagram.Name}");

        Console.WriteLine($"{hexagram.Name}，{hexagram.Text}");
        Console.WriteLine($"象曰：{hexagram.Xiang}");
        Console.WriteLine($"彖曰：{hexagram.Tuan}");
        Console.WriteLine();

        var hexagramLines = hexagram.EnumerateLines(false)
            .Reverse()
            .Append(hexagram.Yong);

        var linePatterns = hexagramLines.Select(line => {
            if (line.YinYang.HasValue)
                return line.YinYang.Value.IsYang ? "-----   " : "-- --   ";
            return "        ";
        });

        var lineTexts = hexagramLines.Select(line => line.LineText);
        var padding = lineTexts.Select(line => {
            return line is null ? 0 : line.Length;
        }).Max() + 2;
        lineTexts = lineTexts.Select(text => text?.PadRight(padding, '　'));

        var xiangTexts = hexagramLines.Select(line => line.Xiang);

        foreach (var (pattern, text, xiangText) in linePatterns.Zip(lineTexts, xiangTexts))
            Console.WriteLine($"{pattern}{text}{xiangText}");
        Console.WriteLine();
    }

    internal void Run()
    {
        string message = "每日一卦 A Hexagram Per Day";
        for (; ; )
        {
            this.Print(this.hexagram, message);

            Console.WriteLine("==================================");
            Console.WriteLine();
            Console.WriteLine("1. 改变其中几爻。需要若干个一到六的数字，表示要变的爻（如 `1 3 4` 以改变第三四爻）");
            Console.WriteLine("2. 获取错卦。");
            Console.WriteLine("3. 获取互卦。");
            Console.WriteLine("4. 获取综卦。");
            Console.WriteLine("e. 退出");
            Console.WriteLine();
            Console.WriteLine("1. Change some of the lines. Numbers from 1 to 6 are required to indicate the changing lines (like `1 3 4` to change the third and the fourth line)");
            Console.WriteLine("2. Get the laterally linked hexagram.");
            Console.WriteLine("3. Get the overlapping hexagram.");
            Console.WriteLine("4. Get the overturned hexagram.");
            Console.WriteLine("e. Exit");
            Console.WriteLine();
            Console.WriteLine("==================================");
            Console.WriteLine();

            var input = Console.ReadLine();
            if (input is null or "e")
                return;
            var inputs = input.Split();
            if (inputs.Length < 1)
            {
                message = "请输入些什么 Please Input Something";
                continue;
            }

#pragma warning disable IDE0018
            GuaHexagram? result;
            string newMessage;
#pragma warning restore IDE0018
            var succeeded = inputs[0] switch
            {
                "1" => this.ApplyDerivation1(inputs.Skip(1), out result, out newMessage),
                "2" => this.ApplyDerivation2(out result, out newMessage),
                "3" => this.ApplyDerivation3(out result, out newMessage),
                "4" => this.ApplyDerivation4(out result, out newMessage),
                _ => this.ApplyDerivationBadInput(out result, out newMessage)
            };
            if (succeeded)
            {
                Debug.Assert(result is not null);
                this.Print(result, newMessage);
                Console.WriteLine("==================================");
                Console.WriteLine();
                Console.WriteLine("输入任意内容以返回 Input Anything To Get Back");
                Console.WriteLine();
                Console.WriteLine("==================================");
                Console.WriteLine();
                _ = Console.ReadLine();
                message = "每日一卦 A Hexagram Per Day";
            }
            else
                message = newMessage;
        }
    }

#pragma warning disable CA1822 // 将成员标记为 static
    private bool ApplyDerivationBadInput(
        [NotNullWhen(true)] out GuaHexagram? result,
        out string message)
    {
        result = null;
        message = "请从中选择一项 Please Select One Item";
        return false;
    }
#pragma warning restore CA1822 // 将成员标记为 static

    private bool ApplyDerivation1(
        IEnumerable<string> args,
        [NotNullWhen(true)] out GuaHexagram? result,
        out string message)
    {
        List<int> values = new List<int>();
        List<int> valuesMinus1 = new List<int>();
        foreach (var str in args)
        {
            if (!int.TryParse(str, out int value) || value < 1 || value > 6)
            {
                result = null;
                message = "参数错误 Invalid Arguments";
                return false;
            }
            values.Add(value);
            valuesMinus1.Add(value - 1);
        }
        result = this.hexagram.ReverseLines(valuesMinus1);
        message = $"变卦 Changed ({string.Join(' ', values)})";
        return true;
    }

    private bool ApplyDerivation2(
        [NotNullWhen(true)] out GuaHexagram? result,
        out string message)
    {
        result = this.hexagram.Cuogua();
        message = "错卦 Laterally Linked";
        return true;
    }
    private bool ApplyDerivation3(
        [NotNullWhen(true)] out GuaHexagram? result,
        out string message)
    {
        result = this.hexagram.Hugua();
        message = "互卦 Overlapping";
        return true;
    }
    private bool ApplyDerivation4(
        [NotNullWhen(true)] out GuaHexagram? result,
        out string message)
    {
        result = this.hexagram.Zonggua();
        message = "综卦 Overturned";
        return true;
    }
}