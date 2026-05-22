using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClickYa.Comercios.Pages.Admin
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Usuario { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        public string Error { get; set; } = "";

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (Usuario == "admin" && Password == "clickya123")
            {
                return RedirectToPage("/Admin/Index");
            }

            Error = "Usuario o contraseña incorrectos";
            return Page();
        }
    }
}
