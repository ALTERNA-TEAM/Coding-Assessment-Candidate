using Alterna.CodingAssessment.Candidate.Contracts;
using Alterna.CodingAssessment.Candidate.Models;
using Microsoft.AspNetCore.Mvc;

namespace Alterna.CodingAssessment.Candidate.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly ILogger<EmailController> _logger;
        private readonly IMailService _mailService;
        public EmailController(ILogger<EmailController> logger, IMailService mailService)
        {
            _logger = logger;
            _mailService = mailService;
        }

        [HttpPost("send")]
        public async Task<EmailSendResponse> Send([FromBody] EmailSendRequest request)
        {
            try
            {
                EmailRequestObject emailServiceRequest = new (request.To,request.Subject,request.Body);
                long mailId = await _mailService.SendAsync(emailServiceRequest);
                
                EmailSendResponse response = new EmailSendResponse { 
                    Id=mailId
                };
                
                _logger.LogInformation($"Mail sent to {request.To} with subject: {request.Subject}");
                
                return response;
            }
            catch (Exception ex)
            {
                EmailSendResponse response = new EmailSendResponse
                {
                    Id = -1
                };
                _logger.LogInformation($"Mail send failed {ex.Message}!");
                return response;
                
            }

        }
        [HttpGet("{*mailId}")]
        public async Task<CheckMailResponse> Check( int mailId)
        {
            CheckMailResponse checkMailResponse = new CheckMailResponse();
            try
            {
                var  serviceResponse = await _mailService.CheckAsync(mailId);

                if (serviceResponse != null)
                {
                    checkMailResponse.Id = serviceResponse.Id;
                    checkMailResponse.IsSent = serviceResponse.IsSent;
                }

                return checkMailResponse;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Check Mail failed {ex.Message}!");
                return checkMailResponse;
            }

        }
    }
}
