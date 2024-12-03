namespace Alterna.CodingAssessment.Candidate.Models;

public class EmailResponseObject
{
    // Should be updated according to the service that is given swagger definition necessary
    public long Id { get; set; }      // Unique identifier for the email
    public bool IsSent { get; set; }    // Status of the email (e.g., "Sent", "Failed", "Pending")
    public EmailResponseObject(long id, bool isSent)
    {
        Id = id;
        IsSent = isSent;
    }

}