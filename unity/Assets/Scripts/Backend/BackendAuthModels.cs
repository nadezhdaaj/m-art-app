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
    public string avatarUrl;
    public UserProgressDto progress;
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
