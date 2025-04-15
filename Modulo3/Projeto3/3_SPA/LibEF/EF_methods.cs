using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
using LibEF.Models;
using System.IO.Pipes;

namespace LibEF
{
	public class EF_methods
	{
		private readonly ProjectoContext context;

		public EF_methods(ProjectoContext dbContext)
		{
			context = dbContext;
		}

		public List<dynamic> GetAllObras(string? search)
		{
			if (search != null)
			{
				return context.Obras.Where(o => o.NomeObra.Contains(search)).ToList<dynamic>();
			}
			return context.Obras.ToList<dynamic>();
		}

		public List<dynamic> GetAllNucleos(string? search)
		{
			if (search != null)
			{
				return context.Nucleos.Where(n => n.NomeNucleo.Contains(search)).ToList<dynamic>();
			}
			return context.Nucleos.ToList<dynamic>();
		}

		public List<dynamic> GetAllLeitores(string? search)
		{
			if (search != null)
			{
				return context.Leitors.Where(l => l.NomeLeitor.Contains(search)).ToList<dynamic>();
			}
			return context.Leitors.ToList<dynamic>();
		}

		public List<dynamic> GetAllRequisicoes(string? search)
		{
			var requisicoes = from r in context.Requisicaos
							  join o in context.Obras on r.PkObra equals o.PkObra
							  join l in context.Leitors on r.PkLeitor equals l.PkLeitor
							  join n in context.Nucleos on r.PkNucleo equals n.PkNucleo
							  select new
							  {
								  r.PkLeitor,
								  r.PkObra,
								  r.PkNucleo,
								  l.NomeLeitor,
								  o.NomeObra,
								  n.NomeNucleo,
								  r.DataLevantamento,
								  r.DataDevolucao,
								  r.Stat,
							  };
			var result = new List<dynamic>();
			foreach (var r in requisicoes)
			{
				int date_diff = 0;
				if (r.DataDevolucao.HasValue)
					date_diff = DateTime.Now.Subtract(r.DataDevolucao.Value).Days;
				string statusMessage = date_diff > 15 ? "ATRASO" :
									   date_diff > 12 ? "Devolução URGENTE" :
									   date_diff > 10 ? "Devolver em breve" : "Em dia";
				var requisicaoWithStatus = new
				{
					r.PkLeitor,
					r.PkObra,
					r.PkNucleo,
					r.NomeLeitor,
					r.NomeObra,
					r.NomeNucleo,
					r.DataLevantamento,
					r.DataDevolucao,
					r.Stat,
					StatusMessage = statusMessage
				};
				result.Add(requisicaoWithStatus);
			}
			if (search != null)
			{
				return result.Where(r => r.NomeObra.Contains(search)).ToList();
			}
			return result;
		}


		public int available_copies(int PkObra, int PkNucleo)
		{
			int count_in_nucleo = context.NucleoObras
						.Where(n => n.PkObra == PkObra && n.PkNucleo == PkNucleo)
						.Select(n => n.Quantidade)
						.FirstOrDefault();
			int count_in_requisitions = context.Requisicaos
						.Where(r => r.PkObra == PkObra && r.PkNucleo == PkNucleo && r.Stat == "borrowed")
						.Count();
			int available_copies = count_in_nucleo - count_in_requisitions;
			return available_copies;
		}

		public string GetPassWordbyLogin(string email)
		{
			try
			{
				var leitor = context.Leitors.FirstOrDefault(l => l.Email == email);
				if (leitor == null)
					throw new Exception("User not found");
				if (leitor.UserRole != "ADMIN")
					throw new Exception("User has no admin permitions");
				return leitor.UserPassword;
			}
			catch (Exception)
			{
				throw;
			}
		}


		/////////////////////
		//		1
		/////////////////////

		public int GetTotalObra()
		{
			return context.NucleoObras.Sum(no => no.Quantidade);
		}

		/////////////////////
		//		2
		/////////////////////

		public List<dynamic> GetTotalObraPorGenero()
		{
			var result = from g in context.Generos
						 from o in g.PkObras
						 join no in context.NucleoObras on o.PkObra equals no.PkObra
						 group no by g.NomeGenero into grouped
						 select new
						 {
							 NomeGenero = grouped.Key,
							 TotalQuantidade = grouped.Sum(n => n.Quantidade)
						 };
			return result.ToList<dynamic>();
		}

		/////////////////////
		//		3
		/////////////////////

		public List<dynamic> GetTopRequestedByTime(DateTime? startDate = null, DateTime? endDate = null)
		{
			var query = context.Requisicaos.Include(r => r.PkObraNavigation).AsQueryable();

			if (startDate.HasValue)
				query = query.Where(r => r.DataLevantamento >= startDate);
			if (endDate.HasValue)
				query = query.Where(r => r.DataLevantamento <= endDate);
			var result = query.GroupBy(r => new { r.PkObraNavigation.NomeObra, r.PkObra })
					.Select(g => new
					{
						NomeObra = g.Key.NomeObra,
						TimesRequested = g.Count()
					})
					.OrderByDescending(o => o.TimesRequested)
					.Take(10);
			return result.ToList<dynamic>();
		}

		/////////////////////
		//		4
		/////////////////////

		public List<dynamic> GetRequisicoesByNucleo(DateTime startDate, DateTime endDate)
		{
			var result = from r in context.Requisicaos
						 join n in context.Nucleos on r.PkNucleo equals n.PkNucleo
						 where r.DataLevantamento >= startDate && r.DataLevantamento <= endDate
						 group r by new { n.PkNucleo, n.NomeNucleo } into g
						 select new
						 {
							 NomeNucleo = g.Key.NomeNucleo,
							 TotalRequisicoes = g.Count()
						 };

			return result.OrderByDescending(r => r.TotalRequisicoes).ToList<dynamic>();
		}

		/////////////////////
		//		5
		/////////////////////

		public bool InsertObra(int? PkNucleo, string nomeObra, string isbn, string autor, string editora, int ano, string imagePath = null, int quantidade = 0)
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					var Obras = context.Obras;
					var Nucleos = context.Nucleos;
					var NucleoObras = context.NucleoObras;

					// Check for existing obra and nucleo in a single query
					// var existingObra = Obras.FirstOrDefault(o => o.Isbn == isbn);
					// if (existingObra != null)
					// 	throw new Exception("Error: obra already exists");
					// Synchronous database operations
					if (Obras.Any(o => o.Isbn == isbn))
						throw new Exception("Error: obra already exists");
					if (PkNucleo.HasValue && !Nucleos.Any(n => n.PkNucleo == PkNucleo))
						throw new Exception("Error: nucleo not found");
					if (!PkNucleo.HasValue)
						throw new Exception("Error: invalid nucleo");
					var obra = new Obra
					{
						NomeObra = nomeObra,
						Isbn = isbn,
						Editora = editora,
						Ano = ano
					};
					Obras.Add(obra);
					context.SaveChanges();
					var nucleoObra = new NucleoObra
					{
						PkNucleo = PkNucleo.Value,
						PkObra = obra.PkObra, //autoincremented
						Quantidade = quantidade
					};
					NucleoObras.Add(nucleoObra);
					context.SaveChanges();
					if (!string.IsNullOrEmpty(imagePath))
						UpdateImage(obra.PkObra, imagePath, isbn);
					transaction.Commit();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		private bool UpdateImage(int pkObra, string imagePath, string isbn)
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					var obra = context.Obras.FirstOrDefault(o => o.PkObra == pkObra);
					if (obra == null)
						throw new Exception("Obras not found.");
					var novaImagem = new ImageReference
					{
						ImagePath = imagePath,
						ImageName = isbn
					};
					context.ImageReferences.Add(novaImagem);
					context.SaveChanges();
					obra.FkImagem = novaImagem.PkImage;
					context.SaveChanges();
					transaction.Commit();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}


		/////////////////////
		//		6
		/////////////////////

		public bool AddObra(int pkObra, int PkNucleo, int quantidade)
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					if (!context.Obras.Any(o => o.PkObra == pkObra))
						throw new Exception("Error: obra not found");

					var nucleoObra = context.NucleoObras.FirstOrDefault(no => no.PkObra == pkObra && no.PkNucleo == PkNucleo);
					if (nucleoObra == null)
						throw new Exception("Error: obra not found in given nucleo");
					nucleoObra.Quantidade += quantidade;
					context.SaveChanges();
					transaction.Commit();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		public bool RemoveObra(int pkObra, int PkNucleo, int quantidade)
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					var nucleoObra = context.NucleoObras.FirstOrDefault(no => no.PkObra == pkObra && no.PkNucleo == PkNucleo);
					if (nucleoObra == null)
						throw new Exception("Error: obra not found in given nucleo");
					int existing_copies = available_copies(pkObra, PkNucleo);
					if (existing_copies < quantidade)
						throw new Exception("Error: insufficient Obras for request in given nucleo");
					nucleoObra.Quantidade -= quantidade;
					context.SaveChanges();
					transaction.Commit();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		/////////////////////
		//		7
		/////////////////////

		public bool AddObraInNucleo(int pkObra, int PkNucleo, int quantidade)
		{//TODO maybe not needed
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					var nucleoObraDestino = context.NucleoObras.FirstOrDefault(no => no.PkObra == pkObra && no.PkNucleo == PkNucleo);
					if (nucleoObraDestino != null)
					{
						AddObra(pkObra, PkNucleo, quantidade);
					}
					else
					{
						var novaNucleoObra = new NucleoObra
						{
							PkNucleo = PkNucleo,
							PkObra = pkObra,
							Quantidade = quantidade
						};
						context.NucleoObras.Add(novaNucleoObra);
						context.SaveChanges();
					}
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		public bool TransferObra(int pkObra, int PkNucleoOrigem, int PkNucleoDestino, int quantidade)
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					RemoveObra(pkObra, PkNucleoOrigem, quantidade);
					AddObraInNucleo(pkObra, PkNucleoDestino, quantidade);
					transaction.Commit();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		/////////////////////
		//		8
		/////////////////////

		public bool InsertLeitor(string nome, string? morada, string? telefone, string? email, string userPassword, string userRole = "USER")
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					if (context.Leitors.Any(l => l.Email == email))
						throw new Exception("matching leitor login already exists");
					if (context.Leitors.Any(l => l.NomeLeitor == nome && l.Morada == morada && l.Telefone == telefone))
						throw new Exception("matching leitor already exists");
					var leitor = new Leitor
					{
						NomeLeitor = nome,
						DataInscricao = DateTime.Now,
						//DataInscricao = DateTime.Now,
						Morada = morada,
						Telefone = telefone,
						Email = email,
						UserPassword = userPassword,
						UserRole = userRole,
						Stat = "active"
					};
					context.Leitors.Add(leitor);
					context.SaveChanges();
					transaction.Commit();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}



		public bool SuspendLateLeitor(int PkLeitor)
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					var leitor = context.Leitors.FirstOrDefault(l => l.PkLeitor == PkLeitor);
					if (leitor == null)
						throw new Exception("leitor not found");
					if (leitor != null && leitor.Stat == "suspended")
						throw new Exception("leitor is suspended");
					var leitor_requesitions = context.Requisicaos.Where(r => r.PkLeitor == PkLeitor);
					var countLate = leitor_requesitions.Where(r => CheckOvertime(r.DataLevantamento.Value, r.DataDevolucao.Value, 15)).Count();
					// removed r.DataDevolucao.HasValue && 
					if (countLate < 3)
					{
						transaction.Rollback();
						return false;
					}
					leitor.Stat = "suspended";
					context.SaveChanges();
					foreach (var requisition in leitor_requesitions)
					{
						if (requisition.Stat == "returned")
							requisition.AlreadySuspend = true;
					}
					context.SaveChanges();
					transaction.Commit();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		public bool SuspendLeitor(int PkLeitor)
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					var leitor = context.Leitors.FirstOrDefault(l => l.PkLeitor == PkLeitor);
					if (leitor == null)
						throw new Exception("leitor not found");
					if (leitor != null && leitor.Stat == "suspended")
						throw new Exception("leitor is suspended");
					leitor.Stat = "suspended";
					context.SaveChanges();
					transaction.Commit();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		public bool CheckOvertime(DateTime dataLevantamento, DateTime? dataDevolucao, int diasLimite)
		{
			if (!dataDevolucao.HasValue)
				dataDevolucao = DateTime.Now;
			var diff = (dataDevolucao.Value - dataLevantamento).Days;
			return diff > diasLimite;
		}

		public List<Requisicao> GetRequisicoesLeitor(int PkLeitor)
		{
			return context.Requisicaos.Where(r => r.PkLeitor == PkLeitor).ToList();
		}

		/////////////////////
		//		bonus
		/////////////////////

		public bool Devolution(int PkLeitor, int pkObra, int nucleo)
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					var leitor = context.Leitors.FirstOrDefault(l => l.PkLeitor == PkLeitor);
					if (leitor == null)
						throw new Exception("leitor not found");
					var requisition = context.Requisicaos.FirstOrDefault(r => r.PkLeitor == PkLeitor && r.PkObra == pkObra && r.PkNucleo == nucleo);
					if (requisition == null)
						throw new Exception("requisition not found");
					requisition.Stat = "returned";
					requisition.DataDevolucao = DateTime.Now;
					context.SaveChanges();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		public bool Requisition(int PkLeitor, int pkObra, int PkNucleo)
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					var leitor = context.Leitors.FirstOrDefault(l => l.PkLeitor == PkLeitor);
					if (leitor == null)
						throw new Exception("leitor not found");
					var obra = context.Obras.FirstOrDefault(o => o.PkObra == pkObra);
					if (obra == null)
						throw new Exception("obra not found");
					var nucleo = context.Nucleos.FirstOrDefault(n => n.PkNucleo == PkNucleo);
					if (nucleo == null)
						throw new Exception("nucleo not found");
					SuspendLateLeitor(PkLeitor);
					if (leitor.Stat == "suspended")
						throw new Exception("leitor is suspended");
					if (available_copies(pkObra, PkNucleo) < 2)
						throw new Exception("no available copies");
					var leitor_requisitions = context.Requisicaos.Where(r => r.PkLeitor == PkLeitor && r.Stat == "borrowed").ToList();
					if (leitor_requisitions.Any(r => r.PkLeitor == PkLeitor && r.PkObra == pkObra))
						throw new Exception("leitor already requested this obra");
					if (leitor_requisitions.Count() == 4)
						throw new Exception("leitor has 4 requesitions already");
					var requisition = new Requisicao
					{
						PkLeitor = PkLeitor,
						PkObra = pkObra,
						PkNucleo = PkNucleo,
						DataLevantamento = DateTime.Now
					};
					context.Requisicaos.Add(requisition);
					context.SaveChanges();
					transaction.Commit();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}



		/////////////////////
		//		10
		/////////////////////

		public bool sp_leitor_reactivate(int PkLeitor)
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					var leitor = context.Leitors.FirstOrDefault(l => l.PkLeitor == PkLeitor);
					if (leitor == null)
						throw new Exception("leitor not found");
					if (leitor.Stat != "suspended")
						throw new Exception("leitor is not suspended");
					leitor.Stat = "active";
					context.SaveChanges();
					transaction.Commit();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		/////////////////////
		//		11
		/////////////////////

		public bool sp_delete_inactive_Leitor(DateTime? date_since)
		{// TODO: not correct i am only deleting people that have done rquesitions and are inactive
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					int days_since;
					if (date_since == null)
						days_since = 365;
					else
						days_since = DateTime.Now.Day;
					// linq commands will be efficiently combined by the provider
					var requesitions = context.Requisicaos
						.Where(r => DateTime.Now.Subtract(r.DataLevantamento.Value).Days > days_since)
						.GroupBy(r => r.PkLeitor);
					foreach (var leitores in requesitions)
						sp_del_leitor(leitores.Key);
					transaction.Commit();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		public bool sp_del_leitor(int PkLeitor)
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					var leitor = context.Leitors.FirstOrDefault(l => l.PkLeitor == PkLeitor);
					if (leitor == null)
						throw new Exception("leitor not found");
					sp_save_leitor_history(PkLeitor);
					context.Leitors.Remove(leitor);
					context.SaveChanges();
					transaction.Commit();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		public bool sp_save_leitor_history(int PkLeitor)
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					var leitor = context.Leitors.FirstOrDefault(l => l.PkLeitor == PkLeitor);
					if (leitor == null)
						throw new Exception("leitor not found");
					// Perform the insert into the History table
					var historyEntries = from r in context.Requisicaos
										 where (r.PkLeitor == PkLeitor)
										 join o in context.Obras on r.PkObra equals o.PkObra
										 join no in context.NucleoObras on o.PkObra equals no.PkObra
										 join n in context.Nucleos on no.PkNucleo equals n.PkNucleo
										 select new History
										 { //same time wedo select wedo create new object
											 NomeObra = o.NomeObra,
											 IdObra = r.PkObra,
											 Nucleo = n.NomeNucleo,
											 DataRequisicao = r.DataLevantamento,
											 DataDevolucao = r.DataDevolucao,
											 NomeLeitor = leitor.NomeLeitor,
											 IdLeitor = leitor.PkLeitor
										 };
					if (historyEntries.Any())
					{
						// if i want to add single entrie
						// context.LeitorHistorie.Add(leitor_history);
						context.Histories.AddRange(historyEntries);
						context.SaveChanges();
					}
					transaction.Commit();
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		// 1. fazer registo do leitor

		// 2. cancelar a inscrição de um leitor

		public bool sp_cancel_leitor(int PkLeitor)
		{
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					var leitor = context.Leitors.FirstOrDefault(l => l.PkLeitor == PkLeitor);
					if (leitor == null)
						throw new Exception("leitor not found");
					var leitor_requisitions = context.Requisicaos.Where(r => r.PkLeitor == PkLeitor);
					var undelivered_requisitions = leitor_requisitions.Where(r => r.Stat == "borrowed");
					foreach (var requisition in undelivered_requisitions)
						Devolution(PkLeitor, requisition.PkObra, requisition.PkNucleo);
					sp_del_leitor(PkLeitor);
					return true;
				}
				catch (Exception)
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		// 3. search for Obras by name

		private int tryParseInt(string input)
		{
			int result;
			if (int.TryParse(input, out result))
			{
				return result;
			}
			return -1;
		}

		public List<Leitor> sp_search_Leitores(string leitorName)
		{
			var by_name = context.Leitors.Where(n => n.NomeLeitor.Contains(leitorName));
			return by_name.ToList();
		}

		public List<Obra> sp_search_Obra(string obraName)
		{
			var by_name = context.Obras.Where(n => n.NomeObra.Contains(obraName));
			return by_name.ToList();

		}

		public List<Nucleo> sp_search_Nucleo(string nucleoName)
		{
			var by_name = context.Nucleos.Where(n => n.NomeNucleo.Contains(nucleoName));
			return by_name.ToList();
		}

		public List<dynamic> sp_search_Request(string? obraName, string? nucleoName, string? leitorName)
		{
			var requisicoes = from r in context.Requisicaos
							  join o in context.Obras on r.PkObra equals o.PkObra
							  where o.NomeObra.Contains(obraName)
							  join n in context.Nucleos on r.PkNucleo equals n.PkNucleo
							  where n.NomeNucleo.Contains(nucleoName)
							  join l in context.Leitors on r.PkLeitor equals l.PkLeitor
							  where l.NomeLeitor.Contains(leitorName)
							  select new
							  {
								  r.PkLeitor,
								  l.NomeLeitor,
								  r.PkObra,
								  o.NomeObra,
								  r.PkNucleo,
								  n.NomeNucleo,
								  r.DataLevantamento,
								  r.DataDevolucao,
								  r.Stat
							  };

			return requisicoes.ToList<dynamic>();
		}

		public List<dynamic> requesicao_status(int PkLeitor, int? PkNucleo)
		{
			var leitoresFiltradas = from l in context.Leitors
									where l.PkLeitor == PkLeitor
									select l;
			var requisicoesFiltradas = from l in context.Requisicaos
									   where l.Stat == "borrowed"
									   select l;
			var query = from l in leitoresFiltradas
						join r in requisicoesFiltradas on l.PkLeitor equals r.PkLeitor
						join o in context.Obras on r.PkObra equals o.PkObra
						join no in context.NucleoObras on o.PkObra equals no.PkObra
						join n in context.Nucleos on no.PkNucleo equals n.PkNucleo // Get Núcleo Name
						select new // anonymous type
						{
							PkLeitor = l.PkLeitor,
							NomeLeitor = l.NomeLeitor,
							NomeObra = o.NomeObra,
							PkNucleo = n.PkNucleo,
							NomeNucleo = n.NomeNucleo,
							DataLevantamento = r.DataLevantamento.Value,
							DataDevolucao = r.DataDevolucao.Value,
							StatusMessage = ""
						};

			// Apply filter if PkNucleo is provided
			if (PkNucleo.HasValue)
			{
				query = query.Where(x => x.PkNucleo == PkNucleo.Value);
			}

			// Convert to List and Set Status Messages
			var result = query.ToList().Select(r =>
			{
				int date_diff = DateTime.Now.Subtract(r.DataDevolucao).Days;
				return new
				{
					r.PkLeitor,
					r.NomeLeitor,
					r.NomeObra,
					r.PkNucleo,
					r.NomeNucleo,
					r.DataLevantamento,
					r.DataDevolucao,
					StatusMessage = date_diff > 15 ? "ATRASO" :
									date_diff > 12 ? "Devolução URGENTE" :
									date_diff > 10 ? "Devolver em breve" : "Em dia"
				};
			}).ToList<dynamic>();

			return result;
		}



		// public async Task<bool> InsertObraAsync(int? PkNucleo, string nomeObra, string isbn, string autor, string editora, int ano, string imagePath = null, int quantidade = 0)
		// {
		// 	using (var transaction = await Database.BeginTransactionAsync())
		// 	{
		// 		try
		// 		{
		// 			// Asynchronous database operations
		// 			if (await Obras.AnyAsync(o => o.Isbn == isbn))
		// 				throw new Exception("Error: obra already exists");
		// 			if (PkNucleo.HasValue && !await Nucleos.AnyAsync(n => n.PkNucleo == PkNucleo))
		// 				throw new Exception("Error: nucleo not found");
		// 			if (!PkNucleo.HasValue)
		// 				PkNucleo = await GetCentralNucleoAsync();
		// 			var obra = new Obras
		// 			{
		// 				NomeObra = nomeObra,
		// 				Isbn = isbn,
		// 				Editora = editora,
		// 				Ano = ano
		// 			};
		// 			Obras.Add(obra);
		// 			await SaveChangesAsync();
		// 			var nucleoObra = new NucleoObras
		// 			{
		// 				PkNucleo = PkNucleo.Value,
		// 				PkObra = obra.PkObra,
		// 				Quantidade = quantidade
		// 			};
		// 			NucleoObras.Add(nucleoObra);
		// 			await SaveChangesAsync();
		// 			if (!string.IsNullOrEmpty(imagePath))
		// 			{
		// 				await UpdateImageAsync(obra.PkObra, imagePath, isbn);
		// 			}
		// 			await transaction.CommitAsync();
		// 			return true;
		// 		}
		// 		catch (Exception)
		// 		{
		// 			await transaction.RollbackAsync();
		// 			throw;
		// 		}
		// 	}
		// }

	}
}
