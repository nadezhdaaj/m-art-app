using System;

[Serializable]
public class ApiErrorDto
{
    public string code;
    public string message;
}

[Serializable]
public class AuthUserDto
{
    public string id;
    public string name;
    public string email;
    public bool emailVerified;
    public string image;
}

[Serializable]
public class AuthSessionDto
{
    public string id;
    public string token;
    public string userId;
}

[Serializable]
public class AuthResponseDto
{
    public bool redirect;
    public string token;
    public AuthUserDto user;
}

[Serializable]
public class SessionResponseDto
{
    public AuthSessionDto session;
    public AuthUserDto user;
}

[Serializable]
public class UserProgressDto
{
    public string id;
    public string profileId;
    public string xp;
}

[Serializable]
public class ProfileDto
{
    public string id;
    public string userId;
    public string displayName;
    public string bio;
    public string avatarUrl;
    public UserProgressDto progress;
}

[Serializable]
public class ProfileUpdateRequestDto
{
    public string username;
    public string bio;
}

[Serializable]
public class ArtworkDto
{
    public string id;
    public string title;
    public string description;
    public string kind;
    public string source;
    public string status;
    public int schemaVersion;
    public string imageUrl;
    public string thumbnailUrl;
    public string publishedAt;
    public string createdAt;
    public string updatedAt;
}

[Serializable]
public class ArtworkArrayWrapperDto
{
    public ArtworkDto[] items;
}

[Serializable]
public class StringArrayWrapperDto
{
    public string[] items;
}

[Serializable]
public class ExhibitViewRewardDto
{
    public string exhibitId;
    public bool applied;
    public int awardedXp;
    public int previousXp;
    public int totalXp;
}

[Serializable]
public class ExhibitFavoriteDto
{
    public string exhibitId;
    public string createdAt;
}

[Serializable]
public class ArtworkUpsertRequestDto
{
    public string kind = "painting";
    public string source = "paint-canvas";
    public string title;
    public string description;
    public string status = "DRAFT";
    public string schemaVersion = "1";
    public byte[] imageBytes;
    public string imageFileName = "artwork.png";
    public string thumbnailFileName = "artwork-thumb.png";
    public byte[] thumbnailBytes;
}

[Serializable]
public class NoteDto
{
    public string id;
    public string text;
    public string category;
    public string exhibitId;
    public string createdAt;
    public string updatedAt;
}

[Serializable]
public class NoteArrayWrapperDto
{
    public NoteDto[] items;
}

[Serializable]
public class NoteUpsertRequestDto
{
    public string text;
    public string category;
    public string exhibitId;
}

[Serializable]
public class SignInRequestDto
{
    public string email;
    public string password;
    public bool rememberMe = true;
}

[Serializable]
public class SignUpRequestDto
{
    public string name;
    public string email;
    public string password;
    public bool rememberMe = true;
}

[Serializable]
public class ChangePasswordRequestDto
{
    public string currentPassword;
    public string newPassword;
}

public class ApiResult<T>
{
    public bool Success;
    public int StatusCode;
    public bool Unauthorized;
    public string ErrorMessage;
    public T Data;
}
