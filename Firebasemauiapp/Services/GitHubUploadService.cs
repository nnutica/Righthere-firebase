using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Firebasemauiapp.Services;

public class GitHubUploadService
{
    private readonly HttpClient _http;

    public GitHubUploadService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RighthereApp", "1.0"));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string originalFileName)
    {
        var owner = Preferences.Default.Get("GitHubOwner", string.Empty);
        var repo = Preferences.Default.Get("GitHubRepo", string.Empty);
        var branch = Preferences.Default.Get("GitHubBranch", "main");
        var pathPrefix = Preferences.Default.Get("GitHubPathPrefix", "uploads");
        var token = Preferences.Default.Get("GitHubToken", string.Empty);

        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo) || string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("GitHub settings not configured. Set GitHubOwner, GitHubRepo, GitHubToken in app preferences.");

        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var path = string.IsNullOrWhiteSpace(pathPrefix) ? fileName : $"{pathPrefix.Trim('/')}/{fileName}";

        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        var base64 = Convert.ToBase64String(ms.ToArray());

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Preflight: verify token can access the repo and has permissions
        var repoUrl = $"https://api.github.com/repos/{owner}/{repo}";
        var probe = await _http.GetAsync(repoUrl);
        if (probe.StatusCode == System.Net.HttpStatusCode.Forbidden || probe.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var details = await probe.Content.ReadAsStringAsync();
            throw new Exception($"Token cannot access repo {owner}/{repo}. Check token type/scopes or SSO. Status {(int)probe.StatusCode}: {details}");
        }

        var url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}";
        var payload = new
        {
            message = $"Add diary image {fileName}",
            content = base64,
            branch = branch
        };
        var json = JsonSerializer.Serialize(payload);
        var resp = await _http.PutAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            throw new Exception($"GitHub upload failed: {resp.StatusCode} {err}");
        }

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        if (doc.RootElement.TryGetProperty("content", out var content) && content.TryGetProperty("download_url", out var dl))
        {
            return dl.GetString()!;
        }
        return $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}";
    }
}
