namespace AcxiomCRM.Authorization
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string SalesExecutive = "SalesExecutive";

        public static readonly string[] AllRoles = new[] { Admin, Manager, SalesExecutive };
    }
}
