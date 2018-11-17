namespace LibSidWiz.Triggers
{
    /// <summary>
    /// Null algorithm just returns the first sample it's given
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    internal class NullTrigger: ITriggerAlgorithm
    {
        public int GetTriggerPoint(Channel channel, int startIndex, int endIndex, int previousIndex)
        {
            return startIndex;
        }
    }
}
