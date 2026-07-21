namespace HCMv3.Authorization.QrLogin;

public class QrLoginAuthenticateResultModel
{
    public string AccessToken { get; set; }

    public string EncryptedAccessToken { get; set; }

    public int ExpireInSeconds { get; set; }

    public long UserId { get; set; }

    public string RefreshToken { get; set; }

    public string ReturnUrl { get; set; }

    public int RefreshTokenExpireInSeconds { get; set; }
}