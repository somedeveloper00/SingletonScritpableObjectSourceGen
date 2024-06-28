using System.Text;

namespace SingletonScritpableObjectSourceGen
{
    public static class CodeWritingExtensions
    {
        /// <summary>
        /// Replaces string sequences such as '$0', '$1' and '$2' etc. with the corresponding argument.
        /// </summary>
        public static StringBuilder ReplaceArguments(this StringBuilder sb, params string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                sb.Replace($"${i}", args[i]);
            }

            return sb;
        }
    }
}
