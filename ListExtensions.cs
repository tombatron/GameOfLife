namespace GameOfLife;

public static class ListExtensions
{
    public static void Map<T>(this List<T> list, Action<T> action)
    {
        foreach(var item in list)
        {
            action(item);
        }
    }
}