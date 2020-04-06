namespace AzDOUtilities
{
    /// <summary>
    /// Names of standard VSTS fields.
    /// See https://docs.microsoft.com/en-us/azure/devops/boards/work-items/guidance/work-item-field?view=azure-devops
    /// </summary>
    public static class Field
    {
        public const string Id = "System.Id";
        public const string Title = "System.Title";
        public const string State = "System.State";
        public const string Tags = "System.Tags";
        public const string Description = "System.Description";
        public const string Priority = "Microsoft.VSTS.Common.Priority";
        public const string WorkItemType = "System.WorkItemType";
        public const string AssignedTo = "System.AssignedTo";
        public const string AcceptedBy = "Microsoft.VSTS.CodeReview.AcceptedBy";
        public const string StackRank = "Microsoft.VSTS.Common.StackRank";
        public const string IterationPath = "System.IterationPath";
        public const string IterationId = "System.IterationId";
        public const string AreaPath = "System.AreaPath";
        public const string ChangedDate = "System.ChangedDate";
        public const string StateChangedDate = "Microsoft.VSTS.Common.StateChangeDate";
        public const string ChangedBy = "System.ChangedBy";
        public const string ClosedBy = "Microsoft.VSTS.Common.ClosedBy";
        public const string ClosedDate = "Microsoft.VSTS.Common.ClosedDate";
        public const string AreaId = "System.AreaId";
        public const string History = "System.History";
        public const string CreatedDate = "System.CreatedDate";
        public const string CreatedBy = "System.CreatedBy";
        public const string Project = "System.TeamProject";
        public const string Revision = "System.Rev";
        public const string Reason = "System.Reason";
        public const string RelatedLinkCount = "System.RelatedLinkCount";
        public const string AttachedFileCount = "System.AttachedFileCount";
        public const string ExternalLinkCount = "System.ExternalLinkCount";
        public const string HyperLinkCount = "System.HyperLinkCount";
        public const string NodeName = "System.NodeName";
        public const string CommentCount = "System.CommentCount";
        public const string Watermark = "System.Watermark";
    }
}
