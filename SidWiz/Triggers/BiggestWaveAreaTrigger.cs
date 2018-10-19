namespace SidWiz.Triggers
{
    /// <summary>
    /// Finds the positive half-wave with the biggest area (= sum of positive samples)
    /// </summary>
    internal class BiggestWaveAreaTrigger : ITriggerAlgorithm
    {
        public int GetTriggerPoint(Channel channel, int startIndex, int endIndex)
        {
            // We step through the sample and select the first negative -> positive transition
            int bestOffset = (startIndex + endIndex) / 2; // Default to centre if no positive waves found
            int lastCrossingPoint = -1;
            float previousSample = channel.GetSample(startIndex);
            float bestArea = 0;
            float currentArea = 0;

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
                        currentArea = 0;
                    }

                    currentArea += sample;
                }
                else
                {
                    // Negative, is it an edge?
                    if (previousSample > 0 && lastCrossingPoint > -1)
                    {
                        if (currentArea > bestArea)
                        {
                            bestOffset = lastCrossingPoint;
                            bestArea = currentArea;
                        }
                    }
                }

                previousSample = sample;
            }

            return bestOffset;
        }
    }
}