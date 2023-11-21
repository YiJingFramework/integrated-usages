# 每日一卦

此用例打开即会显示六十四卦中的一卦（一天内每次打开都相同，每天打开会有所不同），提供卦画、卦爻辞、彖传象传辞，并提供其变卦、错卦、互卦、综卦的相关信息。

When this use case is opened, one of the hexagrams will be shown (it will be the same in a day, but could be in different days). This use case provides its paintings, text in Zhouyi and Yizhuan, as well as the information of its Biangua, Cuogua, Hugua, and Zonggua.

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
            Console.WriteLine("1. Change some of the Yaos (lines). Numbers from 1 to 6 are required to indicate the changing Yaos (like `1 3 4` to change the third and the fourth Yao)");
            Console.WriteLine("2. Get the Cuogua (the laterally linked hexagram).");
            Console.WriteLine("3. Get the Hugua (the overlapping hexagram).");
            Console.WriteLine("4. Get the Zonggua (overturned hexagram).");
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

        var randomYaos = RandomYinYangs(date.DayNumber).Take(6);
        return new GuaHexagram(randomYaos);
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

        var textPadding = hexagram.EnumerateYaos()
            .Select(yao => yao.YaoText?.Length ?? 0)
            .Max() + 2;

        foreach (var yao in hexagram.EnumerateYaos().Reverse())
        {
            var figure = yao.YinYang?.IsYang switch
            {
                true => "-----   ",
                false => "-- --   ",
                null => "        "
            };
            var text = yao.YaoText?.PadRight(textPadding, '　');
            Console.WriteLine($"{figure}{text}{yao.Xiang}");
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
        List<int> values = [];
        foreach (var str in args)
        {
            if (!int.TryParse(str, out int value))
                return (null, "参数错误 Invalid Arguments");
            values.Add(value);
        }
        var result = gua.ChangeYaos(values.Select(x => x - 1), false);
        return (result, $"变卦 Biangua ({string.Join(' ', values)})");
    }

    private static (GuaHexagram?, string) ApplyDerivation2(GuaHexagram gua)
    {
        return (gua.Cuogua(), "错卦 Cuogua");
    }

    private static (GuaHexagram?, string) ApplyDerivation3(GuaHexagram gua)
    {
        return (gua.Hugua(), "互卦 Hugua");
    }

    private static (GuaHexagram?, string) ApplyDerivation4(GuaHexagram gua)
    {
        return (gua.Zonggua(), "综卦 Zonggua");
    }
}
```

## 输出样例 Sample Output

此用例支持互动，在这里仅提供部分输出内容。

This use case contains human-computer interaction, so only part of the output is provided here.

```plain
2023年 11月 21日   每日一卦 A Hexagram Per Day

天火同人
同人，同人于野亨利涉大川利君子贞
象曰：天与火同人君子以类族辨物
彖曰：同人柔得位得中而应乎乾曰同人同人曰同人于野亨利涉大川乾行也文明以健中正而应君子正也唯君子为能通天下之志


-----   同人于郊无悔　　　　　　　　　同人于郊志未得也
-----   同人先号啕而后笑大师克相遇　　同人之先以中直也大师相遇言相克也
-----   乘其墉弗克攻吉　　　　　　　　乘其墉义弗克也其吉则困而反则也
-----   伏戎于莽升其高陵三岁不兴　　　伏戎于莽敌刚也三岁不兴安行也
-- --   同人于宗吝　　　　　　　　　　同人于宗吝道也
-----   同人于门无咎　　　　　　　　　出门同人又谁咎也

==================================

1. 改变其中几爻。需要若干个一到六的数字，表示要变的爻（如 `1 3 4` 以改变第三四爻）
2. 获取错卦。
3. 获取互卦。
4. 获取综卦。
e. 退出

1. Change some of the Yaos (lines). Numbers from 1 to 6 are required to indicate the changing Yaos (like `1 3 4` to change the third and the fourth Yao)
2. Get the Cuogua (the laterally linked hexagram).
3. Get the Hugua (the overlapping hexagram).
4. Get the Zonggua (overturned hexagram).
e. Exit

==================================
```

```plain
2023年 11月 21日   互卦 Hugua

天风姤
姤，女壮勿用取女
象曰：天下有风姤后以施命诰四方
彖曰：姤遇也柔遇刚也勿用取女不可与长也天地相遇品物咸章也刚遇中正天下大行也姤之时义大矣哉


-----   姤其角吝无咎　　　　　　　　　　　　姤其角上穷吝也
-----   以杞包瓜含章有陨自天　　　　　　　　九五含章中正也有陨自天志不舍命也
-----   包无鱼起凶　　　　　　　　　　　　　无鱼之凶远民也
-----   臀无肤其行次且厉无大咎　　　　　　　其行次且行未牵也
-----   包有鱼无咎不利宾　　　　　　　　　　包有鱼义不及宾也
-- --   系于金柅贞吉有攸往见凶羸豕孚踟蹰　　系于金柅柔道牵也

==================================

输入任意内容以返回 Input Anything To Get Back

==================================
```

```plain
2023年 11月 21日   变卦 Biangua (2 4 6)

水天需
需，有孚光亨贞吉利涉大川
象曰：云上于天需君子以饮食宴乐
彖曰：需须也险在前也刚健而不陷其义不困穷矣需有孚光亨贞吉位乎天位以正中也利涉大川往有功也


-- --   入于穴有不速之客三人来敬之终吉　　不速之客来敬之终吉虽不当位未大失也
-----   需于酒食贞吉　　　　　　　　　　　酒食贞吉以中正也
-- --   需于血出自穴　　　　　　　　　　　需于血顺以听也
-----   需于泥致寇至　　　　　　　　　　　需于泥灾在外也自我致寇敬慎不败也
-----   需于沙小有言终吉　　　　　　　　　需于沙衍在中也虽小有言以终吉也
-----   需于郊利用恒无咎　　　　　　　　　需于郊不犯难行也利用恒无咎未失常也

==================================

输入任意内容以返回 Input Anything To Get Back

==================================
```