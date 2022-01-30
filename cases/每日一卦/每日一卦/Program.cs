using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using YiJingFramework.Core;
using YiJingFramework.Painting.Deriving.Extensions;
using YiJingFramework.References.Zhouyi;
using YiJingFramework.References.Zhouyi.Zhuan;

var current = DateTime.Now;

DateOnly? ParseToDateTime(string s, string? format = null)
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
    private readonly Painting hexagram;

    private readonly Zhouyi jing;
    private readonly Tuanzhuan tuan;
    private readonly XiangZhuan xiang;

    private static Painting GetHexagram(int seed)
    {
        IEnumerable<YinYang> RandomYinYangs(int seed)
        {
            Random random = new Random(seed);
            for (; ; )
                yield return (YinYang)random.Next(0, 2);
        }
        return new Painting(RandomYinYangs(seed).Take(6));
    }

    internal Program(DateOnly date)
    {
        this.date = date;
        this.hexagram = GetHexagram(date.DayNumber);

        using FileStream jingFile = new FileStream("./jing.json", FileMode.Open, FileAccess.Read);
        this.jing = new Zhouyi(jingFile);
        using FileStream tuanFile = new FileStream("./tuan.json", FileMode.Open, FileAccess.Read);
        this.tuan = new Tuanzhuan(tuanFile);
        using FileStream xiangFile = new FileStream("./xiang.json", FileMode.Open, FileAccess.Read);
        this.xiang = new XiangZhuan(xiangFile);
    }

    private void Print(Painting hexagramPainting, string message = "")
    {
        IEnumerable<ZhouyiHexagram.Line> AsEnumerable(ZhouyiHexagram zhouyiHexagram)
        {
            yield return zhouyiHexagram.FirstLine;
            yield return zhouyiHexagram.SecondLine;
            yield return zhouyiHexagram.ThirdLine;
            yield return zhouyiHexagram.FourthLine;
            yield return zhouyiHexagram.FifthLine;
            yield return zhouyiHexagram.SixthLine;
        }

        Debug.Assert(hexagramPainting.Count is 6);
        ZhouyiHexagram hexagram = this.jing.GetHexagram(hexagramPainting);

        ZhouyiTrigram upper = hexagram.UpperTrigram;
        ZhouyiTrigram lower = hexagram.LowerTrigram;

        Console.Clear();

        Console.WriteLine($"{this.date:yyyy年 M月 d日}   {message}");
        Console.WriteLine();

        if (upper == lower)
            Console.WriteLine($"{hexagram.Name}为{upper.Nature}");
        else
            Console.WriteLine($"{upper.Nature}{lower.Nature}{hexagram.Name}");

        Console.WriteLine(hexagram.Text);
        Console.WriteLine($"象曰：{this.xiang[hexagram]}");
        Console.WriteLine($"彖曰：{this.tuan[hexagram]}");
        Console.WriteLine();

        var hexagramLines = AsEnumerable(hexagram).Reverse();

        var linePatterns = hexagramLines.Select(line => line.YinYang.IsYang ? "-----   " : "-- --   ");

        var lineTexts = hexagramLines.Select(line => line.ToString());
        var padding = lineTexts.Select(line => line.Length).Max() + 2;
        lineTexts = lineTexts.Select(text => text.PadRight(padding, '　'));

        var xiangTexts = hexagramLines.Select(line => this.xiang[line]);

        foreach (var (pattern, text, xiangText) in linePatterns.Zip(lineTexts, xiangTexts))
            Console.WriteLine($"{pattern}{text}{xiangText}");
        Console.WriteLine();

        var applyNinesOrApplySixes = hexagram.ApplyNinesOrApplySixes;
        if (applyNinesOrApplySixes is not null)
            Console.WriteLine($"{applyNinesOrApplySixes.ToString().TrimEnd()}　　" +
                $"{this.xiang[applyNinesOrApplySixes]}");
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
            Painting? result;
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
        [NotNullWhen(true)] out Painting? result,
        out string message)
    {
        result = null;
        message = "请从中选择一项 Please Select One Item";
        return false;
    }
#pragma warning restore CA1822 // 将成员标记为 static

    private bool ApplyDerivation1(
        IEnumerable<string> args,
        [NotNullWhen(true)] out Painting? result,
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
        result = this.hexagram.ChangeLines(valuesMinus1);
        message = $"变卦 Changed ({string.Join(' ', values)})";
        return true;
    }

    private bool ApplyDerivation2(
        [NotNullWhen(true)] out Painting? result,
        out string message)
    {
        result = this.hexagram.ToLaterallyLinked();
        message = "错卦 Laterally Linked";
        return true;
    }
    private bool ApplyDerivation3(
        [NotNullWhen(true)] out Painting? result,
        out string message)
    {
        result = this.hexagram.ToOverlapping();
        message = "互卦 Overlapping";
        return true;
    }
    private bool ApplyDerivation4(
        [NotNullWhen(true)] out Painting? result,
        out string message)
    {
        result = this.hexagram.ToOverturned();
        message = "综卦 Overturned";
        return true;
    }
}