namespace Alterna.CodingAssessment.Candidate.Models;

public class EmailRequestObject
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }

    // Constructor for easy initialization (optional)
    public EmailRequestObject(string toEmail,string subject, string body)
    {
        To = toEmail;
        Subject = subject;
        Body = body;
    }

}