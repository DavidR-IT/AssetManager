namespace AssetManager.Helpers
{
    public static class StringExtensions
    {
        /// <summary>
        /// Appends a note to existing text. If the combined length exceeds maxLength,
        /// the oldest content is trimmed from the start to stay within the limit.
        /// </summary>
        public static string AppendNote(this string? existing, string addition, int maxLength = 4000)
        {
            var combined = (existing ?? "") + addition;
            return combined.Length > maxLength ? combined[^maxLength..] : combined;
        }
    }
}