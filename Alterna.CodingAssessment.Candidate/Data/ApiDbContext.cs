using Alterna.CodingAssessment.Candidate.Entities;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace Alterna.CodingAssessment.Candidate.Data
{
    public class ApiDbContext: DbContext
    {
        public DbSet<Invitee> Invitees { get; set; }
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
        {
        }

    }
}
