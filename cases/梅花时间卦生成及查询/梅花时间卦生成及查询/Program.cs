using com.nlf.calendar;
using System.Diagnostics;
using YiJingFramework.Core;
using YiJingFramework.Painting.Deriving.Extensions;
using YiJingFramework.References.Zhouyi;
using YiJingFramework.References.Zhouyi.Zhuan;

#region 获取年月日时数 Get the number of year, month, date and hour

Lunar lunar = Lunar.fromDate(DateTime.Now);
Console.WriteLine(lunar.toFullString());
Console.WriteLine();
// 获取农历时间。
// Get lunar time.

int yearBranchIndex = lunar.getYearZhiIndex();
// 获取支序数。
// 不像 YiJingFramework.StemsAndBranches ，
// 此库给出所谓的序数以子为零。
// Get the index of earthly branch.
// Unlike YiJingFramework.StemsAndBranches,
// the so-called indexes given by this repository use 0 to represents Zi(usually considered the first branch).

int yearNumber = yearBranchIndex + 1;
// 《梅花易数》：如子年一数丑年二数直至亥年十二数
// Year number will be 1 if it's in the years of Zi, 2 in Chou(usually considered the second), ... 12 in Hai(usually considered the 12th).

int monthNumber = lunar.getMonth();
// 《梅花易数》：月如正月一数直至十二月亦作十二数
// Month number will be the 1-based index of the (lunar) month.

int dayNumber = lunar.getDay();
// 《梅花易数》：日数如初一一数直至三十日为三十数
// just like the month number

int timeBranchIndex = lunar.getTimeZhiIndex();
int timeNumber = timeBranchIndex + 1;
// 《梅花易数》：时如子时一数直至亥时为十二数
// just like the year number

#endregion

#region 算卦数 Calculate the numbers about the hexagrams

int upperNumber = (yearNumber + monthNumber + dayNumber) % 8;
// 《梅花易数》：年月日共计几数以八除之以零数作上卦
// just do as this line to get the number of the upper hexagram

int lowerNumber = (yearNumber + monthNumber + dayNumber + timeNumber) % 8;
// 《梅花易数》：年月日数加时之数总计几数以八除之零数作下卦
// just do as this line to get the number of the lower hexagram

int changingLineIndex = (yearNumber + monthNumber + dayNumber + timeNumber) % 6;
// 《梅花易数》：就以除六数作动爻
// just do as this line to get the number of the changing line

#endregion

#region 取本卦卦画 Get the original hexagram's painting

Painting GetTrigramPainting(int innateNumber)
{
    Debug.Assert(innateNumber is >= 1 and <= 8);
    innateNumber -= 1;
    var lower = innateNumber >> 2;
    var middle = (innateNumber >> 1) - lower * 2;
    var upper = innateNumber - lower * 4 - middle * 2;
    return new Painting(
        new YinYang(lower == 0),
        new YinYang(middle == 0),
        new YinYang(upper == 0));
}
// 这是通过先天八卦数获取画卦的数学方法。
// 也可以直接查表：1->☰ 2->☱ 3->☲ 4->☳ 5->☴ 6->☵ 7->☶ 8->☷
// This is a mathematical method to get the painting through the innate number.
// You can also directly do this by the map：1->☰ 2->☱ 3->☲ 4->☳ 5->☴ 6->☵ 7->☶ 8->☷

Painting upperPainting = GetTrigramPainting(upperNumber);
Painting lowerPainting = GetTrigramPainting(lowerNumber);
// 获取上卦和下卦的卦画。
// Get the paintings of the upper and the lower trigram.

IEnumerable<YinYang> originalLines = lowerPainting.Concat(upperPainting);
Painting originalPainting = new Painting(originalLines);
// 将上卦下卦放在一起得到一个六爻卦，这就是本卦。
// Put the trigrams together to get a hexagram, which is called the original hexagram.

#endregion

#region 取变卦卦画 Get the changed hexagram's painting

Painting changedPainting = originalPainting.ChangeLines(changingLineIndex - 1);
// 这里使用了 YiJingFramework.Painting.Deriving 包提供的拓展方法，
// 把对应的爻阴阳性质改变，返回新的卦，即变卦。
// Here we used the extension method provided by the YiJingFramework.Painting.Deriving package.
// The specific line's yin-yang attribute has been changed and a new hexagram has returned, which is called as the changed hexagram.

#endregion

#region 取互卦卦画 Get the overlapped hexagram's painting

Painting overlappedPainting = originalPainting.ToOverlapping();
// 仍是 YiJingFramework.Painting.Deriving 包提供的拓展方法，
// 二三四爻作下卦，三四五爻作上卦产生新的卦，这就是互卦。
// It's also an extension method provided by the YiJingFramework.Painting.Deriving package.
// It returns a new hexagram which will be made up of --
// the second line, the third line, the fourth line,
// then the third line again, the fourth line again and the fifth line 
// -- of the original hexagram.
// This new hexagram is the so-called overlapped hexagram.

if (overlappedPainting == originalPainting)
    overlappedPainting = changedPainting.ToOverlapping();
// 《梅花易数》：乾坤无互互其变卦
// If it's the same as the original one, use the changed's instead. 

#endregion

#region 将三个卦打印出来 Print the three hexagrams

YiJingFramework.Painting.Presenting.StringConverter stringConverter =
    new("-----", "-- --", Environment.NewLine);
Console.WriteLine("本卦 THE ORIGINAL");
Console.WriteLine(stringConverter.ConvertTo(originalPainting));
Console.WriteLine();

Console.WriteLine("互卦 THE OVERLAPPED");
Console.WriteLine(stringConverter.ConvertTo(overlappedPainting));
Console.WriteLine();

Console.WriteLine("变卦 THE CHANGED");
Console.WriteLine(stringConverter.ConvertTo(changedPainting));
Console.WriteLine();

#endregion

#region 查询《周易》及其《易传》 Looking it up in Zhouyi and its Yizhuan

Zhouyi zhouyi;
using (var jingFile = new FileStream("./jing.json", FileMode.Open, FileAccess.Read))
    zhouyi = new Zhouyi(jingFile);
XiangZhuan xiang;
using (var xiangFile = new FileStream("./xiang.json", FileMode.Open, FileAccess.Read))
    xiang = new XiangZhuan(xiangFile);
// 初始化 Zhouyi 和 Xiangzhuan 。
// Initialize Zhouyi and Xiangzhuan.

ZhouyiHexagram originalHexagram = zhouyi.GetHexagram(originalPainting);
ZhouyiHexagram.Line changingLine = originalHexagram.GetLine(changingLineIndex);
ZhouyiHexagram changedHexagram = zhouyi.GetHexagram(changedPainting);
ZhouyiHexagram overlappedHexagram = zhouyi.GetHexagram(overlappedPainting);

Console.Write($"得{originalHexagram.Name}之{changedHexagram.Name}，");
// Console.Write($"It's {originalHexagram.Name} changing to {changedHexagram.Name}, ");

ZhouyiTrigram overlappedUpper = overlappedHexagram.UpperTrigram;
ZhouyiTrigram overlappedLower = overlappedHexagram.UpperTrigram;
if (overlappedUpper == overlappedLower)
{
    Console.WriteLine($"互{overlappedUpper.Name}{overlappedUpper.Name}。");
    // Console.Write($"and {overlappedUpper.Name} with {overlappedLower.Name} as the overlapped.");
}
else
{
    Console.WriteLine($"互重{overlappedUpper.Name}。");
    // Console.Write($"and doubled {overlappedUpper.Name} as the overlapped.");
}

Console.WriteLine($"易曰：{changingLine.LineText}");
// Console.WriteLine($"Zhouyi: {changingLine.LineText}");
Console.WriteLine($"象曰：{xiang[changingLine]}");
// Console.WriteLine($"And xiang: {xiang[changingLine]}");
#endregion