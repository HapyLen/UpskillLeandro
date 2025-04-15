using LibADO.CancelAccount;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserMPA.Pages
{
        public class CancelarADModel : PageModel
        {
            private readonly string _connectionString;

            public string Mensagem { get; set; } = string.Empty;
            public bool Sucesso { get; set; }
            public CancelarADModel(string connectionString)
            {
                _connectionString = connectionString;
            }
            public IActionResult OnGet()
            {
                int? pkLeitor = HttpContext.Session.GetInt32("PkLeitor");

                if (pkLeitor == null)
                {
                    return RedirectToPage("/Index");
                }

                try
                {
                    bool cancelado = Method.sp_cancel_leitor(pkLeitor.Value, _connectionString);

                    if (cancelado)
                    {
                        HttpContext.Session.Clear();
                        Sucesso = true;
                        Mensagem = "Adesão cancelada com sucesso.";
                    }
                    else
                    {
                        Sucesso = false;
                        Mensagem = "Falha ao cancelar adesão.";
                    }
                }
                catch (Exception ex)
                {
                    Sucesso = false;
                    Mensagem = $"Erro ao cancelar adesão: {ex.Message}";
                }

                return Page();
            }
        }
    
}
