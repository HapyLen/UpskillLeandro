using System;
using System.Collections.Generic;

namespace LibEF.Models;

public partial class Requisicao
{
    public int PkLeitor { get; set; }

    public int PkObra { get; set; }

    public int PkNucleo { get; set; }

    public string? Stat { get; set; }

    public DateTime? DataLevantamento { get; set; }

    public DateTime? DataDevolucao { get; set; }

    public bool? AlreadySuspend { get; set; }

    public virtual Leitor PkLeitorNavigation { get; set; } = null!;

    public virtual Obra PkObraNavigation { get; set; } = null!;
}
