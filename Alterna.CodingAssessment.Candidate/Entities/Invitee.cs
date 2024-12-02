namespace Alterna.CodingAssessment.Candidate.Entities;

public class Invitee
{
    public long Id { get; set; }
    public string Email { get; set; }
    public long MailId { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsEmailSent { get; set; } // To track email status
}