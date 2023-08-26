using System.Diagnostics;
using System.Net.Http.Json;
using YiJingFramework.Annotating.Zhouyi;
using YiJingFramework.Annotating.Zhouyi.Entities;
using YiJingFramework.EntityRelations.GuaDerivations.Extensions;
using YiJingFramework.Nongli.Lunar;
using YiJingFramework.PrimitiveTypes;
using YiJingFramework.PrimitiveTypes.GuaWithFixedCount;

DateTime dateTime = DateTime.Now;

#region 获取年月日时数 Get the Shus (numbers) of Nian (year), Yue (month), Ri (day) and Shi (hour)
LunarDateTime nongliDateTime = LunarDateTime.FromGregorian(dateTime);
// 取农历年月日时。
// Get the date and time of Nongli.

int nianshu = nongliDateTime.Nian.Dizhi.Index;
// 《梅花易数》：如子年一数丑年二数直至亥年十二数
// The Nianshu will be 1 if it's the Nian of Zi, 2 if Chou, ..., 12 if Hai.

int yueshu = nongliDateTime.Yue;
// 《梅花易数》：月如正月一数直至十二月亦作十二数
// The Yueshu is the 1-based index of the Yue.

int rishu = nongliDateTime.Ri;
// 《梅花易数》：日数如初一一数直至三十日为三十数
// The Rishu is the 1-based index of the Ri.

int shishu = nongliDateTime.Shi.Index;
// 《梅花易数》：时如子时一数直至亥时为十二数
// The Shishu will be 1 if it's the Shi of Zi, 2 if Chou, ..., 12 if Hai.
#endregion

#region 算卦数 Calculate the Guashus (numbers of the Guas)
int upperGuashu = nianshu + yueshu + rishu;
int upperGuaIndex = upperGuashu % 8;
upperGuaIndex = upperGuaIndex == 0 ? 8 : upperGuaIndex;
// 《梅花易数》：年月日共计几数以八除之以零数作上卦
// just do as the above three lines to get the upper Guashu and the index of the upper Gua (trigram)

int lowerGuashu = nianshu + yueshu + rishu + shishu;
int lowerGuaIndex = lowerGuashu % 8;
lowerGuaIndex = lowerGuaIndex == 0 ? 8 : lowerGuaIndex;
// 《梅花易数》：年月日数加时之数总计几数以八除之零数作下卦
// just do as the above three lines to get the lower Guashu and the index of the lower Gua (trigram)

int guashu = lowerGuashu;
int dongyaoIndex = guashu % 6;
dongyaoIndex = dongyaoIndex == 0 ? 6 : dongyaoIndex;
// 《梅花易数》：就以除六数作动爻
// just do as the above three lines to get the total Guashu and the index of the Dongyao (changing line)
#endregion

#region 取本卦卦画 Get the Bengua (the original hexagram)
static GuaTrigram GetTrigram(int xiantanIndex)
{
    xiantanIndex--;
    Debug.Assert(xiantanIndex is >= 0b000 and <= 0b111);
    return new GuaTrigram(
        new Yinyang((xiantanIndex & 0b100) is 0),
        new Yinyang((xiantanIndex & 0b010) is 0),
        new Yinyang((xiantanIndex & 0b001) is 0));
}
// 这是通过先天八卦数获取画卦的数学方法。
// 也可以直接查表：1->☰ 2->☱ 3->☲ 4->☳ 5->☴ 6->☵ 7->☶ 8->☷
// This is a mathematical method to get the painting through the Xiantian indexes.
// It can also be done directly by mapping：1->☰ 2->☱ 3->☲ 4->☳ 5->☴ 6->☵ 7->☶ 8->☷

GuaTrigram upperGua = GetTrigram(upperGuaIndex);
GuaTrigram lowerGua = GetTrigram(lowerGuaIndex);
// 获取上卦和下卦的卦画。
// Get the paintings of the upper and the lower Guas (trigrams).

IEnumerable<Yinyang> linesOfBengua = lowerGua.Concat(upperGua);
GuaHexagram bengua = new GuaHexagram(linesOfBengua);
// 将上卦下卦放在一起得到一个六爻卦，这就是本卦。
// Put the two Guas (trigrams) together to get a Gua (hexagram), which is called Bengua (the original hexagram).
#endregion

#region 取变卦卦画 Get the Biangua (the changed hexagram)
GuaHexagram biangua = bengua.ChangeLines(dongyaoIndex - 1);
// 这里使用了 YiJingFramework.EntityRelations 提供的拓展方法，
// 把对应的爻阴阳性质改变，返回新的卦，即变卦。
// Here we use the extension method provided by the YiJingFramework.EntityRelations.
// The specific line's Yinyang attribute has been changed and a new Gua (hexgram) has been returned, which is called Biangua (the changed hexagram).
#endregion

#region 取互卦卦画 Get the Hugua (the overlapping hexagram)
GuaHexagram hugua = bengua.Hugua();
// 仍是 YiJingFramework.EntityRelations 包提供的拓展方法，
// 二三四爻作下卦，三四五爻作上卦产生新的卦，这就是互卦。
// It's also an extension method provided by the YiJingFramework.EntityRelations package.
// It returns a new hexagram which will be made up of --
// the second line, the third line, the fourth line,
// then the third line again, the fourth line again and the fifth line 
// -- of the original hexagram.
// This new hexagram is the so-called Hugua (the overlapping hexagram).

if (hugua == bengua)
    hugua = biangua.Hugua();
// 《梅花易数》：乾坤无互互其变卦
// If the Bengua is Qian or Kun, which does not have a Hugua, we should use the Biangua's Hugua instead. 
#endregion

#region 将三个卦打印出来 Print the three Guas (hexagrams)
static void PrintGua(GuaHexagram gua)
{
    for (int i = 5; i >= 0; i--)
        Console.WriteLine(gua[i].IsYang ? "-----" : "-- --");
}

Console.WriteLine("本卦 BENGUA");
PrintGua(bengua);
Console.WriteLine();

Console.WriteLine("互卦 HUGUA");
PrintGua(hugua);
Console.WriteLine();

Console.WriteLine("变卦 BIANGUA");
PrintGua(biangua);
Console.WriteLine();
#endregion

#region 查询《周易》及《易传》 Looking it up in Zhouyi and Yizhuan
var storeUri = "https://yueyinqiu.github.io/my-yijing-annotation-stores/975345ca/2023-08-02-1.json";
using var client = new HttpClient();
var zhouyi = await client.GetFromJsonAsync<ZhouyiStore>(storeUri);
Debug.Assert(zhouyi is not null);
// 初始化 ZhouyiStore 。
// 这里是从网上下载了一个注解仓库，当然也可以使用本地文件之类。
// Initialize ZhouyiStore.
// Here the annotation store is downloaded from the internet.
// You can also load it from elsewhere such as from a local file.

ZhouyiHexagram benguaInZhouyi = zhouyi.GetHexagram(bengua);
ZhouyiHexagramLine dongyao = benguaInZhouyi.EnumerateLines().ElementAt(dongyaoIndex - 1);
ZhouyiHexagram bianguaInZhouyi = zhouyi.GetHexagram(biangua);
ZhouyiHexagram huguaInZhouyi = zhouyi.GetHexagram(hugua);

Console.Write($"{benguaInZhouyi.Name}之{bianguaInZhouyi.Name}，");
// Console.Write($"It's {benguaInZhouyi.Name} changing to {bianguaInZhouyi.Name}, ");

(ZhouyiTrigram huguaUpper, ZhouyiTrigram huguaLower) = huguaInZhouyi.SplitToTrigrams(zhouyi);
if (huguaUpper.Painting == huguaLower.Painting)
{
    Console.WriteLine($"互重{huguaUpper.Name}。");
    // Console.WriteLine($"and doubled {huguaUpper.Name} as the Hugua.");
}
else
{
    Console.WriteLine($"互{huguaUpper.Name}{huguaLower.Name}。");
    // Console.WriteLine($"and {huguaUpper.Name} with {huguaLower.Name} as the Hugua.");
}

Console.WriteLine($"易曰：{dongyao.LineText}");
// Console.WriteLine($"Zhouyi: {dongyao.LineText}");
Console.WriteLine($"象曰：{dongyao.Xiang}");
// Console.WriteLine($"And Xiang: {dongyao.Xiang}");
#endregion