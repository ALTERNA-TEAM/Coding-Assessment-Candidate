using Alterna.CodingAssessment.Candidate.Contracts;
using Alterna.CodingAssessment.Candidate.Data;
using Alterna.CodingAssessment.Candidate.Models;
using Microsoft.EntityFrameworkCore;

namespace Alterna.CodingAssessment.Candidate.HostedServices
{
    public class SendMailHostedService  : IHostedService, IDisposable
    {
        private readonly ILogger<SendMailHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory; 
        private Timer _timer;   

        public SendMailHostedService(IServiceScopeFactory scopeFactory, ILogger<SendMailHostedService> logger)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(SendMail, cancellationToken, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }
        private async void SendMail(object state)
        {
            var cancellationToken = (CancellationToken)state;

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
                var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();

                var invitees = await dbContext.Invitees.ToListAsync(cancellationToken);

                foreach (var invitee in invitees)
                {
                    try
                    {
                        EmailRequestObject emailRequest = new(invitee.Email, invitee.Subject, invitee.Body);
                        var emailId = await mailService.SendAsync(emailRequest);
                        if (emailId != -1)
                        {
                            invitee.MailId = emailId;
                            dbContext.Update(invitee);
                            await dbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error sending email to {invitee.Email}: {ex.Message}");
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            _timer?.Dispose();
        }

    }
}
