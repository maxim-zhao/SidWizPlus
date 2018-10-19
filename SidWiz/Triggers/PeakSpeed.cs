namespace SidWiz.Triggers
{
    /// <summary>
    /// Finds the positive edge which most quickly reaches the peak value in the sample range.
    /// This is implemented in a slightly complicated way to make it do it with a single pass over the samples,
    /// you could implement it as:
    /// 1. Find first zero crossing
    /// 2. Find max sample value after that
    /// 4. Select the zero crossing closest to a following max value
    /// This algorithm is based code from オップナー2608.
    /// This algorithm can show good stability for waves which cross the zero point more than once.
    /// </summary>
    class PeakSpeed : ITriggerAlgorithm
    {
        public int GetTriggerPoint(Channel channel, int frameIndexSamples, int frameSamples)
        {
            int max = frameIndexSamples + frameSamples;
            float peakValue = float.MinValue;
            int shortestDistance = int.MaxValue;
            int triggerIndex = frameIndexSamples;
            int i = frameIndexSamples;
            while (i < max)
            {
                // First find a positive edge crossing zero
                while (channel.GetSample(i) > 0 && i < max) ++i;
                while (channel.GetSample(i) <= 0 && i < max) ++i;
                // Remember this point
                int lastCrossing = i;
                // Now move forward looking for a peak
                for (var sample = channel.GetSample(i); sample > 0 && i < max; ++i)
                {
                    if (sample > peakValue)
                    {
                        // It's a new high
                        peakValue = sample;
                        triggerIndex = lastCrossing;
                        shortestDistance = i - lastCrossing;
                    }
                    else if (sample == peakValue && (i - lastCrossing) < shortestDistance)
                    {
                        // It's equal to the best peak but closer to the crossing point
                        triggerIndex = lastCrossing;
                        shortestDistance = i - lastCrossing;
                    }

                    sample = channel.GetSample(i);
                }
            }

            return triggerIndex - frameIndexSamples;
        }
    }
}