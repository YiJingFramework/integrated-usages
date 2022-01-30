# 每日一卦

此用例打开即会显示六十四卦中的一卦（一天内每次打开都相同，每天打开会有所不同），提供卦画、卦爻辞、彖传象传辞，并提供其变卦、错卦、互卦、综卦的相关信息。

When this use case is opened, one of the hexagrams will be shown (it will be the same in a day, but could be in different days). This use case provides its paintings, text in Zhouyi and Yizhuan, as well as the information of the changed hexagrams, the laterally linked hexagram, the overlapping hexagram, and overturned hexagram of it.

## 使用的包 Used Packages

所有包都可以在 [nuget.org](https://www.nuget.org/) 找到。

All the packages could be found on [nuget.org](https://www.nuget.org/).

- YiJingFramework.Core
- YiJingFramework.Painting.Deriving
- YiJingFramework.References.Zhouyi
- YiJingFramework.References.Zhouyi.Zhuan

## 代码 Codes

```csharp
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
        foreach (var str in args)
        {
            if (!int.TryParse(str, out int value) || value < 1 || value > 6)
            {
                result = null;
                message = "参数错误 Invalid Arguments";
                return false;
            }
            values.Add(value - 1);
        }
        result = this.hexagram.ChangeLines(values);
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
```

## 输出样例 Sample Output

此用例支持互动，在这里仅提供部分输出内容。

This use case contains human-computer interaction, so only part of the output is provided here.

```plain
2022年 1月 30日   每日一卦 A Hexagram Per Day

乾为天
元，亨，利，贞。
象曰：天行健，君子以自强不息。
彖曰：大哉乾元，万物资始，乃统天。云行雨施，品物流形。大明终始，六位时成。时乘六龙以御天。乾道变化，各正性命。保合大和，乃利贞。首出庶物，万国威宁。

-----   上九：亢龙，有悔。　　　　　　　　　　　“亢龙有悔”，盈不可久也。
-----   九五：飞龙在天，利见大人。　　　　　　　“飞龙在天”，大人造也。
-----   九四：或跃在渊，无咎。　　　　　　　　　“或跃在渊”，进无咎也。
-----   九三：君子终日乾乾，夕惕若。厉无咎。　　“终日乾乾”，反复道也。
-----   九二：见龙在田，利见大人。　　　　　　　“见龙在田”，德施普也。
-----   初九：潜龙，勿用。　　　　　　　　　　　“潜龙勿用”，阳在下也。

用九：见群龙无首，吉。　　“用九”，天德不可为首也。

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
2022年 1月 30日   变卦 Changed (3 4)

风泽中孚
豚鱼，吉。利涉大川，利贞。
象曰：泽上有风，中孚。君子以议狱缓死。
彖曰：“中孚”，柔在内而刚得中，说而巽，孚乃化邦也。“豚鱼吉”，信及豚鱼也。“利涉大川”，乘木舟虚也。中孚以利贞，乃应乎天也。

-----   上九：翰音登于天，贞凶。　　　　　　　　　　　　　　“翰音登于天”，何可长也？
-----   九五：有孚挛如，无咎。　　　　　　　　　　　　　　　“有孚挛如”，位正当也。
-- --   六四：月几望，马匹亡，无咎。　　　　　　　　　　　　“马匹亡”，绝类上也。
-- --   六三：得敌，或鼓或罢，或泣或歌。　　　　　　　　　　“或鼓或罢”，位不当也。
-----   九二：鸣鹤在阴，其子和之。我有好爵，吾与尔靡之。　　“其子和之”，中心愿也。
-----   初九：虞吉，有它不燕。　　　　　　　　　　　　　　　初九“虞吉”，志未变也。


==================================

输入任意内容以返回 Input Anything To Get Back

==================================


```

```plain
2022年 1月 30日   错卦 Laterally Linked

坤为地
元亨。利牝马之贞。君子有攸往，先迷，後得主，利。西南得朋，东北丧朋。安贞吉。
象曰：地势坤。君子以厚德载物。
彖曰：至哉坤元，万物资生，乃顺承天。坤厚载物，德合无疆。含弘光大，品物咸亨。牝马地类，行地无疆，柔顺利贞。君子。君子攸行，先迷失道，後顺得常。西南得朋，乃与类行。东北丧朋，乃终有庆。安贞之吉，应地无疆。

-- --   上六：龙战于野，其血玄黄。　　　　　　　“龙战于野”，共道穷也。
-- --   六五：黄裳，元吉。　　　　　　　　　　　“黄裳元吉”，文在中也。
-- --   六四：括囊，无咎无誉。　　　　　　　　　“括囊无咎”，慎不害也。
-- --   六三：含章可贞，或从王事，无成有终。　　“含章可贞”，以时发也。“或従王事”，知光大也。
-- --   六二：直方大，不习，无不利。　　　　　　六二之动，直以方也。“不习无不利”，地道光也。
-- --   初六：履霜，坚冰至。　　　　　　　　　　“履霜坚冰”，阴始凝也，驯致其道，至坚冰也。

用六：利永贞。　　用六“永贞”，以大终也。

==================================

输入任意内容以返回 Input Anything To Get Back

==================================


```