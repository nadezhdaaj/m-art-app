using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class BackendAuthApiClient
{
    private readonly string baseUrl;

    public BackendAuthApiClient(string baseUrl)
    {
        this.baseUrl = NormalizeBaseUrl(baseUrl);
    }

    public IEnumerator SignIn(SignInRequestDto payload, Action<ApiResult<AuthResponseDto>> onComplete)
    {
        yield return SendJsonRequest("/auth/sign-in/email", UnityWebRequest.kHttpVerbPOST, JsonUtility.ToJson(payload), null, onComplete);
    }

    public IEnumerator SignUp(SignUpRequestDto payload, Action<ApiResult<AuthResponseDto>> onComplete)
    {
        yield return SendJsonRequest("/auth/sign-up/email", UnityWebRequest.kHttpVerbPOST, JsonUtility.ToJson(payload), null, onComplete);
    }

    public IEnumerator GetSession(string token, Action<ApiResult<SessionResponseDto>> onComplete)
    {
        yield return SendJsonRequest("/auth/get-session", UnityWebRequest.kHttpVerbGET, null, token, onComplete);
    }

    public IEnumerator GetProfile(string token, Action<ApiResult<ProfileDto>> onComplete)
    {
        yield return SendJsonRequest("/profile/me", UnityWebRequest.kHttpVerbGET, null, token, onComplete);
    }

    public IEnumerator SignOut(string token, Action<ApiResult<object>> onComplete)
    {
        yield return SendJsonRequest<object>("/auth/sign-out", UnityWebRequest.kHttpVerbPOST, "{}", token, onComplete);
    }

    public IEnumerator ChangePassword(string token, ChangePasswordRequestDto payload, Action<ApiResult<object>> onComplete)
    {
        yield return SendJsonRequest<object>("/auth/change-password", UnityWebRequest.kHttpVerbPOST, JsonUtility.ToJson(payload), token, onComplete);
    }

    private IEnumerator SendJsonRequest<T>(string path, string method, string jsonBody, string token, Action<ApiResult<T>> onComplete)
    {
        using UnityWebRequest request = new UnityWebRequest(baseUrl + path, method);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Accept", "application/json");

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }

        if (!string.IsNullOrEmpty(jsonBody))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");
        }

        yield return request.SendWebRequest();

        ApiResult<T> result = BuildResult<T>(request);
        onComplete?.Invoke(result);
    }

    private ApiResult<T> BuildResult<T>(UnityWebRequest request)
    {
        string responseText = request.downloadHandler?.text ?? string.Empty;
        bool success = request.result == UnityWebRequest.Result.Success &&
            request.responseCode >= 200 &&
            request.responseCode < 300;

        ApiResult<T> result = new ApiResult<T>
        {
            Success = success,
            StatusCode = (int)request.responseCode,
            Unauthorized = request.responseCode == 401,
            ErrorMessage = success ? null : GetErrorMessage(responseText, request.error),
        };

        if (success && typeof(T) != typeof(object) && !string.IsNullOrWhiteSpace(responseText))
        {
            result.Data = JsonUtility.FromJson<T>(responseText);
        }

        return result;
    }

    private string GetErrorMessage(string responseText, string fallbackError)
    {
        if (!string.IsNullOrWhiteSpace(responseText))
        {
            try
            {
                ApiErrorDto error = JsonUtility.FromJson<ApiErrorDto>(responseText);
                if (!string.IsNullOrWhiteSpace(error?.message))
                {
                    return error.message;
                }
            }
            catch (ArgumentException)
            {
            }

            return responseText;
        }

        return string.IsNullOrWhiteSpace(fallbackError) ? "Request failed." : fallbackError;
    }

    private string NormalizeBaseUrl(string rawBaseUrl)
    {
        string normalized = string.IsNullOrWhiteSpace(rawBaseUrl) ? "http://localhost:3000" : rawBaseUrl.Trim();
        return normalized.EndsWith("/") ? normalized.TrimEnd('/') : normalized;
    }
}
