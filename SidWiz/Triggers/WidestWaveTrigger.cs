namespace SidWiz.Triggers
{
    /// <summary>
    /// Finds the widest positive half-wave in the range
    /// </summary>
    class WidestWaveTrigger : ITriggerAlgorithm
    {
        public int GetTriggerPoint(Channel channel, int startIndex, int endIndex)
        {
            // We step through the sample and select the first negative -> positive transition
            int bestOffset = (startIndex + endIndex) / 2; // Default to centre if no positive waves found
            int lastCrossingPoint = -1;
            float previousSample = channel.GetSample(startIndex);
            int bestLength = 0;

            // For each sample...
            for (int i = startIndex + 1; i < endIndex; ++i)
            {
                var sample = channel.GetSample(i);
                if (sample > 0)
                {
                    // Positive, is it an edge?
                    if (previousSample <= 0)
                    {
                        lastCrossingPoint = i;
                    }
                }
                else
                {
                    // Negative, is it an edge?
                    if (previousSample > 0 && lastCrossingPoint > -1)
                    {
                        int length = i - lastCrossingPoint;
                        if (length > bestLength)
                        {
                            bestLength = length;
                            bestOffset = lastCrossingPoint;
                        }
                    }
                }

                previousSample = sample;
            }

            return bestOffset;
        }
    }
}