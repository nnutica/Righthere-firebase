using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Firebasemauiapp.Services;

public class API
{
    private string emotion;
    private string suggestion;
    private string emotionalReflection;
    private string mood;
    private string keywords;
    private string score;

    public async Task SendData(string Diary)
    {
        using HttpClient client = new HttpClient();

        string url = "https://nitinat-right-here.hf.space/getadvice"; // Hugging Face Spaces URL

        var data = new { text = Diary }; // API ต้องการ key "text"
        string jsonData = JsonSerializer.Serialize(data);
        HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        try
        {
            Console.WriteLine($"🚀 Connecting to: {url}");

            HttpResponseMessage response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            if (responseBody == null)
            {
                throw new Exception("Failed to parse API response");
            }

            if (!responseBody.Contains("emotion") || !responseBody.Contains("advice"))
            {
                throw new Exception("API response missing required fields (emotion, advice)");
            }

            // แปลง JSON เป็น Dictionary
            var responseData = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);

            if (responseData == null)
            {
                throw new Exception("Failed to deserialize API response");
            }

            // ดึงค่าต่างๆ
            emotion = responseData["emotion"] ?? "";
            string adviceRaw = responseData["advice"] ?? "";

            // ใช้ Regex แยกค่าต่าง ๆ ออกจาก adviceRaw
            suggestion = ExtractValue(adviceRaw, "Suggestion");
            emotionalReflection = ExtractValue(adviceRaw, "Emotional Reflection");
            mood = ExtractValue(adviceRaw, "Mood");
            keywords = ExtractValue(adviceRaw, "Keywords");
            score = ExtractValue(adviceRaw, "Sentiment Score");

            // แสดงผลลัพธ์
            Console.WriteLine($"✅ Success with URL: {url}");
            Console.WriteLine($"Emotion: {emotion}");
            Console.WriteLine($"Suggestion: {suggestion}");
            Console.WriteLine($"Emotional Reflection: {emotionalReflection}");
            Console.WriteLine($"Mood: {mood}");
            Console.WriteLine($"Keywords: {keywords}");
            Console.WriteLine($"Score: {score}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Connection failed: {ex.Message}");
            throw new Exception($"Connection failure: {ex.Message}");
        }
    }

    private string ExtractValue(string text, string label)
    {
        var match = Regex.Match(text, $@"- {label}:\s*(.+?)(?:\s*-|$)");
        return match.Success ? match.Groups[1].Value.Trim() : "N/A";
    }

    // ✅ สร้าง Getter สำหรับค่าต่างๆ
    public string GetEmotion() => emotion;
    public string GetSuggestion() => suggestion;
    public string GetEmotionalReflection() => emotionalReflection;
    public string GetMood() => mood;
    public string GetKeywords() => keywords;
    public double GetScore() => double.TryParse(score, out var result) ? result : 5.0;

    public static async Task Main(string data)
    {
        API api = new API();
        await api.SendData(data);

        // ✅ แสดงผลลัพธ์ที่ได้จาก Getter
        Console.WriteLine("\n--- Results ---");
        Console.WriteLine("Emotion: " + api.GetEmotion());
        Console.WriteLine("Suggestion: " + api.GetSuggestion());
        Console.WriteLine("Emotional Reflection: " + api.GetEmotionalReflection());
        Console.WriteLine("Mood: " + api.GetMood());
        Console.WriteLine("Keywords: " + api.GetKeywords());
        Console.WriteLine("Score: " + api.GetScore());
    }
}
