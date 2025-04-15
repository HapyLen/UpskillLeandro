using System;
using System.Collections.Generic;

namespace LibEF.Models;

public partial class Genero
{
    public int PkGenero { get; set; }

    public string NomeGenero { get; set; } = null!;

    public virtual ICollection<Obra> PkObras { get; set; } = new List<Obra>();
}
