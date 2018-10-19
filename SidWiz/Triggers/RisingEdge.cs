namespace SidWiz.Triggers
{
    /// <summary>
    /// Trigger that finds the rising edge of the wave. This is normally fine for simple waveforms but it can fall down when it sees
    /// waves which cross the centre point more than once.
    /// </summary>
    internal class RisingEdge : ITriggerAlgorithm
    {
        public int GetTriggerPoint(Channel channel, int frameIndexSamples, int frameSamples)
        {
            // We step through the sample and select the first negative -> positive transition
            int frameTriggerOffset = 0;
            while (channel.GetSample(frameIndexSamples + frameTriggerOffset) > 0 && frameTriggerOffset < frameSamples) frameTriggerOffset++;
            while (channel.GetSample(frameIndexSamples + frameTriggerOffset) <= 0 && frameTriggerOffset < frameSamples) frameTriggerOffset++;
            if (frameTriggerOffset == frameSamples)
            {
                // Failed to find anything, just stick to the middle
                frameTriggerOffset = frameSamples / 2;
            }

            return frameTriggerOffset;
        }
    }
}