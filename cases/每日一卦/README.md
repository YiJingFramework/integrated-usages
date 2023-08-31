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
        List<int> values = new List<int>();
        List<int> valuesMinus1 = new List<int>();
        foreach (var str in args)
        {
            if (!int.TryParse(str, out int value))
                return (null, "参数错误 Invalid Arguments");
            values.Add(value);
            valuesMinus1.Add(value - 1);
        }
        var result = gua.ChangeYaos(valuesMinus1, false);
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
2023年 8月 31日   每日一卦 A Hexagram Per Day

水火既济
既济，亨小利贞初吉终乱
象曰：水在火上既济君子以思患而豫防之
彖曰：既济亨小者亨也利贞刚柔正而位当也初吉柔得中也终止则乱其道穷也


-- --   濡其首厉　　　　　　　　　　　　　濡其首厉何可久也
-----   东邻杀牛不如西邻之禴祭实受其福　　东邻杀牛不如西邻之时也实受其福吉大来也
-- --   繻有衣袽终日戒　　　　　　　　　　终日戒有所疑也
-----   高宗伐鬼方三年克之小人勿用　　　　三年克之惫也
-- --   妇丧其茀勿逐七日得　　　　　　　　七日得以中道也
-----   曳其轮濡其尾无咎　　　　　　　　　曳其轮义无咎也

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
2023年 8月 31日   互卦 Hugua

火水未济
未济，亨小狐汔济濡其尾无攸利
象曰：火在水上未济君子以慎辨物居方
彖曰：未济亨柔得中也小狐汔济未出中也濡其尾无攸利不续终也虽不当位刚柔应也


-----   有孚于饮酒无咎濡其首有孚失是　　　　饮酒濡首亦不知节也
-- --   贞吉无悔君子之光有孚吉　　　　　　　君子之光其晖吉也
-----   贞吉悔亡震用伐鬼方三年有赏于大国　　贞吉悔亡志行也
-- --   未济征凶利涉大川　　　　　　　　　　未济征凶位不当也
-----   曳其轮贞吉　　　　　　　　　　　　　九二贞吉中以行正也
-- --   濡其尾吝　　　　　　　　　　　　　　濡其尾亦不知极也

==================================

输入任意内容以返回 Input Anything To Get Back

==================================
```

```plain
2023年 8月 31日   变卦 Biangua (2 4 6)

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