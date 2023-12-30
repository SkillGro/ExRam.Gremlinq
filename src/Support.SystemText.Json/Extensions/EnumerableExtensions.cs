namespace ExRam.Gremlinq.Support.SystemTextJson.Extensions
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<(T, T)> PairWise<T>(this IEnumerable<T> source)
        {
            T previous = default!;
            var havePrevious = false;

            foreach (var item in source)
                if (havePrevious)
                {
                    yield return (previous, item);
                    havePrevious = false;
                }
                else
                {
                    previous = item;
                    havePrevious = true;
                }
        }
    }
}
