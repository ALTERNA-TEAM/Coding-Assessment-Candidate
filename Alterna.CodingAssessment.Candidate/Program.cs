using Alterna.CodingAssessment.Candidate.Contracts;
using Alterna.CodingAssessment.Candidate.Data;
using Alterna.CodingAssessment.Candidate.Entities;
using Alterna.CodingAssessment.Candidate.HostedServices;
using Alterna.CodingAssessment.Candidate.Models;
using Alterna.CodingAssessment.Candidate.Services;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Polly;
using Serilog;
using System;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddSingleton(Log.Logger);

builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddScoped<IMailService, MailService>();

builder.Services.AddSingleton<IHostedService, SendMailHostedService>();
builder.Services.AddSingleton<IHostedService, CheckMailStatusHostedService>();


builder.Services.AddHttpClient<IMailService, MailService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["MailService:ApiBaseUrl"]);
});

builder.Services.AddRazorPages();

builder.Services.Configure<ApiServicesSettingsObject>(builder.Configuration.GetSection("MailService"));

builder.Services.AddSingleton<IAsyncPolicy<HttpResponseMessage>>(Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .RetryAsync(3));

builder.Services.AddSingleton<IAsyncPolicy<HttpResponseMessage>>(Policy
    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .RetryAsync(3));

// Add services to the container
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//Auto migrate
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
    dbContext.Database.Migrate();
}

//Random 20 value 
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedData>>();

    await SeedData.Initialize(dbContext, logger);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
