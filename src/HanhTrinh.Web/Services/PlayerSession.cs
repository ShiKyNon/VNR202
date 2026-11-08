using Microsoft.JSInterop;

namespace HanhTrinh.Web.Services;

/// <summary>Mã ẩn + tên của người chơi, lưu trong localStorage của trình duyệt. Chỉ dùng sau lần render đầu.</summary>
public sealed class PlayerSession(IJSRuntime js)
{
    public string? Token { get; private set; }
    public string? Name { get; private set; }

    public async Task<string> EnsureAsync()
    {
        if (Token is not null) return Token;
        Token = await js.InvokeAsync<string?>("localStorage.getItem", "ht_token");
        if (string.IsNullOrEmpty(Token))
        {
            Token = Guid.NewGuid().ToString("N");
            await js.InvokeVoidAsync("localStorage.setItem", "ht_token", Token);
        }
        Name = await js.InvokeAsync<string?>("localStorage.getItem", "ht_name");
        return Token;
    }

    public async Task SetNameAsync(string name)
    {
        Name = name;
        await js.InvokeVoidAsync("localStorage.setItem", "ht_name", name);
    }
}
