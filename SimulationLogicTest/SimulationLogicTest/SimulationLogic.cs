using Verse;

namespace SimulationLogicTest;

public class SimulationLogic(IntVec3 size, int baseHeatTransferCoefficient)
{
    private readonly double[] _temperatures1 = new double[size.x * size.z];

    private readonly double[] _temperatures2 = new double[size.x * size.z];

    private int _currentIndex;

    private readonly double[] _pushHeatGrid = new double[size.x * size.z];

    private readonly int[] _damperCountGrid1 = new int[size.x * size.z];
    private readonly int[] _damperCountGrid2 = new int[size.x * size.z];

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

    public int[] CurrentDamperGrid
    {
        get
        {
            if (_currentIndex == 0)
            {
                return _damperCountGrid1;
            }
            else
            {
                return _damperCountGrid2;
            }
        }
    }

    private int[] NextDamperGrid
    {
        get
        {
            if (_currentIndex == 0)
            {
                return _damperCountGrid2;
            }
            else
            {
                return _damperCountGrid1;
            }
        }
    }

    public void PushHeat(IntVec3 cell, double energy)
    {
        var index = CellToIndex(cell.x, cell.z);
        if (index == null)
        {
            return;
        }

        _pushHeatGrid[index.Value] += energy / (baseHeatTransferCoefficient);
    }

    public void Tick()
    {
        var prev = CurrentTemperatures;
        var next = NextTemperatures;
        var currentDamperGrid = CurrentDamperGrid;
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
                currentDamperGrid[i] = 20;
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

        var cellIsWall = false;

        var temperatures = stackalloc double[4];
        var weights = stackalloc double[4];
        var damperCounts = stackalloc int[4];
        (temperatures[0], weights[0], damperCounts[0]) = GetCellInfo(x + 1, y, cellIsWall, tempGrid, outdoorTemperature);
        (temperatures[1], weights[1], damperCounts[1]) = GetCellInfo(x, y + 1, cellIsWall, tempGrid, outdoorTemperature);
        (temperatures[2], weights[2], damperCounts[2]) = GetCellInfo(x - 1, y, cellIsWall, tempGrid, outdoorTemperature);
        (temperatures[3], weights[3], damperCounts[3]) = GetCellInfo(x, y - 1, cellIsWall, tempGrid, outdoorTemperature);

        var cellEnergy = cellTemperature - outdoorTemperature;
        var cellEnergySign = Math.Sign(cellEnergy);
        var cellEnergyValue = Math.Abs(cellEnergy);
        for (var c = 1; c < baseHeatTransferCoefficient; c++)
        {
            cellEnergyValue *= cellEnergyValue;
        }

        cellEnergyValue *= cellEnergySign;

        var damperCount = CurrentDamperGrid[index];
        var sum = 0.0;
        for (var i = 0; i < 4; i++)
        {
            var targetCellEnergy = temperatures[i] - outdoorTemperature;
            var targetCellEnergySign = Math.Sign(targetCellEnergy);
            var targetCellEnergyValue = Math.Abs(targetCellEnergy);
            for (var c = 1; c < baseHeatTransferCoefficient; c++)
            {
                targetCellEnergyValue *= targetCellEnergyValue;
            }

            targetCellEnergyValue *= targetCellEnergySign;

            var d = (targetCellEnergyValue - cellEnergyValue) * weights[i];
            sum += d;

            damperCount = Math.Max(damperCount, damperCounts[i]);
        }

        var newValue = cellEnergyValue + sum * 0.2;
        var resultSign = Math.Sign(newValue);
        newValue = Math.Abs(newValue);

        if (damperCount > 0)
        {
            switch (baseHeatTransferCoefficient)
            {
                case 2:
                    //newValue *= 0.999;
                    break;
                case 3:
                    newValue *= 0.98;
                    break;
                case 4:
                    newValue *= 0.86;
                    break;
                case 5:
                    newValue *= 0.55;
                    break;
            }

            NextDamperGrid[index] = damperCount - 1;
        }
        else
        {
            NextDamperGrid[index] = 0;
        }

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
            newValue *= 0.99;
        }

        var result = newValue * resultSign + outdoorTemperature;
        return result;
    }

    private (double temperature, double weight, int damperCount) GetCellInfo(int x, int y,
        bool baseCellIsWall,
        double[] tempGrid,
        double defaultTemperature)
    {
        var index = CellToIndex(x, y);
        double temperature;
        bool isWall;
        var damperCount = 0;
        if (!index.HasValue)
        {
            temperature = defaultTemperature;
            isWall = true;
        }
        else
        {
            temperature = tempGrid[index.Value];
            damperCount = CurrentDamperGrid[index.Value];
            isWall = false;
        }

        if (baseCellIsWall == isWall)
        {
            if (baseCellIsWall)
            {
                return (temperature, 0, damperCount);
            }
            else
            {
                return (temperature, 1, damperCount);
            }
        }
        else
        {
            return (temperature, 0, damperCount);
        }
    }

    private int? CellToIndex(int x, int y)
    {
        if (x < 0 || y < 0 || x >= size.x || y >= size.z)
        {
            return null;
        }

        return CellIndicesUtility.CellToIndex(x, y, size.x);
    }

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        var temperatures = CurrentTemperatures;

        for (int z = size.z - 1; z >= 0; z--)
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
}