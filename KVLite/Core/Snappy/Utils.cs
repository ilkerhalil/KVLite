namespace PommaLabs.KVLite.Core.Snappy
{
    internal static class Utils
    {
        public static bool BuffersEqual(byte[] left, byte[] right)
        {
            return left.Length == right.Length && BuffersEqual(left, right, left.Length);
        }

        public static bool BuffersEqual(byte[] left, byte[] right, int count)
        {
            for (var i = 0; i < count; ++i)
                if (left[i] != right[i])
                    return false;
            return true;
        }
    }
}
