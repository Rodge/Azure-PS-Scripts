namespace CompareRBACs;

public static class ListExtensions
{
    public static IEnumerable<TSource> RemoveDuplicatesBy<TSource, TKey>(this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        // Group by the keySelector, then select the first item from each group
        return source.GroupBy(keySelector).Select(x => x.First());
    }
}