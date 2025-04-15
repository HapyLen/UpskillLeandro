using Azure;
using LibEF.Models;
using LibEF;
using System.Text.Json;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Drawing;
using System.Diagnostics.Eventing.Reader;

//TODO make overall / Utilizadores / nucleos / obras pages
//each will auto show all values and will have an search bar // bonus maybe search from what is shown?

//TODO add JWT token auth
//TODO Seperate stuff into classes
//TODO list EF_methods really needed

namespace WebAPI.Model
{
	public class Model
	{

		private readonly ProjectoContext _context;
		private readonly methodsMapping _map_method;

		public Model(string conn_str)
		{
			_context = CreateContext(conn_str);
			_map_method = new methodsMapping();
		}

		private ProjectoContext CreateContext(string connectionString)
		{
			var optionsBuilder = new DbContextOptionsBuilder<ProjectoContext>();
			optionsBuilder.UseSqlServer(connectionString);
			return new ProjectoContext(optionsBuilder.Options);
		}

		public object MethodCaller (string description, JsonElement param)
		{
			string method = _map_method.searchbyDescrition(description);
			var methods_dic = _map_method.MethodsDict;
			if (!methods_dic.ContainsKey(method))
				throw new Exception("no such method listed.");
            var ParamList = _map_method.GetParamList(method, param);
			var result = ResolveMethod(method, ParamList);
			return (result);
		}

		

		public object ResolveMethod (string method, List <object>? ParamList)
		{
			EF_methods ef = new EF_methods(_context);
			Type type = typeof(EF_methods);
			MethodInfo? method_fun = type.GetMethod(method);
			try
			{
				if (method_fun != null)
				{
					if (ParamList != null && ParamList.Count > 0)
						return method_fun.Invoke(ef, ParamList.ToArray());  // Call the method with multiple arguments
					else
						return method_fun.Invoke(ef, null);
				}
				else 
					throw new Exception("failed, no such method");
			}
			catch (TargetInvocationException ex)
			{
				throw new Exception(ex.InnerException?.Message ?? ex.Message);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}

		public bool Login(JsonElement jsonRes)
		{
			ProjectoContext context = new ProjectoContext();
			EF_methods ef = new EF_methods(_context);
			try
			{
				Dictionary<string, string> res = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonRes);
				string pw_res = ef.GetPassWordbyLogin(res["email"]);
				string pw_login = res["password"];
				if (pw_login != pw_res){
					throw new Exception("Invalid Password");
				}	
				return true;
			}
			catch
			{
				return false;
			}
		}

		public Object GetData(string tab, string? search = null, string? page = null)
		{
			try
			{
				ProjectoContext context = new ProjectoContext();
				EF_methods ef = new EF_methods(_context);
                List<dynamic> result = new List<dynamic>();
				if (search == "null")
					search = null;
				if (tab == "reservas")
                    result = ef.GetAllRequisicoes(search);
				else if (tab == "obras")
                    result = ef.GetAllObras(search);
				else if (tab == "nucleos")
                    result = ef.GetAllNucleos(search);
				else if (tab == "utilizadores")
                    result = ef.GetAllLeitores(search);
				return paginationrow(result, page);
            }
			catch (Exception ex)
            {
				throw new Exception(ex.Message);
			}
		}

        public object paginationrow(List<dynamic> result, string? page)
        {
            int count = result.Count;
            int sizeInt = 10;
			int pageInt = 1;
			if (int.TryParse(page, out pageInt) == true)
				;
            result = result.Skip((pageInt - 1) * sizeInt).Take(sizeInt).ToList();
			var totalPages = count / sizeInt;
			if (totalPages == 0)
				totalPages++;
            var response = new Dictionary<string, object>
            {
                { "table", result },
                { "currPage", pageInt },
                { "totalPages", totalPages },
            };
            return response;
        }
    }
}
