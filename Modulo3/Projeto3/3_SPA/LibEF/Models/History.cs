using System;
using System.Collections.Generic;

namespace LibEF.Models;

public partial class History
{
    public int PkLog { get; set; }

    public string? NomeObra { get; set; }

    public int? IdObra { get; set; }

    public string? Nucleo { get; set; }

    public DateTime? DataRequisicao { get; set; }

    public DateTime? DataDevolucao { get; set; }

    public string? NomeLeitor { get; set; }

    public int? IdLeitor { get; set; }
}
