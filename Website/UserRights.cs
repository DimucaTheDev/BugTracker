namespace Website
{
    [Flags]
    public enum UserRights
    {
        None = 0,
        //PostReport = 1 << 0,
        //AttachMedia = 1 << 1,
        Comment = 1 << 2,
        DeleteUserReport = 1 << 3,
        ChangeReportStatus = 1 << 4,
        FullAdmin = 1 << 5,
    }
}
