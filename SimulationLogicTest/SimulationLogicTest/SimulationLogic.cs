using System.Text;
using Verse;

namespace SimulationLogicTest;

public class SimulationLogic(bool[]? grid, (int x, int y) size, int baseHeatTransferCoefficient)
{
    private readonly double[] _temperatures1 = new double[size.x * size.y];

    private readonly double[] _temperatures2 = new double[size.x * size.y];

    private int _currentIndex;

    private readonly double[] _pushHeatGrid = new double[size.x * size.y];

    public double[] CurrentTemperatures
    {
        get
        {
            if (_currentIndex == 0)
            {
                return _temperatures1;
            }
            else
            {
                return _temperatures2;
            }
        }
    }

    private double[] NextTemperatures
    {
        get
        {
            if (_currentIndex == 0)
            {
                return _temperatures2;
            }
            else
            {
                return _temperatures1;
            }
        }
    }

    public void PushHeat((int x, int y) cell, double energy)
    {
        var index = CellToIndex(cell.x, cell.y);    
        if (index == null)
        {
            return;
        }

        _pushHeatGrid[index.Value] += energy / (baseHeatTransferCoefficient * baseHeatTransferCoefficient);
    }

    public void Tick()
    {
        var prev = CurrentTemperatures;
        var next = NextTemperatures;
        const int outdoorTemperature = 0;

        Parallel.For(0, _pushHeatGrid.Length,
            i =>
            {
                var heat = _pushHeatGrid[i];
                if (heat == 0)
                {
                    return;
                }

                _pushHeatGrid[i] = 0;

                prev[i] += heat;
            });

        Parallel.For(0, prev.Length,
            i =>
            {
                var x = i % size.x;
                var y = i / size.x;

                var temperature = NextCellTemperature(x, y, prev, true, outdoorTemperature);
                next[i] = temperature;
            });

        _currentIndex = (_currentIndex + 1) % 2;
    }

    private unsafe double NextCellTemperature(int x, int y,
        double[] tempGrid, bool isRoof, double outdoorTemperature)
    {
        var rawIndex = CellToIndex(x, y);

        if (!rawIndex.HasValue)
        {
            return outdoorTemperature;
        }

        var index = rawIndex.Value;
        var cellTemperature = tempGrid[index];

        var cellIsWall = grid != null && grid[index];

        var temperatures = stackalloc double[4];
        var weights = stackalloc double[4];
        (temperatures[0], weights[0]) = GetCellInfo(x + 1, y, cellIsWall, tempGrid, outdoorTemperature);
        (temperatures[1], weights[1]) = GetCellInfo(x, y + 1, cellIsWall, tempGrid, outdoorTemperature);
        (temperatures[2], weights[2]) = GetCellInfo(x - 1, y, cellIsWall, tempGrid, outdoorTemperature);
        (temperatures[3], weights[3]) = GetCellInfo(x, y - 1, cellIsWall, tempGrid, outdoorTemperature);

        var cellEnergy = cellTemperature - outdoorTemperature;
        var cellEnergySign = Math.Sign(cellEnergy);
        var cellEnergyValue = Math.Abs(cellEnergy);
        for (var c = 1; c < baseHeatTransferCoefficient; c++)
        {
            cellEnergyValue *= cellEnergyValue;
        }

        cellEnergyValue *= cellEnergySign;

        var sum = 0.0;
        for (var i = 0; i < 4; i++)
        {
            var weight = weights[i];
            
            var targetCellEnergy = temperatures[i] - outdoorTemperature;
            var targetCellEnergySign = Math.Sign(targetCellEnergy);
            var targetCellEnergyValue = Math.Abs(targetCellEnergy);
            for (var c = 1; c < baseHeatTransferCoefficient; c++)
            {
                targetCellEnergyValue *= targetCellEnergyValue;

                weight *= weights[i];
            }

            targetCellEnergyValue *= targetCellEnergySign;

            var d = (targetCellEnergyValue - cellEnergyValue) * weight;
            sum += d;
        }

        var newValue = cellEnergyValue + sum * 0.2;
        var resultSign = Math.Sign(newValue);
        newValue = Math.Abs(newValue);

        for (var c = 1; c < baseHeatTransferCoefficient; c++)
        {
            newValue = Math.Sqrt(newValue);
        }

        if (!isRoof)
        {
            newValue *= 0.5;
        }
        else
        {
            newValue *= 0.998;
        }

        var result = newValue * resultSign + outdoorTemperature;
        return result;
    }

    private (double temperature, double weight) GetCellInfo(int x, int y,
        bool baseCellIsWall,
        double[] tempGrid,
        double defaultTemperature)
    {
        var index = CellToIndex(x, y);
        double temperature;
        bool isWall;
        if (!index.HasValue)
        {
            temperature = defaultTemperature;
            isWall = true;
        }
        else
        {
            temperature = tempGrid[index.Value];
            isWall = grid != null && grid[index.Value];
        }

        if (baseCellIsWall == isWall)
        {
            if (baseCellIsWall)
            {
                return (temperature, 0.1);
            }
            else
            {
                return (temperature, 1);
            }
        }
        else
        {
            return (temperature, 0.1);
        }
    }

    private int? CellToIndex(int x, int y)
    {
        if (x < 0 || y < 0 || x >= size.x || y >= size.y)
        {
            return null;
        }

        return CellIndicesUtility.CellToIndex(x, y, size.x);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        var temperatures = CurrentTemperatures;

        for (int z = size.y - 1; z >= 0; z--)
        {
            for (int x = 0; x < size.x; x++)
            {
                var index = CellToIndex(x, z);
                if (index.HasValue)
                {
                    sb.Append($"{temperatures[index.Value],6:F1} ");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
    
    public void WriteGridYaml(StringBuilder sb)
    {
        var data = CurrentTemperatures;
        var width = size.x;
        var height = size.y;
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Length != width * height)
            throw new ArgumentException("data.Length must equal width*height");

        sb.AppendLine("-");  // 최상위 시퀀스 시작

        for (var y = 0; y < height; y++)
        {
            sb.Append("  - [");
            for (var x = 0; x < width; x++)
            {
                var v = data[y * width + x];
                // 소수점 자리수를 필요에 따라 조절할 수 있습니다.
                sb.Append(v.ToString("G"));
                if (x < width - 1)
                    sb.Append(", ");
            }
            sb.AppendLine("]");
        }
    }
}