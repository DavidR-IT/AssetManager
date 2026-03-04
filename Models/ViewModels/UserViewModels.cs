using AssetManager.Models;
using System.ComponentModel.DataAnnotations;

namespace AssetManager.Models.ViewModels
{
    public class UserViewModel
    {
        public string? Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public int AssetCount { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public List<string>? SelectedRoles { get; set; } = new();
    }

    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a role")]
        public string SelectedRole { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UserDetailViewModel
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<Asset> Assets { get; set; } = new();
    }
}