using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    public IEnumerator UpdateProfile(string token, ProfileUpdateRequestDto payload, Action<ApiResult<ProfileDto>> onComplete)
    {
        yield return SendJsonRequest(
            "/profile/me",
            "PATCH",
            JsonUtility.ToJson(payload),
            token,
            onComplete
        );
    }

    public IEnumerator GetMyArtworks(string token, Action<ApiResult<ArtworkArrayWrapperDto>> onComplete)
    {
        using UnityWebRequest request = new UnityWebRequest(baseUrl + "/artworks/me", UnityWebRequest.kHttpVerbGET);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Accept", "application/json");

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }

        yield return request.SendWebRequest();

        ApiResult<ArtworkArrayWrapperDto> result = BuildArrayResult<ArtworkDto, ArtworkArrayWrapperDto>(request, "items");
        onComplete?.Invoke(result);
    }

    public IEnumerator GetViewedExhibits(string token, Action<ApiResult<StringArrayWrapperDto>> onComplete)
    {
        using UnityWebRequest request = new UnityWebRequest(baseUrl + "/progress/exhibits/views", UnityWebRequest.kHttpVerbGET);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Accept", "application/json");

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }

        yield return request.SendWebRequest();

        ApiResult<StringArrayWrapperDto> result = BuildArrayResult<string, StringArrayWrapperDto>(request, "items");
        onComplete?.Invoke(result);
    }

    public IEnumerator RecordExhibitView(string token, string exhibitId, Action<ApiResult<ExhibitViewRewardDto>> onComplete)
    {
        string escapedExhibitId = UnityWebRequest.EscapeURL(exhibitId ?? string.Empty);
        yield return SendJsonRequest($"/progress/exhibits/{escapedExhibitId}/view", UnityWebRequest.kHttpVerbPOST, "{}", token, onComplete);
    }

    public IEnumerator GetFavoriteExhibits(string token, Action<ApiResult<StringArrayWrapperDto>> onComplete)
    {
        using UnityWebRequest request = new UnityWebRequest(baseUrl + "/favorites/exhibits", UnityWebRequest.kHttpVerbGET);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Accept", "application/json");

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }

        yield return request.SendWebRequest();

        ApiResult<StringArrayWrapperDto> result = BuildArrayResult<string, StringArrayWrapperDto>(request, "items");
        onComplete?.Invoke(result);
    }

    public IEnumerator AddFavoriteExhibit(string token, string exhibitId, Action<ApiResult<ExhibitFavoriteDto>> onComplete)
    {
        string escapedExhibitId = UnityWebRequest.EscapeURL(exhibitId ?? string.Empty);
        yield return SendJsonRequest($"/favorites/exhibits/{escapedExhibitId}", UnityWebRequest.kHttpVerbPOST, "{}", token, onComplete);
    }

    public IEnumerator RemoveFavoriteExhibit(string token, string exhibitId, Action<ApiResult<object>> onComplete)
    {
        string escapedExhibitId = UnityWebRequest.EscapeURL(exhibitId ?? string.Empty);
        yield return SendJsonRequest<object>($"/favorites/exhibits/{escapedExhibitId}", UnityWebRequest.kHttpVerbDELETE, null, token, onComplete);
    }

    public IEnumerator GetArtwork(string token, string artworkId, Action<ApiResult<ArtworkDto>> onComplete)
    {
        yield return SendJsonRequest($"/artworks/me/{artworkId}", UnityWebRequest.kHttpVerbGET, null, token, onComplete);
    }

    public IEnumerator CreateArtwork(string token, ArtworkUpsertRequestDto payload, Action<ApiResult<ArtworkDto>> onComplete)
    {
        yield return SendArtworkMultipartRequest("/artworks/me", "POST", token, payload, onComplete);
    }

    public IEnumerator UpdateArtwork(string token, string artworkId, ArtworkUpsertRequestDto payload, Action<ApiResult<ArtworkDto>> onComplete)
    {
        yield return SendArtworkMultipartRequest($"/artworks/me/{artworkId}", "PATCH", token, payload, onComplete);
    }

    public IEnumerator DeleteArtwork(string token, string artworkId, Action<ApiResult<object>> onComplete)
    {
        yield return SendJsonRequest<object>($"/artworks/me/{artworkId}", UnityWebRequest.kHttpVerbDELETE, null, token, onComplete);
    }

    public IEnumerator GetMyNotes(string token, Action<ApiResult<NoteArrayWrapperDto>> onComplete)
    {
        using UnityWebRequest request = new UnityWebRequest(baseUrl + "/notes/me", UnityWebRequest.kHttpVerbGET);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Accept", "application/json");

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }

        yield return request.SendWebRequest();

        ApiResult<NoteArrayWrapperDto> result = BuildArrayResult<NoteDto, NoteArrayWrapperDto>(request, "items");
        onComplete?.Invoke(result);
    }

    public IEnumerator CreateNote(string token, NoteUpsertRequestDto payload, Action<ApiResult<NoteDto>> onComplete)
    {
        yield return SendJsonRequest("/notes/me", UnityWebRequest.kHttpVerbPOST, JsonUtility.ToJson(payload), token, onComplete);
    }

    public IEnumerator UpdateNote(string token, string noteId, NoteUpsertRequestDto payload, Action<ApiResult<NoteDto>> onComplete)
    {
        yield return SendJsonRequest($"/notes/me/{noteId}", "PATCH", JsonUtility.ToJson(payload), token, onComplete);
    }

    public IEnumerator DeleteNote(string token, string noteId, Action<ApiResult<object>> onComplete)
    {
        yield return SendJsonRequest<object>($"/notes/me/{noteId}", UnityWebRequest.kHttpVerbDELETE, null, token, onComplete);
    }

    public IEnumerator SignOut(string token, Action<ApiResult<object>> onComplete)
    {
        yield return SendJsonRequest<object>("/auth/sign-out", UnityWebRequest.kHttpVerbPOST, "{}", token, onComplete);
    }

    public IEnumerator ChangePassword(string token, ChangePasswordRequestDto payload, Action<ApiResult<object>> onComplete)
    {
        yield return SendJsonRequest<object>("/auth/change-password", UnityWebRequest.kHttpVerbPOST, JsonUtility.ToJson(payload), token, onComplete);
    }

    public IEnumerator UpdateProfileAvatar(string token, string filePath, Action<ApiResult<ProfileDto>> onComplete)
    {
        byte[] fileBytes = File.ReadAllBytes(filePath);
        string fileName = Path.GetFileName(filePath);
        string contentType = GetImageContentType(filePath);

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("avatar", fileBytes, fileName, contentType),
        };

        using UnityWebRequest request = UnityWebRequest.Post(baseUrl + "/profile/me", formData);
        request.method = "PATCH";
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Accept", "application/json");

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }

        yield return request.SendWebRequest();

        ApiResult<ProfileDto> result = BuildResult<ProfileDto>(request);
        onComplete?.Invoke(result);
    }

    public IEnumerator DeleteProfileAvatar(string token, Action<ApiResult<ProfileDto>> onComplete)
    {
        yield return SendJsonRequest<ProfileDto>(
            "/profile/me/avatar",
            UnityWebRequest.kHttpVerbDELETE,
            null,
            token,
            onComplete
        );
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

        request.timeout = 10;

        yield return request.SendWebRequest();

        ApiResult<T> result = BuildResult<T>(request);
        onComplete?.Invoke(result);
    }

    private IEnumerator SendArtworkMultipartRequest(string path, string method, string token, ArtworkUpsertRequestDto payload, Action<ApiResult<ArtworkDto>> onComplete)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

        AddMultipartField(formData, "kind", payload?.kind);
        AddMultipartField(formData, "source", payload?.source);
        AddMultipartField(formData, "title", payload?.title);
        AddMultipartField(formData, "description", payload?.description);
        AddMultipartField(formData, "status", payload?.status);
        AddMultipartField(formData, "schemaVersion", payload?.schemaVersion);

        if (payload?.imageBytes != null && payload.imageBytes.Length > 0)
        {
            formData.Add(new MultipartFormFileSection(
                "image",
                payload.imageBytes,
                string.IsNullOrWhiteSpace(payload.imageFileName) ? "artwork.png" : payload.imageFileName,
                "image/png"
            ));
        }

        if (payload?.thumbnailBytes != null && payload.thumbnailBytes.Length > 0)
        {
            formData.Add(new MultipartFormFileSection(
                "thumbnail",
                payload.thumbnailBytes,
                string.IsNullOrWhiteSpace(payload.thumbnailFileName) ? "artwork-thumb.png" : payload.thumbnailFileName,
                "image/png"
            ));
        }

        using UnityWebRequest request = UnityWebRequest.Post(baseUrl + path, formData);
        request.method = method;
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Accept", "application/json");

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }

        yield return request.SendWebRequest();

        ApiResult<ArtworkDto> result = BuildResult<ArtworkDto>(request);
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

    private ApiResult<TWrapper> BuildArrayResult<TItem, TWrapper>(UnityWebRequest request, string wrapperFieldName)
    {
        string responseText = request.downloadHandler?.text ?? string.Empty;
        bool success = request.result == UnityWebRequest.Result.Success &&
            request.responseCode >= 200 &&
            request.responseCode < 300;

        ApiResult<TWrapper> result = new ApiResult<TWrapper>
        {
            Success = success,
            StatusCode = (int)request.responseCode,
            Unauthorized = request.responseCode == 401,
            ErrorMessage = success ? null : GetErrorMessage(responseText, request.error),
        };

        if (success && !string.IsNullOrWhiteSpace(responseText))
        {
            string wrappedJson = "{\"" + wrapperFieldName + "\":" + responseText + "}";
            result.Data = JsonUtility.FromJson<TWrapper>(wrappedJson);
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

    private void AddMultipartField(List<IMultipartFormSection> formData, string fieldName, string value)
    {
        if (value == null)
        {
            return;
        }

        formData.Add(new MultipartFormDataSection(fieldName, value));
    }

    private string GetImageContentType(string filePath)
    {
        string extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        switch (extension)
        {
            case ".png":
                return "image/png";
            case ".jpg":
            case ".jpeg":
                return "image/jpeg";
            case ".webp":
                return "image/webp";
            case ".gif":
                return "image/gif";
            case ".bmp":
                return "image/bmp";
            default:
                return "application/octet-stream";
        }
    }

}
