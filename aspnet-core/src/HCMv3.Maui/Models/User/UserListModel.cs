using HCMv3.Authorization.Users.Dto;

namespace HCMv3.Maui.Models.User;

public class UserListModel : UserListDto
{
    public string Photo { get; set; }

    public string FullName => Name + " " + Surname;
}