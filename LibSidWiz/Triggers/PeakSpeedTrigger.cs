namespace LibSidWiz.Triggers
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
    public class PeakSpeedTrigger : ITriggerAlgorithm
    {
        public int GetTriggerPoint(Channel channel, int startIndex, int endIndex)
        {
            float peakValue = float.MinValue;
            int shortestDistance = int.MaxValue;
            int triggerIndex = (startIndex + endIndex) / 2; // Default to centre if no peaks found
            int i = startIndex;
            while (i < endIndex)
            {
                // First find a positive edge crossing zero
                while (channel.GetSample(i) > 0 && i < endIndex) ++i;
                while (channel.GetSample(i) <= 0 && i < endIndex) ++i;
                // Remember this point
                int lastCrossing = i;
                // Now move forward looking for a peak
                for (var sample = channel.GetSample(i); sample > 0 && i < endIndex; ++i)
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

            return triggerIndex;
        }
    }
}