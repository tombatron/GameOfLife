namespace GameOfLife;

public class WorldCpu
{
    private readonly int _maxX;
    private readonly int _maxY;

    private int[,] _next;
    private int[,] _current;
    private bool _firstPass = true;

    public WorldCpu(int maxX, int maxY)
    {
        _maxX = maxX;
        _maxY = maxY;

        _current = _next = new int[maxX, maxY];
    }

    public IEnumerable<CellResult> Step()
    {
        ComputeNext();

        foreach (var (x, y) in WorldCoordinates)
        {
            if (IsCellChanged(x, y))
            {
                yield return new CellResult(new CellCoordinate(x, y), _next[x, y] == 1);
            }
        }
    }

    private bool IsCellChanged(int x, int y) =>
        !(_next[x, y] == _current[x, y]);

    private IEnumerable<(int x, int y)> WorldCoordinates
    {
        get
        {
            var coordinates = from x in Enumerable.Range(0, _maxX)
                              from y in Enumerable.Range(0, _maxY)
                              select (x, y);

            return coordinates;
        }
    }

    private void ComputeNext()
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 1
        };

        if (_firstPass)
        {
            var rand = new Random();

            _current = new int[_maxX, _maxY];
            _next = new int[_maxX, _maxY];

            Parallel.ForEach(WorldCoordinates, coordinates =>
            {
                var x = coordinates.x;
                var y = coordinates.y;

                _next[x, y] = rand.Next(10_000) % 3 == 0 ? 1 : 0;
                // Invert the initial value so that the entire board is rendered correctly
                // on the first pass.
                _current[x, y] = _next[x, y] == 1 ? 0 : 1;
            });

            _firstPass = false;
        }
        else
        {
            var temp = _current;

            _current = _next;
            _next = temp;

            Parallel.ForEach(WorldCoordinates, coordinates =>
            {
                var x = coordinates.x;
                var y = coordinates.y;
                var xPlusOne = x + 1 < _maxX ? x + 1 : 0;
                var xMinusOne = x - 1 >= 0 ? x - 1 : _maxX - 1;
                var yPlusOne = y + 1 < _maxY ? y + 1 : 0;
                var yMinusOne = y - 1 >= 0 ? y - 1 : _maxY - 1;

                var livingNeighborCount =
                    _current[x, yPlusOne] +
                    _current[xPlusOne, yMinusOne] +
                    _current[xPlusOne, y] +
                    _current[xMinusOne, y] +
                    _current[xPlusOne, yMinusOne] +
                    _current[xMinusOne, yPlusOne] +
                    _current[xPlusOne, yPlusOne] +
                    _current[xMinusOne, yMinusOne];

                var currentState = _current[x, y];
                var nextState = 0;

                if (currentState == 1)
                {
                    if (livingNeighborCount < 2)
                    {
                        nextState = 0;
                    }

                    if (livingNeighborCount == 2 || livingNeighborCount == 3)
                    {
                        nextState = 1;
                    }

                    if (livingNeighborCount > 3)
                    {
                        nextState = 0;
                    }
                }
                else
                {
                    if (livingNeighborCount == 3)
                    {
                        nextState = 1;
                    }
                }

                _next[x, y] = nextState;
            });
        }
    }
}
