using Alterna.CodingAssessment.Candidate.Contracts;
using Alterna.CodingAssessment.Candidate.Data;
using Alterna.CodingAssessment.Candidate.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alterna.CodingAssessment.Candidate.HostedServices
{
    public class CheckMailStatusHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<CheckMailStatusHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;
        public CheckMailStatusHostedService(IServiceScopeFactory scopeFactory, ILogger<CheckMailStatusHostedService> logger)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(CheckMailStatus, cancellationToken, TimeSpan.Zero, TimeSpan.FromMinutes(3));
            return Task.CompletedTask;
        }
        private async void CheckMailStatus(object state)
        {
            var cancellationToken = (CancellationToken)state;

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
                var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();

                var invitees = await dbContext.Invitees.Where(t=>t.IsCheck == false).ToListAsync(cancellationToken);//check servisi call edilmemiş kayıtlları alıyoruz. 
                foreach (var invitee in invitees)
                {
                    try
                    {
                        var serviceResponse = await mailService.CheckAsync(invitee.MailId);
                        if (serviceResponse.Id > 0)
                        {
                            invitee.MailId = serviceResponse.Id;
                            invitee.IsEmailSent = serviceResponse.IsSent;
                            invitee.IsCheck = true;
                            dbContext.Update(invitee);
                            await dbContext.SaveChangesAsync();
                        }
                        _logger.LogInformation($"Checked status for {invitee.Email}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error check status to {invitee.Email}: {ex.Message}");
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
