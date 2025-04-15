using System;
using System.Collections.Generic;

namespace LibEF.Models;

public partial class NucleoObra
{
    public int PkNucleo { get; set; }

    public int PkObra { get; set; }

    public int Quantidade { get; set; }

    public virtual Nucleo PkNucleoNavigation { get; set; } = null!;

    public virtual Obra PkObraNavigation { get; set; } = null!;
}
