using System;

namespace LibSidWiz.Triggers
{
    /// <summary>
    /// Finds the positive+negative wave with the biggest area (= sum of absolute samples)
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    internal class BiggestWaveAreaTrigger : ITriggerAlgorithm
    {
        public int GetTriggerPoint(Channel channel, int startIndex, int endIndex, int previousIndex)
        {
            int bestOffset = (startIndex + endIndex) / 2; // Default to centre if no positive waves found
            int lastCrossingPoint = endIndex;
            float previousSample = channel.GetSample(startIndex);
            float bestArea = 0;
            float currentArea = float.MinValue;

            // For each sample...
            for (int i = startIndex + 1; i < endIndex; ++i)
            {
                // Add on the area
                var sample = channel.GetSample(i);
                currentArea += Math.Abs(sample);
                if (sample > 0 && previousSample <= 0)
                {
                    // Positive edge - check if it's a new biggest
                    if (currentArea > bestArea)
                    {
                        bestArea = currentArea;
                        bestOffset = lastCrossingPoint;
                    }

                    // And reset
                    lastCrossingPoint = i;
                    currentArea = sample;
                }

                previousSample = sample;
            }
            return bestOffset;
        }
    }
}