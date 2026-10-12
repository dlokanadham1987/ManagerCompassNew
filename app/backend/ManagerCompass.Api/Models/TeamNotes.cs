namespace ManagerCompass.Api.Models;

public class TeamMember
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public class Team
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<TeamMember> Members { get; set; } = new();
}

/// <summary>A manager's comment on a whole team (TargetType "team") or one member (TargetType "member").</summary>
public class TeamComment
{
    public string Id { get; set; } = "";
    public string TargetType { get; set; } = "";
    public string TargetId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TeamCommentInput
{
    public string TargetType { get; set; } = "";
    public string TargetId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}
