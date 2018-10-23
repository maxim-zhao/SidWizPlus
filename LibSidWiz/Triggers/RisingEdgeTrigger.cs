namespace LibSidWiz.Triggers
{
    /// <summary>
    /// Trigger that finds the rising edge of the wave.
    /// This is normally fine for simple waveforms but it can fall down when it sees
    /// waves which cross the centre point more than once.
    /// It also only finds the first rising edge in the sample range, rather than the
    /// one nearest the centre.
    /// </summary>
    internal class RisingEdgeTrigger : ITriggerAlgorithm
    {
        public int GetTriggerPoint(Channel channel, int startIndex, int endIndex)
        {
            // We step through the sample and select the first negative -> positive transition
            int i = startIndex;
            while (channel.GetSample(i) > 0 && i < endIndex) ++i;
            while (channel.GetSample(i) <= 0 && i < endIndex) ++i;
            if (i == endIndex)
            {
                // Failed to find anything, just stick to the middle
                i = (startIndex + endIndex) / 2;
            }

            return i;
        }
    }
}