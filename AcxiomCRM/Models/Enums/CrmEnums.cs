namespace AcxiomCRM.Models.Enums
{
    public enum LeadStatus
    {
        New = 1,
        Contacted = 2,
        Qualified = 3,
        Unqualified = 4,
        Converted = 5,
        Lost = 6
    }

    public enum LeadSource
    {
        Website = 1,
        Referral = 2,
        ColdCall = 3,
        Advertisement = 4,
        Partner = 5,
        TradeShow = 6,
        Other = 7
    }

    public enum LeadPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Urgent = 4
    }

    public enum OpportunityStage
    {
        Qualification = 1,
        Proposal = 2,
        Negotiation = 3,
        Won = 4,
        Lost = 5
    }

    public enum OpportunityStatus
    {
        Open = 1,
        Won = 2,
        Lost = 3,
        Abandoned = 4
    }

    public enum FollowUpStatus
    {
        Planned = 1,
        Completed = 2,
        Missed = 3,
        Cancelled = 4
    }

    public enum FollowUpType
    {
        Call = 1,
        Meeting = 2,
        Email = 3,
        Demo = 4,
        Task = 5
    }

    public enum ActivityType
    {
        Call = 1,
        Meeting = 2,
        Email = 3,
        Task = 4
    }

    public enum ActivityStatus
    {
        Pending = 1,
        Completed = 2,
        Cancelled = 3
    }
}
