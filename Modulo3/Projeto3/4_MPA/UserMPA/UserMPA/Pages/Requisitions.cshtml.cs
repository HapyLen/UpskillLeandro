using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LibADO.UserRequisitions;

namespace UserMPA.Pages
{
    public class RequisitionsModel : PageModel
    {
        private readonly GetUserRequisitions _requisicaoRepository;

        public List<Dictionary<string, object>> ObrasRequisitadas { get; set; } = new();
        public RequisitionsModel(string connectionString)
        {
            _requisicaoRepository = new GetUserRequisitions(connectionString);
        }
        public IActionResult OnGet()
        {
            int? pkLeitor = HttpContext.Session.GetInt32("PkLeitor");

            if (pkLeitor == null)
            {
                return RedirectToPage("/Index");
            }
            Console.WriteLine($" PkLeitor encontrado na sessão: {pkLeitor.Value}");

            ObrasRequisitadas = _requisicaoRepository.GetObrasRequisitadas(pkLeitor.Value);
            return Page();
        }
    }
}
