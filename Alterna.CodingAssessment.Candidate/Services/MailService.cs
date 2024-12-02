using Alterna.CodingAssessment.Candidate.Contracts;
using Alterna.CodingAssessment.Candidate.Models;
using Microsoft.Extensions.Options;
using System.Net.Http;
using Polly;
using Polly.CircuitBreaker;
using System.Text.Json;
using System.Text;
using Polly.Registry;
using System.Net.Http.Headers;
using Bogus.Bson;
using System.Net;
using System;
using Alterna.CodingAssessment.Candidate.Entities;
using Microsoft.EntityFrameworkCore;
using Polly.Retry;
using Newtonsoft.Json;

namespace Alterna.CodingAssessment.Candidate.Services
{
    public class MailService : IMailService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MailService> _logger;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        private readonly IAsyncPolicy<HttpResponseMessage> _circuitBreakerPolicy;
        private readonly ApiServicesSettingsObject _mailSettings;
        //private readonly RetryPolicy retryPolicy_;
        //private readonly CircuitBreakerPolicy circuitBreakerPolicy_;
        private readonly string _apiBaseUrl;
        private static string _jwtToken;
        private readonly string _tokenMail;
        private readonly string _tokenPassword;
        private static DateTime _tokenExpirationTime;

        public MailService(IOptions<ApiServicesSettingsObject> options, ILogger<MailService> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            _apiBaseUrl = options.Value.ApiBaseUrl;
            _tokenMail = options.Value.TokenMail;
            _tokenPassword = options.Value.TokenPassword;
            //retryPolicy_ = retryPolicy;
            //circuitBreakerPolicy_ = circuitBreakerPolicy;

            // hatalarda 3 defa deneme 
            _retryPolicy = Policy.Handle<HttpRequestException>()
                                 .OrResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                                 .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                                 (result, timeSpan, retryCount, context) =>
                                 {
                                     _logger.LogWarning($"Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {result?.Result.StatusCode}");
                                 });

            // 30 saniye içinde 5 failden sonra iş keser
            _circuitBreakerPolicy = Policy.Handle<HttpRequestException>()
                                          .OrResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                                          .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
                                            onBreak: (exception, timespan) =>
                                            {
                                                _logger.LogWarning("Circuit breaker opened due to {Exception}. Will not make calls for {Timespan}.", exception.GetType(), timespan);
                                            },
                                            onReset: () =>
                                            {
                                                _logger.LogInformation("Circuit breaker reset. The system is ready for new requests.");
                                            },
                                            onHalfOpen: () =>
                                            {
                                                _logger.LogInformation("Circuit breaker in half-open state. Testing the service.");
                                            });

        }
        public EmailResponseObject Check(long id)
        {
            EmailResponseObject responseObject = new(0,false);
            try
            {
                GetJwtTokenAsync();

                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/check/{id}")
                {
                    Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _jwtToken) }
                };

                var response =  _httpClient.Send(request);

                if (response != null )
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Failed to check mail status: {response.ReasonPhrase}");
                        return responseObject;
                    }

                    var emailResponse = response.Content.ReadAsStringAsync().Result;

                    var emailResponseObject = JsonConvert.DeserializeObject<EmailResponseObject>(emailResponse);

                    responseObject = emailResponseObject;
                }

                return responseObject;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError($"Circuit is open, cannot check mail status. Error: {ex.Message}");
                return responseObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error checking mail status: {ex.Message}");
                return responseObject;
            }
        }

        public async Task<EmailResponseObject> CheckAsync(long id)
        {
            EmailResponseObject responseObject = new(0, false);
            try
            {
                GetJwtTokenAsync();

                EmailResponseObject serviceResponse = new EmailResponseObject(id, false);

                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/check/{id}")
                {
                    Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _jwtToken) }
                };

                var response = await _retryPolicy.WrapAsync(_circuitBreakerPolicy).ExecuteAsync(() => _httpClient.SendAsync(request));

                if (response != null)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Failed to check mail status: {response.ReasonPhrase}");
                        return serviceResponse;
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var emailResponseObject = JsonConvert.DeserializeObject<EmailResponseObject>(jsonResponse);
                    responseObject = emailResponseObject;
                }

                return serviceResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Request to mail service failed: {ex.Message}");
                return responseObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}");
                return responseObject;
            }
        }

        public long Send(EmailRequestObject mail)
        {
            try
            {
                long responseId = 0;

                GetJwtTokenAsync(); 

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/api/send")
                {
                    Content = new StringContent(JsonConvert.SerializeObject(new { To = mail.To, Subject = mail.Subject, Body = mail.Body }), Encoding.UTF8, "application/json")
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);

                //var response = retryPolicy_.Wrap(circuitBreakerPolicy_).Execute(() => _httpClient.Send(request));
                var response = _httpClient.Send(request);
                if (response != null)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            _logger.LogWarning("Rate limit hit. Retrying...");
                        }
                        else
                        {
                            _logger.LogError($"Failed to send email: {response.ReasonPhrase}");
                            return -1;
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Email sent successfully.");

                        var jsonResponse = response.Content.ReadAsStringAsync().Result;
                        var emailStatus = JsonConvert.DeserializeObject<EmailResponseObject>(jsonResponse);

                        responseId = emailStatus.Id;
                    }
                }
                return responseId;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError($"Circuit is open, cannot send email. Error: {ex.Message}");
                return -1;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error sending email: {ex.Message}");
                return -1;
            }
        }

        public async Task<long> SendAsync(EmailRequestObject mail)
        {
            try
            {
                long responseId = 0;
                GetJwtTokenAsync();

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/api/send")
                {
                    Content = new StringContent(JsonConvert.SerializeObject(new { To = mail.To, Subject = mail.Subject, Body = mail.Body }), Encoding.UTF8, "application/json")
                };


                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _jwtToken);

                var response = await _retryPolicy.WrapAsync(_circuitBreakerPolicy).ExecuteAsync(() => _httpClient.SendAsync(request));
                if (response != null)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            _logger.LogWarning("Rate limit hit, retrying in 30 seconds...");
                            await Task.Delay(TimeSpan.FromSeconds(30));
                            return await SendAsync(mail);
                        }
                        else
                        {
                            _logger.LogError($"Failed to send email: {response.ReasonPhrase}");
                            return -1;
                        }
                    }
                    else
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var emailStatus = JsonConvert.DeserializeObject<EmailResponseObject>(jsonResponse);

                        responseId= emailStatus.Id;
                    }
                }
                return responseId;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Request to mail send service failed: {ex.Message}");
                return -1;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError($"Circuit breaker triggered for mail send service: {ex.Message}");
                return -1;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error while sending email: {ex.Message}");
                return -1;
            }
        }

        private string GetJwtTokenAsync()
        {
            if (_jwtToken != null && DateTime.Now < _tokenExpirationTime)
            {
                return _jwtToken;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/token")
            {
                Content = new StringContent(JsonConvert.SerializeObject(new { Email = _tokenMail, Password = _tokenPassword }), Encoding.UTF8, "application/json")
            };

            var response = _httpClient.Send(request);  

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to retrieve JWT token: {response.ReasonPhrase}");
                
            }

            var tokenResponse = response.Content.ReadAsStringAsync().Result;  // Synchronous wait
            var tokenData = JsonConvert.DeserializeObject<TokenResponse>(tokenResponse);

            if (tokenData != null)
            {
                _jwtToken = tokenData.Access_token;
                _tokenExpirationTime = DateTime.Now.AddSeconds(Convert.ToInt32(tokenData.Expires_in));
                
            }

            _logger.LogInformation("JWT token acquired and cached.");

            return _jwtToken;
        }
    }
}
