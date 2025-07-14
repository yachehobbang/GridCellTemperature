// See https://aka.ms/new-console-template for more information

using SimulationLogicTest;
using Verse;
using System.Text;

const int size = 20;
var size3 = new IntVec3(size, 0, size);

var centerCell = new IntVec3(size3.x / 2, 0, size3.z / 2);
//var centerCell = new IntVec3(size / 2, 0, 0);
List<(SimulationLogic, StringBuilder)> logics = [];
for (var k = 1; k <= 5; k++)
{
    var logic = new SimulationLogic(size3, k);
    logics.Add((logic, new StringBuilder()));
}

// foreach (var valueTuple in logics)
// {
//     const int loopCount = 30;
//     valueTuple.Item1.PushHeat(centerCell, 1000);
//     for (var i = 0; i < loopCount; i++)
//     {
//         //valueTuple.Item1.PushHeat(centerCell, 40);
//         valueTuple.Item1.Tick();
//
//         valueTuple.Item2.AppendLine(valueTuple.Item1.ToString());
//         valueTuple.Item2.AppendLine($"{i,6}: {valueTuple.Item1.CurrentTemperatures.Average(),6:F2}, {valueTuple.Item1.CurrentTemperatures.Max(),6:F2}");
//         valueTuple.Item2.AppendLine();
//     }
// }

foreach (var valueTuple in logics)
{
    const int loopCount = 300;
    valueTuple.Item1.PushHeat(centerCell, 1000);
    for (var i = 0; i < loopCount; i++)
    {
        //valueTuple.Item1.PushHeat(centerCell, 40);

        valueTuple.Item1.Tick();

        if (i == loopCount - 1 || i % 10 == 0)
        {
            var avg = valueTuple.Item1.CurrentTemperatures.Average();
            valueTuple.Item2.AppendLine($"{i,6}: {avg,6:F2}, {valueTuple.Item1.CurrentTemperatures.Max(),6:F2}");
        }
    }

    valueTuple.Item2.AppendLine();
    valueTuple.Item2.AppendLine(valueTuple.Item1.ToString());
}

var sb = new StringBuilder();

var lines = logics[0].Item2.ToString().Split(Environment.NewLine);
for (var i = 0; i < lines.Length - 1; i++)
{
    foreach (var (_, stringBuilder) in logics)
    {
        var currentLines = stringBuilder.ToString().Split(Environment.NewLine);
        sb.Append(currentLines[i]);
        sb.Append("        ");
    }
    sb.AppendLine();
}

File.WriteAllText("output.log", sb.ToString());