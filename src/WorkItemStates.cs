namespace Julmar.AzDOUtilities
{
    /// <summary>
    /// States for a work item. Note that the process template
    /// decides the available states. These are here for convenience.
    /// </summary>
    public static class WorkItemStates
    {
        /// <summary>
        /// New state
        /// </summary>
        public const string New = "New";

        /// <summary>
        /// Active bug
        /// </summary>
        public const string Active = "Active";

        /// <summary>
        /// Inactive state
        /// </summary>
        public const string Inactive = "Inactive";

        /// <summary>
        /// Resolved bug
        /// </summary>
        public const string Resolved = "Resolved";

        /// <summary>
        /// Closed
        /// </summary>
        public const string Closed = "Closed";

        /// <summary>
        /// Removed work item
        /// </summary>
        public const string Removed = "Removed";

        /// <summary>
        /// Test case design
        /// </summary>
        public const string Design = "Design";

        /// <summary>
        /// Test case ready
        /// </summary>
        public const string Ready = "Ready";

        /// <summary>
        /// Test Suite in planning
        /// </summary>
        public const string InPlanning = "In Planning";

        /// <summary>
        /// Test suite in progress
        /// </summary>
        public const string InProgress = "In Progress";

        /// <summary>
        /// Completed
        /// </summary>
        public const string Completed = "Completed";
    }
}
