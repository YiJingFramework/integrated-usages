# 每日一卦

此用例打开即会显示六十四卦中的一卦（一天内每次打开都相同，每天打开会有所不同），提供卦画、卦爻辞、彖传象传辞，并提供其变卦、错卦、互卦、综卦的相关信息。

When this use case is opened, one of the hexagrams will be shown (it will be the same in a day, but could be in different days). This use case provides its paintings, text in Zhouyi and Yizhuan, as well as the information of the changed hexagrams, the laterally linked hexagram, the overlapping hexagram, and overturned hexagram of it.

## 使用的包 Used Packages

所有包都可以在 [nuget.org](https://www.nuget.org/) 找到。

All the packages could be found on [nuget.org](https://www.nuget.org/).

- YiJingFramework.Annotating.Zhouyi
- YiJingFramework.EntityRelationships.MostAccepted

## 代码 Codes

```csharp
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
```

## 输出样例 Sample Output

此用例支持互动，在这里仅提供部分输出内容。

This use case contains human-computer interaction, so only part of the output is provided here.

```plain
2023年 5月 5日   每日一卦 A Hexagram Per Day

火水未濟
未濟，亨，小狐汔濟，濡其尾，無攸利。
象曰：火在水上，未濟。君子以慎辨物居方。
彖曰：「未濟，亨」，柔得中也。「小狐汔濟」，未出中也。「濡其尾，無攸利」，不續終也。雖不當位，剛柔應也。

-----   有孚于飲酒，無咎，濡其首，有孚失是。　　　　飲酒濡首，亦不知節也。
-- --   貞吉，無悔，君子之光，有孚，吉。　　　　　　「君子之光」，其暉吉也。
-----   貞吉，悔亡，震用伐鬼方，三年有賞于大國。　　「貞吉，悔亡」，志行也。
-- --   未濟，征凶，利涉大川。　　　　　　　　　　　「未濟，征凶」，位不當也。
-----   曳其輪，貞吉。　　　　　　　　　　　　　　　九二「貞吉」，中以行正也。
-- --   濡其尾，吝。　　　　　　　　　　　　　　　　「濡其尾」，亦不知極也。


==================================

1. 改变其中几爻。需要若干个一到六的数字，表示要变的爻（如 `1 3 4` 以改变第三四爻）
2. 获取错卦。
3. 获取互卦。
4. 获取综卦。
e. 退出

1. Change some of the lines. Numbers from 1 to 6 are required to indicate the changing lines (like `1 3 4` to change the third and the fourth line)
2. Get the laterally linked hexagram.
3. Get the overlapping hexagram.
4. Get the overturned hexagram.
e. Exit

==================================
```

```plain
2023年 5月 5日   变卦 Changed (1 2 5)

天雷無妄
無妄，元亨，利貞。其匪正有眚，不利有攸往。
象曰：天下雷行，物與無妄。先王以茂對時，育萬物。
彖曰：無妄，剛自外來而為主於內。動而健，剛中而應，大亨以正，天之命也。「其匪正有眚，不利有攸往」，無妄之往，何之矣？天命不佑行矣哉？

-----   無妄行，有眚，無攸利。　　　　　　　　　　　無妄之行，窮之災也。
-----   無妄之疾，勿藥有喜。　　　　　　　　　　　　無妄之藥，不可試也。
-----   可貞，無咎。　　　　　　　　　　　　　　　　「可貞，無咎」，固有之也。
-- --   無妄之災，或系之牛，行人之得，邑人之災。　　行人得牛，邑人災也。
-- --   不耕獲，不菑畬，則利有攸往。　　　　　　　　「不耕獲」，未富也。
-----   無妄往，吉。　　　　　　　　　　　　　　　　無妄之往，得志也。


==================================

输入任意内容以返回 Input Anything To Get Back

==================================
```

```plain
2023年 5月 5日   错卦 Laterally Linked

水火既濟
既濟，亨，小利貞，初吉終亂。
象曰：水在火上，既濟。君子以思患而預防之。
彖曰：「既濟，亨」，小者亨也。「利貞」，剛柔正而位當也。「初吉」，柔得中也。終止則亂，其道窮也。

-- --   濡其首，厲。　　　　　　　　　　　　　　「濡其首，厲」，何可久也！
-----   東鄰殺牛，不如西鄰之禴祭，實受其福。　　「東鄰殺牛」，不如西鄰之時也。「實受其福」，吉大來也。
-- --   儒有衣袽，終日戒。　　　　　　　　　　　「終日戒」，有所疑也。
-----   高宗伐鬼方，三年克之，小人勿用。　　　　「三年克之」，憊也。
-- --   婦喪其茀，勿逐，七日得。　　　　　　　　「七日得」，以中道也。
-----   曳其輪，濡其尾，無咎。　　　　　　　　　「曳其輪」，義無咎也。


==================================

输入任意内容以返回 Input Anything To Get Back

==================================
```