using System.Diagnostics;
using System.Net.Http.Json;
using LunarCsharpYiJingFrameworkExtensions;
using YiJingFramework.Annotating.Zhouyi;
using YiJingFramework.Annotating.Zhouyi.Entities;
using YiJingFramework.EntityRelations.GuaDerivations.Extensions;
using YiJingFramework.PrimitiveTypes;
using YiJingFramework.PrimitiveTypes.GuaWithFixedCount;

DateTime dateTime = DateTime.Now;
Console.WriteLine(dateTime.ToString("yyyy/MM/dd HH:mm"));
Console.WriteLine();

#region 获取年月日时数 Get the number of year, month, day and hour
if (dateTime.Hour is 23)
    dateTime = dateTime.AddHours(1);
// 使 23:00 到 24:00 取为后一日。
// Let 23:00-24:00 to be considered as the next day.

Lunar.Lunar lunar = Lunar.Lunar.FromDate(dateTime);
// 获取农历时间。
// Get lunar time.

Console.WriteLine(lunar);
Console.WriteLine();

int yearNumber = lunar.YearZhi().Index;
// 《梅花易数》：如子年一数丑年二数直至亥年十二数
// The year number will be 1 if it's in the years of Zi, 2 in Chou (usually considered the second), ..., 12 in Hai (usually considered the 12th).

int monthNumber = Math.Abs(lunar.Month);
// 《梅花易数》：月如正月一数直至十二月亦作十二数
// The month number is the 1-based index of the (lunar) month.

int dayNumber = lunar.Day;
// 《梅花易数》：日数如初一一数直至三十日为三十数
// just like the month number

int timeNumber = lunar.TimeZhi().Index;
// 《梅花易数》：时如子时一数直至亥时为十二数
// just like the year number

#endregion

#region 算卦数 Calculate the numbers of the hexagrams

int upperNumber = (yearNumber + monthNumber + dayNumber) % 8;
upperNumber = upperNumber == 0 ? 8 : upperNumber;
// 《梅花易数》：年月日共计几数以八除之以零数作上卦
// just do as the above two lines to get the index of the upper hexagram

int lowerNumber = (yearNumber + monthNumber + dayNumber + timeNumber) % 8;
lowerNumber = lowerNumber == 0 ? 8 : lowerNumber;
// 《梅花易数》：年月日数加时之数总计几数以八除之零数作下卦
// just do as the above two lines to get the index of the lower hexagram

int changingLineIndex = (yearNumber + monthNumber + dayNumber + timeNumber) % 6;
changingLineIndex = changingLineIndex == 0 ? 6 : changingLineIndex;
// 《梅花易数》：就以除六数作动爻
// just do as the above two lines to get the index of the changing line

#endregion

#region 取本卦卦画 Get the original hexagram

static GuaTrigram GetTrigram(int numberXiantian)
{
    numberXiantian--;
    Debug.Assert(numberXiantian is >= 0b000 and <= 0b111);
    return new GuaTrigram(
        new Yinyang((numberXiantian & 0b100) is 0),
        new Yinyang((numberXiantian & 0b010) is 0),
        new Yinyang((numberXiantian & 0b001) is 0));
}
// 这是通过先天八卦数获取画卦的数学方法。
// 也可以直接查表：1->☰ 2->☱ 3->☲ 4->☳ 5->☴ 6->☵ 7->☶ 8->☷
// This is a mathematical method to get the painting through the Xiantian number.
// You can also directly do this by mapping：1->☰ 2->☱ 3->☲ 4->☳ 5->☴ 6->☵ 7->☶ 8->☷

GuaTrigram upperTrigram = GetTrigram(upperNumber);
GuaTrigram lowerTrigram = GetTrigram(lowerNumber);
// 获取上卦和下卦的卦画。
// Get the paintings of the upper and the lower trigram.

IEnumerable<Yinyang> originalLines = lowerTrigram.Concat(upperTrigram);
GuaHexagram originalHexagram = new GuaHexagram(originalLines);
// 将上卦下卦放在一起得到一个六爻卦，这就是本卦。
// Put the trigrams together to get a hexagram, which is called the original hexagram.

#endregion

#region 取变卦卦画 Get the changed hexagram

GuaHexagram changedHexagram = originalHexagram.ChangeLines(changingLineIndex - 1);
// 这里使用了 YiJingFramework.EntityRelations 提供的拓展方法，
// 把对应的爻阴阳性质改变，返回新的卦，即变卦。
// Here we used the extension method provided by the YiJingFramework.EntityRelations.
// The specific line's yin-yang attribute has been changed and a new hexagram has returned, which is called as the changed hexagram.

#endregion

#region 取互卦卦画 Get the overlapping hexagram

GuaHexagram overlappingHexagram = originalHexagram.Hugua();
// 仍是 YiJingFramework.EntityRelations 包提供的拓展方法，
// 二三四爻作下卦，三四五爻作上卦产生新的卦，这就是互卦。
// It's also an extension method provided by the YiJingFramework.EntityRelations package.
// It returns a new hexagram which will be made up of --
// the second line, the third line, the fourth line,
// then the third line again, the fourth line again and the fifth line 
// -- of the original hexagram.
// This new hexagram is the so-called overlapping hexagram.

if (overlappingHexagram == originalHexagram)
    overlappingHexagram = changedHexagram.Hugua();
// 《梅花易数》：乾坤无互互其变卦
// If the original is Qian or Kun, which does not have a overlapping hexagram, use the changed's instead. 

#endregion

#region 将三个卦打印出来 Print the three hexagrams
static void PrintHexagram(GuaHexagram hexagram)
{
    for (int i = 5; i >= 0; i--)
        Console.WriteLine(hexagram[i].IsYang ? "-----" : "-- --");
}

Console.WriteLine("本卦 THE ORIGINAL");
PrintHexagram(originalHexagram);
Console.WriteLine();

Console.WriteLine("互卦 THE OVERLAPPING");
PrintHexagram(overlappingHexagram);
Console.WriteLine();

Console.WriteLine("变卦 THE CHANGED");
PrintHexagram(changedHexagram);
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
// Here the annotation store is downloaded from the internet,
// You can also load it from elsewhere such as from a local file.

ZhouyiHexagram originalInZhouyi = zhouyi.GetHexagram(originalHexagram);
ZhouyiHexagramLine changingLine = originalInZhouyi.EnumerateLines().ElementAt(changingLineIndex - 1);
ZhouyiHexagram changedInZhouyi = zhouyi.GetHexagram(changedHexagram);
ZhouyiHexagram overlappingInZhouyi = zhouyi.GetHexagram(overlappingHexagram);

Console.Write($"{originalInZhouyi.Name}之{changedInZhouyi.Name}，");
// Console.Write($"It's {originalInZhouyi.Name} changing to {changedInZhouyi.Name}, ");

(ZhouyiTrigram overlappingUpper, ZhouyiTrigram overlappingLower) = overlappingInZhouyi.SplitToTrigrams(zhouyi);
if (overlappingUpper.Painting == overlappingLower.Painting)
{
    Console.WriteLine($"互重{overlappingUpper.Name}。");
    // Console.Write($"and doubled {overlappingUpperInZhouyi.Name} as the overlapping.");
}
else
{
    Console.WriteLine($"互{overlappingUpper.Name}{overlappingLower.Name}。");
    // Console.Write($"and {overlappingUpperInZhouyi.Name} with {overlappingLowerInZhouyi.Name} as the overlapping.");
}

Console.WriteLine($"易曰：{changingLine.LineText}");
// Console.WriteLine($"Zhouyi: {changingLine.LineText}");
Console.WriteLine($"象曰：{changingLine.Xiang}");
// Console.WriteLine($"And xiang: {changingLine.Xiang}");
#endregion