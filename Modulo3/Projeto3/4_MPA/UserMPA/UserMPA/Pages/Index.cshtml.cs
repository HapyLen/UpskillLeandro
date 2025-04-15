using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LibADO.Login;

namespace UserMPA.Pages
{
    public class IndexModel : PageModel
    {
        private readonly LoginService _loginService;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Senha { get; set; } = string.Empty;

        public string MensagemErro { get; set; } = string.Empty;

        public IndexModel(LoginService loginService)
        {
            _loginService = loginService;
        }

        public void OnGet()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("MensagemErro")))
            {
                MensagemErro = HttpContext.Session.GetString("MensagemErro")!;
                HttpContext.Session.Remove("MensagemErro");
            }
        }

        public IActionResult OnPost()
        {
            string resultadoLogin = _loginService.TentarLogin(Email, Senha);

            if (resultadoLogin == "OK")
            {
                var usuario = _loginService.ObterUsuario(Email);
                if (usuario != null)
                {
                    HttpContext.Session.SetString("NomeUsuario", usuario.Nome);
                    HttpContext.Session.SetInt32("PkLeitor", usuario.Id);
                    return RedirectToPage("/Index");
                }
            }

            HttpContext.Session.SetString("MensagemErro", resultadoLogin);
            return RedirectToPage("/Index");
        }
    }
}