using Alterna.CodingAssessment.Candidate.Models;

namespace Alterna.CodingAssessment.Candidate.Contracts;

public interface IMailService
{
    long Send(EmailRequestObject mail);
    Task<long> SendAsync(EmailRequestObject mail);
    EmailResponseObject Check(long id);
    Task<EmailResponseObject> CheckAsync(long id);
}