public class UserSummaryDto
{
    public string? Username { get; set; }
    public double AverageRating { get; set; }
    public int TotalPlayTime { get; set; }
    public string? MostPlayedGame { get; set; }
    public List<CommentDto> Comments { get; set; } = new();
}

public class CommentDto
{
    public string? GameName { get; set; }
    public string? Content { get; set; }
}
