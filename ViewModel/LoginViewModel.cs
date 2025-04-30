using System.ComponentModel.DataAnnotations;

namespace Zoom.ViewModel
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Login As My Team?")]
        public bool LoginAsOthers { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string? EmailAs { get; set; }

    }

}
