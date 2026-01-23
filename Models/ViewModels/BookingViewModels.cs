using System.ComponentModel.DataAnnotations;

namespace LuginaTicket.ViewModels;

public class PaymentViewModel
{
    [Required]
    [Display(Name = "Card Number")]
    public string? CardNumber { get; set; }

    [Required]
    [Display(Name = "Expiry Date")]
    [DataType(DataType.Date)]
    public DateTime ExpiryDate { get; set; }

    [Required]
    [Display(Name = "CVC")]
    public string? CVC { get; set; }

    [Required]
    [Display(Name = "Name on Card")]
    public string? NameOnCard { get; set; }

    [Required]
    [Display(Name = "Country or Region")]
    public string? Country { get; set; }
}



public class ContactViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subject is required")]
    [Display(Name = "Subject")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required")]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;
}
