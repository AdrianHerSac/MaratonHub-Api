namespace MaratonHub.Api.Groups.Dtos;

public class ChatMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public class SendMessageDto
{
    public string Message { get; set; } = string.Empty;
}
