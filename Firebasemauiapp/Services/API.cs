using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Firebasemauiapp.Services;

public class API
{
    private string suggestion = string.Empty;
    private string emotionalReflection = string.Empty;
    private string keywords = string.Empty;

    public async Task SendData(string Diary, string? moodData = null)
    {
        using HttpClient client = new HttpClient();

        string url = "https://nitinat-right-here.hf.space/getadvice"; // Hugging Face Spaces URL

        // Build request data - combine diary with mood data if provided
        string requestText = Diary;
        if (!string.IsNullOrEmpty(moodData))
        {
            requestText = $"{Diary}\n\n{moodData}";
        }

        var requestData = new { text = requestText };
        string jsonData = JsonSerializer.Serialize(requestData);
        HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        try
        {
            Console.WriteLine($"🚀 Connecting to: {url}");

            HttpResponseMessage response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                throw new Exception("Failed to parse API response");
            }

            Console.WriteLine($"📦 Raw Response: {responseBody}");

            // Parse new format with "result" wrapper
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseBody, options);

            if (apiResponse?.Result == null)
            {
                throw new Exception("API response missing 'result' field");
            }

            // ดึงค่าจาก result object
            suggestion = apiResponse.Result.suggestion ?? "";
            emotionalReflection = apiResponse.Result.reflection_message ?? "";

            // Convert keywords array to comma-separated string
            if (apiResponse.Result.Keywords != null && apiResponse.Result.Keywords.Length > 0)
            {
                keywords = string.Join(", ", apiResponse.Result.Keywords);
            }
            else
            {
                keywords = "";
            }

            // แสดงผลลัพธ์
            Console.WriteLine($"✅ Success with URL: {url}");
            Console.WriteLine($"Suggestion: {suggestion}");
            Console.WriteLine($"Emotional Reflection: {emotionalReflection}");
            Console.WriteLine($"Keywords: {keywords}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Connection failed: {ex.Message}");
            throw new Exception($"Connection failure: {ex.Message}");
        }
    }

    // ✅ Getters สำหรับค่าต่างๆ
    public string GetSuggestion() => suggestion;
    public string GetEmotionalReflection() => emotionalReflection;
    public string GetKeywords() => keywords;
}

// Response models for new API format
public class ApiResponse
{
    [JsonPropertyName("result")]
    public ResultData? Result { get; set; }

    [JsonPropertyName("raw_model_output")]
    public string? RawModelOutput { get; set; }
}

public class ResultData
{
    [JsonPropertyName("suggestion")]
    public string? Suggestion { get; set; }

    [JsonPropertyName("emotional_reflection")]
    public string? EmotionalReflection { get; set; }

    [JsonPropertyName("keywords")]
    public string[]? Keywords { get; set; }

    [JsonPropertyName("consistency_check")]
    public string? ConsistencyCheck { get; set; }

    [JsonPropertyName("safety_note")]
    public string? SafetyNote { get; set; }
}
