namespace LibSidWiz.Triggers
{
    public interface ITriggerAlgorithm
    {
        /// <summary>
        /// Finds a "trigger point" within a channel's samples
        /// </summary>
        /// <param name="channel">Channel object holding samples</param>
        /// <param name="startIndex">Index of start of frame for analysis</param>
        /// <param name="endIndex">Index of end of frame for analysis</param>
        /// <param name="previousIndex">Index of previously found trigger</param>
        /// <returns>Index of the trigger point, should be between startIndex and endIndex. Return -1 for failure.</returns>
        int GetTriggerPoint(Channel channel, int startIndex, int endIndex, int previousIndex);
    }
}