namespace SidWiz.Triggers
{
    internal interface ITriggerAlgorithm
    {
        /// <summary>
        /// Finds a "trigger point" within a channel's samples
        /// </summary>
        /// <param name="channel">Channel object holding samples</param>
        /// <param name="frameIndexSamples">Index of start of frame for analysis</param>
        /// <param name="frameSamples">Length of frame for analysis</param>
        /// <returns>Index of the trigger point, relative to the start of the frame</returns>
        int GetTriggerPoint(Channel channel, int frameIndexSamples, int frameSamples);
    }
}