namespace GameOfLife;

public record CellCoordinate(int X, int Y);

public class Cell
{
    private World _world;

    public CellCoordinate CellLocation { get; private set; }

    public bool IsAlive { get; private set; }

    public Cell(CellCoordinate cellLocation, World world, bool isAlive)
    {
        CellLocation = cellLocation;
        IsAlive = isAlive;

        _world = world;
    }

    public void Step()
    {
        IsAlive = Next();
        _next = default;
    }

    private bool? _next;
    public bool Next()
    {
        if (_next is null)
        {
            _next = false;

            var liveNeighbors = Neighbors.Count(x => x.IsAlive);

            if (IsAlive)
            {
                if (liveNeighbors < 2)
                {
                    _next = false;
                }

                if (liveNeighbors == 2 || liveNeighbors == 3)
                {
                    _next = true;
                }

                if (liveNeighbors > 3)
                {
                    _next = false;
                }
            }
            else
            {
                _next = liveNeighbors == 3;
            }
        }

        return _next.Value;
    }

    private Cell[]? _neighbors;
    public Cell[] Neighbors
    {
        get
        {
            if (_neighbors == null)
            {
                var x = CellLocation.X;
                var y = CellLocation.Y;
                var xPlusOne = x + 1 < _world.MaxX ? x + 1 : 0;
                var xMinusOne = x - 1 >= 0 ? x - 1 : _world.MaxX - 1;
                var yPlusOne = y + 1 < _world.MaxY ? y + 1 : 0;
                var yMinusOne = y - 1 >= 0 ? y - 1 : _world.MaxY - 1;

                _neighbors = new[]
                {
                    _world.Cells[x, yPlusOne],
                    _world.Cells[xPlusOne, yMinusOne],
                    _world.Cells[xPlusOne, y],
                    _world.Cells[xMinusOne, y],
                    _world.Cells[xPlusOne, yMinusOne],
                    _world.Cells[xMinusOne, yPlusOne],
                    _world.Cells[xPlusOne, yPlusOne],
                    _world.Cells[xMinusOne, yMinusOne]
                };
            }

            return _neighbors;
        }
    }
}