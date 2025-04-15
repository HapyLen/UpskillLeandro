using System.Text.Json;

namespace WebAPI.Model
{
	public class MethodParameter
	{
		public string name { get; set; }
		public string type { get; set; }
		public int? size { get; set; }
		public bool mandatory { get; set; }
	}

	public class MethodDetails
	{
		public string Description { get; set; }
		public string MethodName { get; set; }
		public string Category { get; set; }
	}

	public class methodsMapping
	{
		public readonly Dictionary<string, List<MethodDetails>> method_category;
		public Dictionary<string, List<MethodParameter>> MethodsDict;
		public readonly string MethodsJson;

		public methodsMapping()
		{
			method_category = new Dictionary<string, List<MethodDetails>>();
			MethodsDict = new Dictionary<string, List<MethodParameter>>();
			BuildMethodCategory();
			BuildMethodsDict();
			MethodsJson = JsonSerializer.Serialize(MethodsDict);
		}

		public List<object>? GetParamList(string method, JsonElement param)
		{
			if (param.ValueKind == JsonValueKind.Undefined || param.ValueKind == JsonValueKind.Null || MethodsDict[method] == null)
            {
				return null;
			}
			var paramDict = JsonSerializer.Deserialize<Dictionary<string, string>>(param);
			var paramList = new List<object>();
			foreach (var item in MethodsDict[method])
			{
				var paramName = item.GetType().GetProperty("name")?.GetValue(item)?.ToString();
				var paramType = item.GetType().GetProperty("type")?.GetValue(item)?.ToString();

				if (paramName != null && paramDict.ContainsKey(paramName))
				{
					switch (paramType)
					{
						case "number":
							paramList.Add(int.Parse(paramDict[paramName]));
							break;
						case "text":
						case "email":
						case "password":
						case "radio":
							paramList.Add(paramDict[paramName].ToString());
							break;
						case "date":
							paramList.Add(DateTime.Parse(paramDict[paramName]));
							break;
						default:
							paramList.Add(paramDict[paramName].ToString());
							break;
					}
				}
				else
					paramList.Add(null);
			}
			foreach (var val in paramList) //TODO remove degub lines
				Console.WriteLine(val ?? "null");
			return paramList;
		}

		public string searchbyDescrition (string Description)
		{
			List<MethodDetails> methods = new List<MethodDetails>();
			foreach (var meth in method_category)
			{
				methods.AddRange(meth.Value);
			}
			var result = new Dictionary<string, List<MethodParameter>>();
			foreach (var method in methods)
			{
				if (method.Description == Description)
					return method.MethodName;
            }
			return "";
		}

		public Dictionary<string, List<MethodParameter>> GetMethods(string level, string cat)
		{
			if (level != "all" && !method_category.ContainsKey(level))
				throw new Exception("level not found try any:" + string.Join(", ", method_category.Keys));
			List<MethodDetails> methods = new List<MethodDetails>();
			foreach (var meth in method_category)
			{
				if (level == "all" || meth.Key == level)
					methods.AddRange(meth.Value);
			}
			var result = new Dictionary<string, List<MethodParameter>>();
			foreach (var method in methods)
			{
				if (cat == "all" || method.Category == cat)
				{
					if (MethodsDict.ContainsKey(method.MethodName))
					{
						result[method.Description] = MethodsDict[method.MethodName];
					}
				}
			}
			if (result.Count == 0)
				throw new Exception("No methods found for <" + cat + "> try: " + string.Join(", ", methods.Select(x => x.Category)));
			return result;
		}

		public void BuildMethodCategory()
		{
            method_category["obras"] = new List<MethodDetails>
			{
				new MethodDetails
				{
					Description = "Inserir Obra",
					MethodName = "InsertObra",
					Category = "items"
				},
				new MethodDetails
				{
					Description = "Atualizar Imagem da Obra",
					MethodName = "UpdateImage",
					Category = "items"
				},
				new MethodDetails
				{
					Description = "Adicionar Obra",
					MethodName = "AddObra",
					Category = "items"
				},
				new MethodDetails
				{
					Description = "Remover Obra",
					MethodName = "RemoveObra",
					Category = "items"
				},

				new MethodDetails
				{
					Description = "Transferir Obra",
					MethodName = "TransferObra",
					Category = "items"
				},
			};

			method_category["nucleos"] = new List<MethodDetails>
			{
				new MethodDetails
				{
					Description = "Adicionar Obra em Núcleo",
					MethodName = "AddObraInNucleo",
					Category = "items"
				},
			};

			method_category["reservas"] = new List<MethodDetails>
			{
				new MethodDetails
				{
					Description = "Status da Requisição",
					MethodName = "requesicao_status",
					Category = "filter"
				},
			};

			method_category["utilizadores"] = new List<MethodDetails>
			{
				new MethodDetails
				{
					Description = "Inserir Utilizador",
					MethodName = "InsertLeitor",
					Category = "items"
				},
				new MethodDetails
				{
					Description = "Suspender Utilizador por Atraso",
					MethodName = "SuspendLateLeitor",
					Category = "items"
				},
				new MethodDetails
				{
					Description = "Suspender Utilizador",
					MethodName = "SuspendLeitor",
					Category = "items"
				},
				new MethodDetails
				{
					Description = "Reativar Utilizador",
					MethodName = "sp_leitor_reactivate",
					Category = "items"
				},
				new MethodDetails
				{
					Description = "Deletar Utilizador Inativo",
					MethodName = "sp_delete_inactive_Leitor",
					Category = "items"
				},
				new MethodDetails
				{
					Description = "Deletar Utilizador",
					MethodName = "sp_del_leitor",
					Category = "items"
				},
				new MethodDetails
				{
					Description = "Salvar Histórico do Utilizador",
					MethodName = "sp_save_leitor_history",
					Category = "items"
				},
				new MethodDetails
				{
					Description = "Cancelar Utilizador",
					MethodName = "sp_cancel_leitor",
					Category = "items"
				},
			};

            method_category["stats"] = new List<MethodDetails>
            {
                new MethodDetails
                {
                    Description = "Get Total de Obras",
                    MethodName = "GetTotalObra",
                    Category = "items"
                },
                new MethodDetails
                {
                    Description = "Get Total de Obras por Gênero",
                    MethodName = "GetTotalObraPorGenero",
                    Category = "items"
                },
                new MethodDetails
                {
                    Description = "Get Top Obras Requisitadas por Tempo",
                    MethodName = "GetTopRequestedByTime",
                    Category = "items"
                },
            };
        }

		public void BuildMethodsDict()
		{
			MethodsDict["GetObra"] = null;
			MethodsDict["GetAllLeitores"] = null;
			MethodsDict["GetAllNucleos"] = null;
			MethodsDict["GetAllRequisicoes"] = null;
			MethodsDict["GetTotalObra"] = null;
			MethodsDict["GetTotalObraPorGenero"] = null;
			MethodsDict["GetTopRequestedByTime"] = new List<MethodParameter> {
				new MethodParameter { name = "Data início", type = "date", size = 0, mandatory = true},
				new MethodParameter { name = "Data fim", type = "date", size = 0, mandatory = true}
			};
			MethodsDict["GetRequisicoesByNucleo"] = new List<MethodParameter> {
				new MethodParameter { name = "Data início", type = "date", size = 0, mandatory = true},
				new MethodParameter { name = "Data fim", type = "date", size = 0, mandatory = true}
			};
			MethodsDict["InsertObra"] = new List<MethodParameter> {
				new MethodParameter { name = "Núcleo a adicionar (opcional)", type = "number", mandatory = false},
				new MethodParameter { name = "Nome da obra", type = "text", size = 50, mandatory = true},
				new MethodParameter { name = "ISBN", type = "text", size = 50, mandatory = true},
				new MethodParameter { name = "Autor", type = "text", size = 50, mandatory = true},
				new MethodParameter { name = "Editora", type = "text", size = 50, mandatory = true},
				new MethodParameter { name = "Ano", type = "text", size = 50, mandatory = true},
				new MethodParameter { name = "Imagem", type = "file", mandatory = true},
				new MethodParameter { name = "Quantidade (opcional)", type = "number", mandatory = false},

			};
			MethodsDict["UpdateImage"] = new List<MethodParameter> {
				new MethodParameter { name = "ID da obra a adicionar", type = "number", mandatory = true},
				new MethodParameter { name = "Imagem", type = "file", mandatory = true},
				new MethodParameter { name = "ISBN", type = "text", size = 50, mandatory = true}
			};
			MethodsDict["GetCentralNucleo"] = null;
			MethodsDict["AddObraInNucleo"] = new List<MethodParameter> {
				new MethodParameter { name = "ID da obra", type = "number", mandatory = true},
				new MethodParameter { name = "Núcleo a adicionar", type = "number", mandatory = true},
				new MethodParameter { name = "Quantidade", type = "number", mandatory = true},
			};
			MethodsDict["RemoveObra"] = new List<MethodParameter> {
				new MethodParameter { name = "ID da obra a remover", type = "number", mandatory = true},
				new MethodParameter { name = "Núcleo a remover", type = "number", mandatory = true},
				new MethodParameter { name = "Quantidade", type = "number", mandatory = true},
			};
			MethodsDict["TransferObra"] = new List<MethodParameter> {
				new MethodParameter { name = "ID da obra a transferir", type = "number", mandatory = true},
				new MethodParameter { name = "Núcleo de origem", type = "number", mandatory = true},
				new MethodParameter { name = "Núcleo de destino", type = "number", mandatory = true},
				new MethodParameter { name = "Quantidade", type = "number", mandatory = true},

			};
			MethodsDict["InsertLeitor"] = new List<MethodParameter> {
				new MethodParameter { name = "Nome do utilizador", type = "text", size = 50, mandatory = true},
				new MethodParameter { name = "Morada", type = "text", size = 50, mandatory = true},
				new MethodParameter { name = "Telemóvel", type = "number", size = 9, mandatory = true},
				new MethodParameter { name = "Email", type = "email", size = 50, mandatory = true},
				new MethodParameter { name = "Password", type = "password", size = 50, mandatory = true},
				new MethodParameter { name = "Tipo de utilizador", type = "radio", mandatory = true},
			};
			MethodsDict["SuspendLateLeitor"] = new List<MethodParameter> {
				new MethodParameter { name = "ID do leitor", type = "number", mandatory = true},
			};
			MethodsDict["GetRequisicoesLeitor"] = new List<MethodParameter> {
				new MethodParameter { name = "ID do leitor", type = "number", mandatory = true},
			};
			MethodsDict["Devolution"] = new List<MethodParameter>
			{
				new MethodParameter { name = "ID do leitor", type = "number", mandatory = true},
				new MethodParameter { name = "ID da obra a devolver", type = "number", mandatory = true},
				new MethodParameter { name = "Núcleo", type = "number", mandatory = true},
			};
			MethodsDict["Requisition"] = new List<MethodParameter>
			{
				new MethodParameter { name = "ID do leitor", type = "number", mandatory = true},
				new MethodParameter { name = "ID da obra a devolver", type = "number", mandatory = true},
				new MethodParameter { name = "Núcleo", type = "number", mandatory = true},
			};
			MethodsDict["sp_leitor_reactivate"] = new List<MethodParameter>
			{
				new MethodParameter { name = "ID do leitor", type = "number", mandatory = true},

			};
			MethodsDict["sp_delete_inactive_Leitor"] = null;
			MethodsDict["sp_del_leitor"] = new List<MethodParameter>
			{
				new MethodParameter { name = "ID do leitor", type = "number", mandatory = true},
			};
			MethodsDict["sp_save_leitor_history"] = new List<MethodParameter>
			{
				new MethodParameter { name = "ID do leitor", type = "number", mandatory = true},
			};
			MethodsDict["sp_cancel_leitor"] = new List<MethodParameter>
			{
				new MethodParameter { name = "ID do leitor", type = "number", mandatory = true},
			};
			MethodsDict["sp_search_Obra"] = new List<MethodParameter>
			{
				new MethodParameter { name = "Obra a pesquisar", type = "text", size = 50, mandatory = true},
			};

			MethodsDict["sp_search_Obra_genero"] = new List<MethodParameter>
			{
				new MethodParameter { name = "Género a pesquisar", type = "text", size = 50, mandatory = true},
			};

			MethodsDict["requesicao_status"] = new List<MethodParameter>
			{
				new MethodParameter { name = "ID do leitor", type = "number", mandatory = true},
				new MethodParameter { name = "Núcleo", type = "number", mandatory = false},

			};
		}
	}
}
