using System.Collections.ObjectModel;

namespace GameOfLife;

public record CellResult(CellCoordinate CellLocation, bool IsAlive);

public class World
{
    public int MaxX { get; private set; }
    public int MaxY { get; private set; }

    public Cell[,] Cells { get; private set; }
    private readonly List<Cell> _allCells;

    public World() : this(50, 50, true) { }

    public World(int maxX, int maxY, bool autoGenCells)
    {
        MaxX = maxX;
        MaxY = maxY;

        // _cells = new Cell[size.X, size.Y];
        Cells = new Cell[maxX, maxY];

        _allCells = new List<Cell>(maxX * maxY);

        if (autoGenCells)
        {
            InitializeWorldCells();
        }
    }

    private ReadOnlyCollection<CellCoordinate>? _allCoordinates;

    public ReadOnlyCollection<CellCoordinate> AllCoordinates
    {
        get
        {
            if (_allCoordinates == null)
            {
                var allCoordinates = new List<CellCoordinate>(MaxX * MaxY);

                for (var x = 0; x < MaxX; x++)
                {
                    for (var y = 0; y < MaxY; y++)
                    {
                        allCoordinates.Add(new CellCoordinate(x, y));
                    }
                }

                _allCoordinates = new ReadOnlyCollection<CellCoordinate>(allCoordinates);
            }

            return _allCoordinates;
        }
    }

    private bool _firstPass = true;

    public IEnumerable<CellResult> Step()
    {
        _allCells.Map(x => x.Step());

        var cells = _firstPass ? _allCells : _allCells.Where(x => x.Next() != x.IsAlive);

        foreach (var cell in cells)
        {
            yield return new CellResult(cell.CellLocation, cell.Next());
        }

        _firstPass = false;
    }

    private void InitializeWorldCells()
    {
        var seed = (int)DateTime.Now.Ticks & 0x0000FFFF;
        var random = new Random(seed);

        foreach (var coordinate in AllCoordinates)
        {
            var isAlive = random.Next(10_000) % 3 == 0;

            var cell = new Cell(coordinate, this, isAlive);

            _allCells.Add(cell);
            Cells[coordinate.X, coordinate.Y] = cell;
        }
    }
}