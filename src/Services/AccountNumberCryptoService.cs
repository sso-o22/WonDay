using Microsoft.JSInterop;

namespace WonDay.Services;

public class EncryptedValue
{
    public string Ciphertext { get; set; } = "";
    public string Iv { get; set; } = "";
    public string Salt { get; set; } = "";
}

/// <summary>
/// 계좌번호처럼 민감한 값을 브라우저에서 암호화/복호화합니다.
/// PIN은 서버로 전송되지 않고, 이 세션(탭을 새로고침하거나 닫기 전까지) 동안만 메모리에 잠깐 보관해요.
/// PIN을 잊어버리면 이미 저장해둔 번호는 복구할 방법이 없다는 걸 사용자에게 꼭 안내해야 해요.
/// </summary>
public class AccountNumberCryptoService
{
    private readonly IJSRuntime _js;
    private string? _cachedPin;

    public AccountNumberCryptoService(IJSRuntime js)
    {
        _js = js;
    }

    public bool HasPinForSession => !string.IsNullOrEmpty(_cachedPin);

    public void SetSessionPin(string pin) => _cachedPin = pin;

    public void ClearSessionPin() => _cachedPin = null;

    public async Task<EncryptedValue> EncryptAsync(string plaintext, string pin)
    {
        _cachedPin = pin; // 입력한 김에 이 세션 동안은 재사용
        return await _js.InvokeAsync<EncryptedValue>("wondayCrypto.encrypt", plaintext, pin);
    }

    /// <summary>PIN이 틀리면 JSException이 발생해요.</summary>
    public async Task<string> DecryptAsync(EncryptedValue value, string pin)
    {
        var result = await _js.InvokeAsync<string>(
            "wondayCrypto.decrypt", value.Ciphertext, value.Iv, value.Salt, pin);
        _cachedPin = pin;
        return result;
    }

    // 이 세션에서 이미 PIN을 입력한 적 있으면 그 값으로 바로 시도
    public async Task<string?> TryDecryptWithCachedPinAsync(EncryptedValue value)
    {
        if (_cachedPin is null) return null;
        try
        {
            return await DecryptAsync(value, _cachedPin);
        }
        catch
        {
            return null;
        }
    }

    public async Task CopyToClipboardAsync(string text)
    {
        await _js.InvokeVoidAsync("wondayCrypto.copyToClipboard", text);
    }
}
