namespace AssetManager.Models
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Approval = "Approval";
        public const string Issuer = "Issuer";
        public const string Viewer = "Viewer";
    }

    public static class RequestStatuses
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Cancelled = "Cancelled";
    }

    public static class RequestTypes
    {
        public const string Request = "Request";
        public const string Return = "Return";
        public const string Issue = "Issue";
    }
}