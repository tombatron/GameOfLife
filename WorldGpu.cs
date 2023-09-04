using ILGPU;
using ILGPU.Runtime;

namespace GameOfLife;

public sealed class WorldGpu : IDisposable
{
    private readonly int _maxX;
    private readonly int _maxY;

    private int[,] _next;
    private MemoryBuffer2D<int, Stride2D.DenseX> _deviceNext;
    private int[,] _current;
    private MemoryBuffer2D<int, Stride2D.DenseX> _deviceCurrent;
    private bool _firstPass = true;

    Context _context;
    Accelerator _accelerator;
    Action<Index2D, Index1D, Index1D, ArrayView2D<int, Stride2D.DenseX>, ArrayView2D<int, Stride2D.DenseX>> _gameProcessor;
    public WorldGpu(int maxX, int maxY)
    {
        _maxX = maxX;
        _maxY = maxY;

        _current = _next = new int[maxX, maxY];

        _context = Context.CreateDefault();

        var device = _context.GetPreferredDevice(preferCPU: true);

        _accelerator = device.CreateAccelerator(_context);

        _deviceNext = _accelerator.Allocate2DDenseX(_next);
        _deviceCurrent = _accelerator.Allocate2DDenseX(_current);

        _gameProcessor = _accelerator.LoadAutoGroupedStreamKernel<Index2D, Index1D, Index1D, ArrayView2D<int, Stride2D.DenseX>, ArrayView2D<int, Stride2D.DenseX>>(GameProcessorKernel);
    }

    static void GameProcessorKernel(Index2D coords, Index1D maxX, Index1D maxY, ArrayView2D<int, Stride2D.DenseX> input, ArrayView2D<int, Stride2D.DenseX> output)
    {
        var _current = input;
        var _next = output;

        int x = coords.X;
        int y = coords.Y;
        var xPlusOne = x + 1 < maxX ? x + 1 : 0;
        var xMinusOne = x - 1 >= 0 ? x - 1 : (int)maxX - 1;
        var yPlusOne = y + 1 < maxY ? y + 1 : 0;
        var yMinusOne = y - 1 >= 0 ? y - 1 : (int)maxY - 1;

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

            // Copy the initial board to the GPU. This will be needed on the second pass.
            _deviceCurrent.CopyFromCPU(_next);
        }
        else
        {
            // `_next` is what we just displayed. We want to copy that into the GPU memory as the
            // input parameter for the next pass.
            // _deviceCurrent.CopyFromCPU(_next);
            _gameProcessor(new Index2D(2000, 2000), _maxX, _maxY, _deviceCurrent, _deviceNext);

            _accelerator.Synchronize();

            // Copy the rendered board from the GPU to the CPU for display on the screen.
            _deviceNext.CopyToCPU(_next);
            _deviceCurrent.CopyToCPU(_current);
            _deviceCurrent.CopyFrom(_deviceNext);
        }
    }

    public void Dispose()
    {
        _deviceCurrent.Dispose();
        _deviceNext.Dispose();
        _accelerator.Dispose();
        _context.Dispose();
    }
}
