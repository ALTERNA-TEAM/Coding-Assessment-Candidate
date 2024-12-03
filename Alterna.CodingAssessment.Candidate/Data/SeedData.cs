using Alterna.CodingAssessment.Candidate.Entities;
using Bogus;
using Microsoft.EntityFrameworkCore;

namespace Alterna.CodingAssessment.Candidate.Data
{
    public class SeedData
    {
        public static async Task Initialize(ApiDbContext context, ILogger<SeedData> logger)
        {
            if (await context.Invitees.AnyAsync())
            {
                logger.LogInformation("Database already seeded with invitees.");
                return; 
            }

            var invitees = new Faker<Invitee>()
               .RuleFor(i => i.Email, f => f.Internet.Email())
               .RuleFor(i => i.Subject, f => f.Lorem.Sentence())
               .RuleFor(i => i.Body, f => f.Lorem.Paragraph())
               .RuleFor(i => i.IsEmailSent, f => f.Random.Bool())
               .Generate(20); 


            await context.Invitees.AddRangeAsync(invitees);
            await context.SaveChangesAsync();

            logger.LogInformation("Successfully seeded 20 invitees.");
        }
    }
}
