using Microsoft.AspNetCore.Mvc;
using LuginaTicket.Services;
using LuginaTicket.ViewModels;

namespace LuginaTicket.Controllers;

public class SupportController : Controller
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SupportController> _logger;

    public SupportController(IEmailService emailService, ILogger<SupportController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    // GET: Support
    public IActionResult Index()
    {
        return View();
    }

    // POST: Support/Contact
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        try
        {
            var subject = $"Contact Form: {model.Subject}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>New Contact Form Submission</h2>
                    <p><strong>Name:</strong> {model.Name}</p>
                    <p><strong>Email:</strong> {model.Email}</p>
                    <p><strong>Subject:</strong> {model.Subject}</p>
                    <p><strong>Message:</strong></p>
                    <p style='white-space: pre-wrap;'>{model.Message}</p>
                </body>
                </html>";

            await _emailService.SendEmailAsync("focusedstudio@proton.me", subject, body);

            TempData["SuccessMessage"] = "Thank you for contacting us! We will get back to you soon.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending contact form email from {Email} with subject {Subject}", model.Email, model.Subject);
            ModelState.AddModelError("", "An error occurred while sending your message. Please check your email configuration or try again later.");
            return View("Index", model);
        }
    }
}
