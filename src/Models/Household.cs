using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace WonDay.Models;

[Table("households")]
public class Household : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "우리집";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

[Table("household_members")]
public class HouseholdMember : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("household_id")]
    public Guid HouseholdId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    // "owner" 또는 "member"
    [Column("role")]
    public string Role { get; set; } = "member";

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; }
}

[Table("household_invites")]
public class HouseholdInvite : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("household_id")]
    public Guid HouseholdId { get; set; }

    [Column("invited_email")]
    public string InvitedEmail { get; set; } = string.Empty;

    [Column("invited_by")]
    public Guid InvitedBy { get; set; }

    // "pending" / "accepted" / "declined"
    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
