# Alterna Coding Assessment

This project created for coding assessment to the candidates who apply for a job.

## Requirement

We are waiting from you to develop robust system described below; 

- We assume that you have 1 entity namely Invitee which is added to Entities folder in the project. Implement DbContext for Sqlite with EF Core 8.x.x or 9.0.0.
- Add seed for this context random 20 invitee with the code first strategy. 
- Sqlite database must be automatically created and migrated when run application.
- Create a new MailService which implements IMailService interface according to link described below this document.
- Implement two hosted services for sending and checking status of the mails. Sending mail hosted service should work every minute. Checking status hosted service should work every 3 minutes.
- All the exceptions must be handled successfully and logged. Logging can do with Serilog to a file.
- Adding comments to necessary logical code blocks well deserved.

**Notes**
- Mail service which is given below have Rate Limiting and throw randomly bad request. You must think about resilience of the MailService which implemented. These behaviors should not break your application.
- Mail service that will integrate has a jwt token authorization. You should take into consideration when develop this mail service.
- All the DI(Dependency Injection) operations in Program.cs up to you.
- API Credentials for the Mail Service will be given

---
> ### Changes must sent as a Pull Request.
---

### [Mail Service Address](https://coding-assessment.alternacx.com)