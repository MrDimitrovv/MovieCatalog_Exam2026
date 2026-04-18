using MovieCatalogExam.Models;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Text.Json;

namespace MovieCatalogExam
{
    [TestFixture]

    public class Tests
    {
        private RestClient client;
        private static string LastCreatedMovieId;
        private const string BaseUrl = "http://144.91.123.158:5000";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJiMGY4NjM0YS00YWRiLTQ5OTEtOWM3Zi1hOTZmMTk4NWM5NzAiLCJpYXQiOiIwNC8xOC8yMDI2IDA2OjEwOjA0IiwiVXNlcklkIjoiMTFmMDZhZmUtNTFkZi00NTI2LTYyMmItMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJESm9obnNvbkBSb2NrLmNvbSIsIlVzZXJOYW1lIjoiRHdheW5lIiwiZXhwIjoxNzc2NTE0MjA0LCJpc3MiOiJNb3ZpZUNhdGFsb2dfQXBwX1NvZnRVbmkiLCJhdWQiOiJNb3ZpZUNhdGFsb2dfV2ViQVBJX1NvZnRVbmkifQ.dcyhveIWK0fDCeEixwlzNSjqp6lfxF7J-d4Azd8RSAA";
        private const string LoginEmail = "DJohnson@Rock.com";
        private const string LoginPassword = "lift252";

        [OneTimeSetUp]

        public void Setup()
        {
            string jwtToken;
            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }
            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            this.client = new RestClient(options);
            
        }

        private string GetJwtToken(string email, string password)
        {
            var tempclient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });
            var response = tempclient.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("token").GetString();
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new Exception($"Failed to retrieve JWT token. Status Code: {response.StatusCode}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateMovie_WithValidData_ShouldReturnSuccess()
        {
            var movieRequest = new MovieDTO
            {
                Title = "Jumanji",
                Description = "New Movie called Jumanji"
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieRequest);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected Status Code 200 OK.");

            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(createResponse, Is.Not.Null, "Expected a response body deserializing to ApiResponseDTO.");
            Assert.That(createResponse.Movie, Is.Not.Null, "Expected a Movie object in the response.");
            Assert.That(createResponse.Movie.Id, Is.Not.Null.Or.Empty, "Expected returned Movie.Id to be set.");
            Assert.That(createResponse.Msg, Is.EqualTo("Movie created successfully!"));

            
            LastCreatedMovieId = createResponse.Movie.Id;
        }

        [Order(2)]
        [Test]
        public void EditExistingMovie_ShouldReturnSuccess()
        {
            var editrequestdata = new MovieDTO
            {
                Title = "Edited Title to Jumanji",
                Description = "This is an edited test Description for Jumanji",
                
            };
            
            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", LastCreatedMovieId);
            request.AddJsonBody(editrequestdata);

            var response = this.client.Execute(request);

            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected Status Code 200 OK.");
            Assert.That(editResponse.Msg, Is.EqualTo("Movie edited successfully!"));
            Assert.That(LastCreatedMovieId, Is.Not.Null.Or.Empty, "Created movie id is required.");
        }
        [Order(3)]
        [Test]
        public void GetAllMovies_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected Status Code 200 OK.");

            var moviesResponse = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);
            Assert.That(moviesResponse, Is.Not.Null, "Expected response to deserialize to a list of ApiResponseDTO.");
            Assert.That(moviesResponse, Is.Not.Empty, "Expected the returned list of movies to be non-empty.");

            // Optionally update LastCreatedMovieId from the last returned item
            var lastMovieId = moviesResponse.LastOrDefault()?.Movie?.Id;
            if (!string.IsNullOrWhiteSpace(lastMovieId))
            {
                LastCreatedMovieId = lastMovieId;
            }
        }
        [Order(4)]
        [Test]
        public void DeleteExistingIdea_ShouldReturnSuccess()
        {

            var request = new RestRequest("/api/Movie/Delete", Method.Delete);

            request.AddQueryParameter("movieId", LastCreatedMovieId);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected Status Code 200 OK.");

            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(deleteResponse, Is.Not.Null, "Expected response to deserialize to ApiResponseDTO.");
            Assert.That(deleteResponse.Msg, Is.EqualTo("Movie deleted successfully!"));
        }
        [Order(5)]
        [Test]
        public void CreateMovie_WithMissingRequiredFields_ShouldReturnBadRequest()
        {
            var movieRequest = new MovieDTO
            {
                Title = "",
                Description = "This is a failed test Description",
               
            };
            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieRequest);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected Status Code 400 Bad Request.");
        }
        [Order(6)]
        [Test]
        public void EditMovie_WithInvalidId_ShouldReturnBadRequest()
        {
            var editrequestdata = new MovieDTO
            {
                Title = "Edited Movie with bad id",
                Description = "This should fail",
                
            };
            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", "invalid-id");
            request.AddJsonBody(editrequestdata);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected Status Code 400 Bad Request.");
            var editInvalidResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(editInvalidResponse, Is.Not.Null, "Expected response to deserialize to ApiResponseDTO.");
            Assert.That(editInvalidResponse.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }
        [Order(7)]
        [Test]
        public void DeleteMovie_WithInvalidId_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", "invalid-id");
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected Status Code 400 Bad Request.");
            var deleteInvalidResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(deleteInvalidResponse, Is.Not.Null, "Expected response to deserialize to ApiResponseDTO.");
            Assert.That(deleteInvalidResponse.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));

        }
        [OneTimeTearDown]

            public void TearDown()
            {
                
                this.client?.Dispose();
            }
        
    }
}

