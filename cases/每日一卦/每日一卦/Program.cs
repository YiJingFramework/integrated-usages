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