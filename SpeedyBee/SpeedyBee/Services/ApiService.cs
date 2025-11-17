using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpeedyBee.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string BASE_URL = "http://localhost:8000"; // FastAPI default port

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BASE_URL),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        // Save a run to PostgreSQL
        public async Task<SaveRunResponse?> SaveRunAsync(string runName, List<FrameData> frames)
        {
            try
            {
                var request = new SaveRunRequest
                {
                    Name = runName,
                    Frames = frames
                };

                var response = await _httpClient.PostAsJsonAsync("/api/runs", request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<SaveRunResponse>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save run: {ex.Message}", ex);
            }
        }

        // Get all runs (for search/list)
        public async Task<List<RunSummary>> GetRunsAsync(string? searchName = null)
        {
            try
            {
                var url = string.IsNullOrEmpty(searchName) 
                    ? "/api/runs" 
                    : $"/api/runs?search={Uri.EscapeDataString(searchName)}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<RunSummary>>() ?? new List<RunSummary>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get runs: {ex.Message}", ex);
            }
        }

        // Get a specific run with all its frames
        public async Task<RunDetails?> GetRunByIdAsync(int runId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/runs/{runId}");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<RunDetails>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get run details: {ex.Message}", ex);
            }
        }

        // Delete a run
        public async Task DeleteRunAsync(int runId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/runs/{runId}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete run: {ex.Message}", ex);
            }
        }
    }

    // Request/Response Models
    public class SaveRunRequest
    {
        public string Name { get; set; } = string.Empty;
        public List<FrameData> Frames { get; set; } = new();
    }

    public class SaveRunResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int FrameCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RunSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int FrameCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RunDetails
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<FrameData> Frames { get; set; } = new();
    }

    public class FrameData
    {
        public int AccelX { get; set; }
        public int AccelY { get; set; }
        public int AccelZ { get; set; }
        public int GyroX { get; set; }
        public int GyroY { get; set; }
        public int GyroZ { get; set; }
        public int FrameNumber { get; set; }
    }
}
