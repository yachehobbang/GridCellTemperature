using SimulationLogicTest;
using System.Text;

// Read grid dimensions from first line (width and height) and grid data from subsequent lines

// var lines = File.ReadAllLines("Grid1.txt");
// var width = lines[0].Split(' ').Count();
// var height = lines.Length - 1;
// var dimensions = new[] { width, height };
// var grid = lines
//     .SelectMany(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
//     .Select(c => c == "1")
//     .ToArray();
//(int x, int y) size = (dimensions[0], dimensions[1]);

(int x, int y) size = (20, 20);
bool[]? grid = null;

(int x, int y) centerCell = (size.x / 2, size.y / 2);
List<(SimulationLogic, StringBuilder)> logics = [];
for (var k = 1; k <= 5; k++)
{
    var logic = new SimulationLogic(grid, size, k);
    logics.Add((logic, new StringBuilder()));
}

foreach (var valueTuple in logics)
{
    const int loopCount = 1000;
    //valueTuple.Item1.PushHeat(centerCell, 1000);
    for (var i = 0; i < loopCount; i++)
    {
        if (i % 4 == 0)
        {
            valueTuple.Item1.PushHeat(centerCell, 80);
        }

        valueTuple.Item1.Tick();
        
        valueTuple.Item1.WriteGridYaml(valueTuple.Item2);
    }
}

for (var index = 0; index < logics.Count; index++)
{
    var valueTuple = logics[index];
    
    File.WriteAllText($"gridOutput{index + 1}.txt", valueTuple.Item2.ToString());
}