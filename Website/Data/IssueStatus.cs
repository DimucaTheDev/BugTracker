namespace Website.Data
{
    public enum IssueStatus
    {
        // The following values are used in the database and should not be changed
        //TODO: Move this to a configuration file
        Open,
        Resolved,
        InProgress,
        Confirmed,
        Closed,
        AwaitingConfirmation,
        AwaitingTesting,
        Deferred,
        CannotReproduce,
        Cancelled,
        WontFix,
    }
}