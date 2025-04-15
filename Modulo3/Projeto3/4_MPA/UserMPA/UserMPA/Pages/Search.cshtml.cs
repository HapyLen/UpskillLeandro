using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LibADO.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using LibADO.Search.LibADO.Search;

namespace UserMPA.Pages
{
    public class SearchModel : PageModel
    {
        private readonly string _connectionString;

        public List<Dictionary<string, object>> Obras { get; set; } = new List<Dictionary<string, object>>();
        public List<string> Categorias { get; set; } = new();

        public int PaginaAtual { get; set; } = 1;
        public int TotalPaginas { get; set; }

        private const int ItensPorPagina = 6;
        public SearchModel(string connectionString)
        {
            _connectionString = connectionString;
        }
        public void OnGet(string? obra, string? genre, int pagina = 1)
        {
            MethodesSearch searchMethods = new();
            Categorias = searchMethods.ObterCategorias(_connectionString);

            PaginaAtual = pagina < 1 ? 1 : pagina;

            var todasObras = SearchSP.sp_search_obras_com_imagem(obra, genre, _connectionString);
            TotalPaginas = (int)Math.Ceiling(todasObras.Count / (double)ItensPorPagina);
            Obras = todasObras.Skip((PaginaAtual - 1) * ItensPorPagina).Take(ItensPorPagina).ToList();
        }
    }
}
