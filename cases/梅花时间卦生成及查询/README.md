# 梅花时间卦生成及查询

此用例生成梅花时间卦并在《周易》查询。

This use case generates the time based hexagrams in plum blossom numerology and looks them up in Zhouyi.

## 使用的包 Used Packages

所有包都可以在 [nuget.org](https://www.nuget.org/) 找到。

All the packages could be found on [nuget.org](https://www.nuget.org/).

- lunar-csharp
- YiJingFramework.Core
- YiJingFramework.Painting.Deriving
- YiJingFramework.Painting.Presenting
- YiJingFramework.References.Zhouyi
- YiJingFramework.References.Zhouyi.Zhuan

## 代码 Codes
```csharp
using com.nlf.calendar;
using System.Diagnostics;
using YiJingFramework.Core;
using YiJingFramework.Painting.Deriving.Extensions;
using YiJingFramework.References.Zhouyi;
using YiJingFramework.References.Zhouyi.Zhuan;


DateTime dateTime = DateTime.Now;
Console.WriteLine(dateTime.ToString("yyyy/MM/dd HH:mm"));
Console.WriteLine();

#region 获取年月日时数 Get the number of year, month, day and hour

Lunar lunar = Lunar.fromDate(dateTime);
// 获取农历时间。
// Get lunar time.

Console.WriteLine(lunar.toFullString());
Console.WriteLine();

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
upperNumber = upperNumber == 0 ? 8 : upperNumber;
// 《梅花易数》：年月日共计几数以八除之以零数作上卦
// just do as this two lines to get the number of the upper hexagram

int lowerNumber = (yearNumber + monthNumber + dayNumber + timeNumber) % 8;
lowerNumber = lowerNumber == 0 ? 8 : lowerNumber;
// 《梅花易数》：年月日数加时之数总计几数以八除之零数作下卦
// just do as this two lines to get the number of the lower hexagram

int changingLineIndex = (yearNumber + monthNumber + dayNumber + timeNumber) % 6;
changingLineIndex = changingLineIndex == 0 ? 6 : changingLineIndex;
// 《梅花易数》：就以除六数作动爻
// just do as this two lines to get the number of the changing line

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

#region 取互卦卦画 Get the overlapping hexagram's painting

Painting overlappingPainting = originalPainting.ToOverlapping();
// 仍是 YiJingFramework.Painting.Deriving 包提供的拓展方法，
// 二三四爻作下卦，三四五爻作上卦产生新的卦，这就是互卦。
// It's also an extension method provided by the YiJingFramework.Painting.Deriving package.
// It returns a new hexagram which will be made up of --
// the second line, the third line, the fourth line,
// then the third line again, the fourth line again and the fifth line 
// -- of the original hexagram.
// This new hexagram is the so-called overlapping hexagram.

if (overlappingPainting == originalPainting)
    overlappingPainting = changedPainting.ToOverlapping();
// 《梅花易数》：乾坤无互互其变卦
// If the original is Qian or Kun, which does not have a overlapping hexagram, use the changed's instead. 

#endregion

#region 将三个卦打印出来 Print the three hexagrams

YiJingFramework.Painting.Presenting.StringConverter stringConverter =
    new("-----", "-- --", Environment.NewLine);
Console.WriteLine("本卦 THE ORIGINAL");
Console.WriteLine(stringConverter.ConvertTo(originalPainting));
Console.WriteLine();

Console.WriteLine("互卦 THE OVERLAPPING");
Console.WriteLine(stringConverter.ConvertTo(overlappingPainting));
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
ZhouyiHexagram overlappingHexagram = zhouyi.GetHexagram(overlappingPainting);

Console.Write($"得{originalHexagram.Name}之{changedHexagram.Name}，");
// Console.Write($"It's {originalHexagram.Name} changing to {changedHexagram.Name}, ");

ZhouyiTrigram overlappingUpper = overlappingHexagram.UpperTrigram;
ZhouyiTrigram overlappingLower = overlappingHexagram.LowerTrigram;
if (overlappingUpper == overlappingLower)
{
    Console.WriteLine($"互重{overlappingUpper.Name}。");
    // Console.Write($"and doubled {overlappingUpper.Name} as the overlapping.");
}
else
{
    Console.WriteLine($"互{overlappingUpper.Name}{overlappingLower.Name}。");
    // Console.Write($"and {overlappingUpper.Name} with {overlappingLower.Name} as the overlapping.");
}

Console.WriteLine($"易曰：{changingLine.LineText}");
// Console.WriteLine($"Zhouyi: {changingLine.LineText}");
Console.WriteLine($"象曰：{xiang[changingLine]}");
// Console.WriteLine($"And xiang: {xiang[changingLine]}");
#endregion
```

## 输出样例 Sample Output

```plain
2022/01/31 13:45

二〇二一年腊月廿九 辛丑(牛)年 辛丑(牛)月 甲申(猴)日 未(羊)时 纳音[壁上土 壁上土 泉中水 路旁土] 星期一 (除夕) 西方白虎 星宿[毕月乌](吉) 彭祖百忌[甲不开仓财物耗散 申不安床鬼祟入房] 喜神方位[艮](东北) 阳贵神方位[坤](西南) 阴贵神方位[艮](东北) 福神方位[坎](正北) 财神方位[艮](东北) 冲[(戊寅)虎] 煞[南]

本卦 THE ORIGINAL
-----
-- --
-----
-----
-- --
-----

互卦 THE OVERLAPPING
-- --
-----
-----
-----
-----
-- --

变卦 THE CHANGED
-----
-- --
-----
-- --
-- --
-----

得离之噬嗑，互兑巽。
易曰：日昃之离，不鼓缶而歌，则大耋之嗟，凶。
象曰：“日昃之离”，何可久也？
```