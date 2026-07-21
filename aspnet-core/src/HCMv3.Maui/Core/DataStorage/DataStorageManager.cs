using System.Text.Json;
using Abp.Dependency;
using Abp.Runtime.Security;
using HCMv3.Maui.Core.Helpers;

namespace HCMv3.Maui.Core.DataStorage;

/// <summary>
/// Uses Xamarin.Forms Application Properties to save data.
/// If you need to store secure values such as password, use ISecureStorage.
/// </summary>
public class DataStorageManager : ISingletonDependency, IDataStorageManager
{
    private static void StorePrimitive(string key, object value)
    {
        if (value == null)
        {
            Preferences.Set(key, null);
        }
        else
        {
            Preferences.Set(key, value.ToString());
        }
    }

    private static void StoreObject(string key, object value)
    {
        Preferences.Set(key, JsonSerializer.Serialize(value));
    }

    private T GetPrimitive<T>(string key, T defaultValue = default(T))
    {
        if (!HasKey(key))
        {
            return defaultValue;
        }

        return (T)Convert.ChangeType(Preferences.Get(key, null), typeof(T));
    }

    private T RetrieveObject<T>(string key, T defaultValue = default(T))
    {
        var value = Preferences.Get(key, "");
        return !HasKey(key) ?
            defaultValue :
            JsonSerializer.Deserialize<T>(value);
    }

    public bool HasKey(string key)
    {
        return Preferences.ContainsKey(key);
    }

    public T Retrieve<T>(string key, T defaultValue = default(T), bool shouldDecrpyt = false)
    {
        var value = TypeHelper.IsPrimitive(typeof(T), false) ?
            GetPrimitive(key, defaultValue) :
            RetrieveObject(key, defaultValue);

        if (!shouldDecrpyt)
        {
            return value;
        }

        var decrypted = SimpleStringCipher.Instance.Decrypt(Convert.ToString(value));
        return (T)Convert.ChangeType(decrypted, typeof(T));
    }

    public async Task StoreAsync<T>(string key, T value, bool shouldEncrypt = false)
    {
        if (TypeHelper.IsPrimitive(typeof(T), false))
        {
            if (shouldEncrypt)
            {
                StorePrimitive(key, SimpleStringCipher.Instance.Encrypt(Convert.ToString(value)));
            }
            else
            {
                StorePrimitive(key, value);
            }
        }
        else
        {
            StoreObject(key, value);
        }

        await Task.CompletedTask;
    }

    public void RemoveIfExists(string key)
    {
        if (HasKey(key))
        {
            Preferences.Remove(key);
        }
    }
}