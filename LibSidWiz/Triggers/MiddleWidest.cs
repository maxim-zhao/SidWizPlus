using System.Collections.Generic;

namespace LibSidWiz.Triggers
{
    /// <summary>
    /// This corresponds to SidWiz's "alternate" algorithm.
    /// We measure the width of each full wave, and then select the widest ones.
    /// We then select the start point of the "middle" one, if more than one was found.
    /// </summary>
    // ReSharper disable once UnusedType.Global
    class MiddleWidest: ITriggerAlgorithm
    {
        public int GetTriggerPoint(Channel channel, int startIndex, int endIndex, int previousIndex)
        {
            var candidates = new List<int>();
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
                        candidates.Clear();
                        bestLength = length;
                    }

                    if (length == bestLength)
                    {
                        candidates.Add(lastCrossingPoint);
                    }

                    lastCrossingPoint = i;
                }

                previousSample = sample;
            }

            if (candidates.Count == 0)
            {
                return -1;
            }

            // We select the "middle" one, preferring the one on the right if even
            return candidates[candidates.Count / 2];
        }
    }
}
