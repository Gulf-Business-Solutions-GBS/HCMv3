using System.Threading.Tasks;

namespace HCMv3.Authorization.Users.Profile;

public interface IProfilePictureValidator
{
    Task ValidateProfilePictureDimensions(byte[] imageBytes);
    Task ValidateProfilePictureSize(byte[] imageBytes);
}