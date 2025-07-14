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
        
        // ลิสต์ URL ที่จะลองเชื่อมต่อ
        var urls = new[]
        {
            "http://192.168.1.107:8000/getadvice", // IP ของเครื่องจริง
            "http://10.0.2.2:8000/getadvice",      // Android Emulator
            "http://localhost:8000/getadvice",      // Local development
            "http://127.0.0.1:8000/getadvice"      // Localhost IP
        };

        var data = new { text = Diary }; // API ต้องการ key "text"
        string jsonData = JsonSerializer.Serialize(data);
        HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        Exception lastException = null;

        // ลองเชื่อมต่อกับ URL ทีละตัว
        foreach (string url in urls)
        {
            try
            {
                Console.WriteLine($"🚀 Trying URL: {url}");
                
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

                // ดึงค่าต่างๆ
                emotion = responseData["emotion"] ?? "";
                string adviceRaw = responseData["advice"] ?? "";

                // ใช้ Regex แยกค่าต่าง ๆ ออกจาก adviceRaw
                suggestion = ExtractValue(adviceRaw, "Suggestion");
                emotionalReflection = ExtractValue(adviceRaw, "Emotional Reflection");
                mood = ExtractValue(adviceRaw, "Mood");
                keywords = ExtractValue(adviceRaw, "Keywords");
                score = ExtractValue(adviceRaw, "Score");

                // แสดงผลลัพธ์
                Console.WriteLine($"✅ Success with URL: {url}");
                Console.WriteLine($"Emotion: {emotion}");
                Console.WriteLine($"Suggestion: {suggestion}");
                Console.WriteLine($"Emotional Reflection: {emotionalReflection}");
                Console.WriteLine($"Mood: {mood}");
                Console.WriteLine($"Keywords: {keywords}");
                Console.WriteLine($"Score: {score}");
                
                return; // สำเร็จแล้ว ออกจาก method
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed with {url}: {ex.Message}");
                lastException = ex;
                continue; // ลอง URL ถัดไป
            }
        }

        // ถ้าทุก URL ล้มเหลว
        throw new Exception($"Connection failure - all URLs failed. Last error: {lastException?.Message}");
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
