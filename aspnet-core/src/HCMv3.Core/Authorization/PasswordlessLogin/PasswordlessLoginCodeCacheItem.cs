using System;

namespace HCMv3.Authorization.PasswordlessLogin;

public class PasswordlessLoginCodeCacheItem
{
    public const string CacheName = "AppPasswordlessVerificationCodeCache";

    public string Code { get; set; }

    public static readonly TimeSpan DefaultSlidingExpireTime = TimeSpan.FromMinutes(1);

    public PasswordlessLoginCodeCacheItem()
    {

    }

    public PasswordlessLoginCodeCacheItem(string code)
    {
        Code = code;
    }
}

