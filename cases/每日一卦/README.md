# 每日一卦

此用例打开即会显示六十四卦中的一卦（一天内每次打开都相同，每天打开会有所不同），提供卦画、卦爻辞、彖传象传辞，并提供其变卦、错卦、互卦、综卦的相关信息。

When this use case is opened, one of the hexagrams will be shown (it will be the same in a day, but could be in different days). This use case provides its paintings, text in Zhouyi and Yizhuan, as well as the information of the changed hexagrams, the laterally linked hexagram, the overlapping hexagram, and overturned hexagram of it.

## 使用的包 Used Packages

所有包都可以在 [nuget.org](https://www.nuget.org/) 找到。

All the packages could be found on [nuget.org](https://www.nuget.org/).

- YiJingFramework.Annotating.Zhouyi
- YiJingFramework.EntityRelations

## 代码 Codes

```csharp
using System.Diagnostics;
using System.Net.Http.Json;
using YiJingFramework.Annotating.Zhouyi;
using YiJingFramework.Annotating.Zhouyi.Entities;
using YiJingFramework.EntityRelations.GuaDerivations.Extensions;
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
        var (upper, lower) = hexagram.SplitToTrigrams(zhouyi);

        Console.Clear();

        Console.WriteLine($"{date:yyyy年 M月 d日}   {message}");
        Console.WriteLine();

        if (upper.Painting == lower.Painting)
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
        var result = gua.ChangeLines(valuesMinus1, false);
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
2023年 8月 5日   每日一卦 A Hexagram Per Day

泽水困
困，亨贞大人吉无咎有言不信
象曰：泽无水困君子以致命遂志
彖曰：困刚掩也险以说困而不失其所亨其唯君子乎贞大人吉以刚中也有言不信尚口乃穷也


-- --   困于葛藟于臲卼曰动悔有悔征吉　　　　困于葛藟未当也动悔有悔吉行也
-----   劓刖困于赤绂乃徐有说利用祭祀　　　　劓刖志未得也乃徐有说以中直也利用祭祀受福也
-----   来徐徐困于金车吝有终　　　　　　　　来徐徐志在下也虽不当位有与也
-- --   困于石据于蒺藜入于其宫不见其妻凶　　据于蒺藜乘刚也入于其宫不见其妻不祥也
-----   困于酒食朱绂方来利用亨祀征凶无咎　　困于酒食中有庆也
-- --   臀困于株木入于幽谷三岁不见　　　　　入于幽谷幽不明也

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
2023年 8月 5日   变卦 Changed (1 3 6)

乾为天
乾，元亨利贞
象曰：天行健君子以自强不息
彖曰：大哉乾元万物资始乃统天云行雨施品物流形大明终始六位时成时乘六龙以御天乾道变化各正性命保合大和乃利贞首出庶物万国咸宁

        见群龙无首吉　　　　　　　　用九天德不可为首也
-----   亢龙有悔　　　　　　　　　　亢龙有悔盈不可久也
-----   飞龙在天利见大人　　　　　　飞龙在天大人造也
-----   或跃在渊无咎　　　　　　　　或跃在渊进无咎也
-----   君子终日乾乾夕惕若厉无咎　　终日乾乾反复道也
-----   见龙在田利见大人　　　　　　见龙在田德施普也
-----   潜龙勿用　　　　　　　　　　潜龙勿用阳在下也

==================================

输入任意内容以返回 Input Anything To Get Back

==================================
```

```plain
2023年 8月 5日   综卦 Overturned

水风井
井，改邑不改井无丧无得往来井井汔至亦未繘井羸其瓶凶
象曰：木上有水井君子以劳民劝相
彖曰：巽乎水而上水井井养而不穷也改邑不改井乃以刚中也汔至亦未繘井未有功也羸其瓶是以凶也


-- --   井收勿幕有孚元吉　　　　　　　　　　　元吉在上大成也
-----   井冽寒泉食　　　　　　　　　　　　　　寒泉之食中正也
-- --   井甃无咎　　　　　　　　　　　　　　　井甃无咎修井也
-----   井渫不食为我心恻可用汲王明并受其福　　井渫不食行恻也求王明受福也
-----   井谷射鲋瓮敝漏　　　　　　　　　　　　井谷射鲋无与也
-- --   井泥不食旧井无禽　　　　　　　　　　　井泥不食下也旧井无禽时舍也

==================================

输入任意内容以返回 Input Anything To Get Back

==================================
```