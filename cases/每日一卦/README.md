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
using System.Net.Http.Json;
using YiJingFramework.Annotating.Zhouyi;
using YiJingFramework.Annotating.Zhouyi.Entities;
using YiJingFramework.EntityRelationships.MostAccepted.GuaDerivingExtensions;
using YiJingFramework.PrimitiveTypes;
using YiJingFramework.PrimitiveTypes.GuaWithFixedCount;

internal static class Program
{
    private static async Task Main()
    {
        var zhouyi = await DownloadStoreAsync();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var gua = GetTheGuaOf(today);

        var message = "每日一卦 A Hexagram Per Day";
        for (; ; )
        {
            PrintGua(zhouyi, today, gua, message);

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

            (var derivedGua, message) = inputs[0] switch
            {
                "1" => ApplyDerivation1(gua, inputs.Skip(1)),
                "2" => ApplyDerivation2(gua),
                "3" => ApplyDerivation3(gua),
                "4" => ApplyDerivation4(gua),
                _ => ApplyDerivationBadInput()
            };

            if (derivedGua is not null)
            {
                PrintGua(zhouyi, today, derivedGua, message);
                message = "每日一卦 A Hexagram Per Day";

                Console.WriteLine("==================================");
                Console.WriteLine();
                Console.WriteLine("输入任意内容以返回 Input Anything To Get Back");
                Console.WriteLine();
                Console.WriteLine("==================================");
                Console.WriteLine();
                _ = Console.ReadLine();
            }
        }
    }

    private static async ValueTask<ZhouyiStore> DownloadStoreAsync()
    {
        var uri = "https://yueyinqiu.github.io/my-yijing-annotation-stores/975345ca/2023-08-02-1.json";
        using var client = new HttpClient();
        var store = await client.GetFromJsonAsync<ZhouyiStore>(uri);
        Debug.Assert(store is not null);
        return store;
    }

    private static GuaHexagram GetTheGuaOf(DateOnly date)
    {
        static IEnumerable<Yinyang> RandomYinYangs(int seed)
        {
            Random random = new Random(seed);
            for (; ; )
                yield return (Yinyang)random.Next(0, 2);
        }

        var randomLines = RandomYinYangs(date.DayNumber).Take(6);
        return new GuaHexagram(randomLines);
    }

    private static void PrintGua(
        ZhouyiStore zhouyi,
        DateOnly date, 
        GuaHexagram gua, 
        string message)
    {
        ZhouyiHexagram hexagram = zhouyi.GetHexagram(gua);

        var (upperPainting, lowerPainting) = hexagram.SplitToTrigrams();
        var upper = zhouyi.GetTrigram(upperPainting);
        var lower = zhouyi.GetTrigram(lowerPainting);

        Console.Clear();

        Console.WriteLine($"{date:yyyy年 M月 d日}   {message}");
        Console.WriteLine();

        if (upperPainting == lowerPainting)
            Console.WriteLine($"{hexagram.Name}为{upper.Nature}");
        else
            Console.WriteLine($"{upper.Nature}{lower.Nature}{hexagram.Name}");

        Console.WriteLine($"{hexagram.Name}，{hexagram.Text}");
        Console.WriteLine($"象曰：{hexagram.Xiang}");
        Console.WriteLine($"彖曰：{hexagram.Tuan}");
        Console.WriteLine();

        var lineTextPadding = hexagram.EnumerateLines()
            .Select(line => line.LineText?.Length ?? 0)
            .Max() + 2;

        foreach (var line in hexagram.EnumerateLines().Reverse())
        {
            var figure = line.YinYang?.IsYang switch
            {
                true => "-----   ",
                false => "-- --   ",
                null => "        "
            };
            var text = line.LineText?.PadRight(lineTextPadding, '　');
            Console.WriteLine($"{figure}{text}{line.Xiang}");
        }
        Console.WriteLine();
    }

    private static (GuaHexagram?, string) ApplyDerivationBadInput()
    {
        return (null, "请从中选择一项 Please Select One Item");
    }

    private static (GuaHexagram?, string) ApplyDerivation1(
        GuaHexagram gua, IEnumerable<string> args)
    {
        List<int> values = new List<int>();
        List<int> valuesMinus1 = new List<int>();
        foreach (var str in args)
        {
            if (!int.TryParse(str, out int value))
                return (null, "参数错误 Invalid Arguments");
            values.Add(value);
            valuesMinus1.Add(value - 1);
        }
        var result = gua.ReverseLines(valuesMinus1, false);
        return (result, $"变卦 Changed ({string.Join(' ', values)})");
    }

    private static (GuaHexagram?, string) ApplyDerivation2(GuaHexagram gua)
    {
        return (gua.Cuogua(), "错卦 Laterally Linked");
    }

    private static (GuaHexagram?, string) ApplyDerivation3(GuaHexagram gua)
    {
        return (gua.Hugua(), "互卦 Overlapping");
    }

    private static (GuaHexagram?, string) ApplyDerivation4(GuaHexagram gua)
    {
        return (gua.Zonggua(), "综卦 Overturned");
    }
}
```

## 输出样例 Sample Output

此用例支持互动，在这里仅提供部分输出内容。

This use case contains human-computer interaction, so only part of the output is provided here.

```plain
2023年 8月 2日   每日一卦 A Hexagram Per Day

天泽履
履，履虎尾不咥人亨
象曰：上天下泽履君子以辨上下定民志
彖曰：履柔履刚也说而应乎乾是以履虎尾不咥人亨刚中正履帝位而不疚光明也


-----   视履考祥其旋元吉　　　　　　　　　　　　元吉在上大有庆也
-----   夬履贞厉　　　　　　　　　　　　　　　　夬履贞厉位正当也
-----   履虎尾愬愬终吉　　　　　　　　　　　　　愬愬终吉志行也
-- --   眇能视跛能履履虎尾咥人凶武人为于大君　　眇能视不足以有明也跛能履不足以与行也咥人之凶位不当也武人为于大君志刚也
-----   履道坦坦幽人贞吉　　　　　　　　　　　　幽人贞吉中不自乱也
-----   素履往无咎　　　　　　　　　　　　　　　素履之往独行愿也

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
2023年 8月 2日   变卦 Changed (1 2 4 5 6)

坤为地
坤，元亨利牝马之贞君子有攸往先迷后得主利西南得朋东北丧朋安贞吉
象曰：地势坤君子以厚德载物
彖曰：至哉坤元万物资生乃顺承天坤厚载物德合无疆含弘光大品物咸亨牝马地类行地无疆柔顺利贞君子攸行先迷失道后顺得常西南得朋乃与类行东北丧朋乃终有庆安贞之吉应地无疆

        利永贞　　　　　　　　　　　用六永贞以大终也
-- --   龙战于野其血玄黄　　　　　　龙战于野其道穷也
-- --   黄裳元吉　　　　　　　　　　黄裳元吉文在中也
-- --   括囊无咎无誉　　　　　　　　括囊无咎慎不害也
-- --   含章可贞或从王事无成有终　　含章可贞以时发也或从王事知光大也
-- --   直方大不习无不利　　　　　　六二之动直以方也不习无不利地道光也
-- --   履霜坚冰至　　　　　　　　　履霜坚冰阴始凝也驯致其道至坚冰也

==================================

输入任意内容以返回 Input Anything To Get Back

==================================
```

```plain
2023年 8月 2日   综卦 Overturned

风天小畜
小畜，亨密云不雨自我西郊
象曰：风行天上小畜君子以懿文德
彖曰：小畜柔得位而上下应之曰小畜健而巽刚中而志行乃亨密云不雨尚往也自我西郊施未行也


-----   既雨既处尚德载妇贞厉月几望君子征凶　　既雨既处德积载也君子征凶有所疑也
-----   有孚挛如富以其邻　　　　　　　　　　　有孚挛如不独富也
-- --   有孚血去惕出无咎　　　　　　　　　　　有孚惕出上合志也
-----   舆说辐夫妻反目　　　　　　　　　　　　夫妻反目不能正室也
-----   牵复吉　　　　　　　　　　　　　　　　牵复在中亦不自失也
-----   复自道何其咎吉　　　　　　　　　　　　复自道其义吉也

==================================

输入任意内容以返回 Input Anything To Get Back

==================================
```