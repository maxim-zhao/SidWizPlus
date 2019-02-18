namespace LibSidWiz.Triggers
{
    /// <summary>
    /// Finds the widest positive+negative wave in the range
    /// This can get confused by volume changes on SN76489 noise, which it will perceive as a wide wave
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    internal class WidestWaveTrigger : ITriggerAlgorithm
    {
        public int GetTriggerPoint(Channel channel, int startIndex, int endIndex, int previousIndex)
        {
            int bestOffset = startIndex; // Default to left if no waves found
            int lastCrossingPoint = endIndex;
            float previousSample = channel.GetSample(startIndex);
            int bestLength = 0;

            // For each sample...
            for (int i = startIndex + 1; i < endIndex; ++i)
            {
                var sample = channel.GetSample(i);
                if (sample > 0 && previousSample <= 0)
                {
                    // Positive edge
                    int length = i - lastCrossingPoint;
                    if (length > bestLength)
                    {
                        bestLength = length;
                        bestOffset = lastCrossingPoint;
                    }

                    lastCrossingPoint = i;
                }

                previousSample = sample;
            }

            return bestOffset;
        }
    }
}