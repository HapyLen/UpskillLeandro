using System;
using System.Collections.Generic;

namespace LibEF.Models;

public partial class Autor
{
    public int PkAutor { get; set; }

    public string NomeAutor { get; set; } = null!;

    public virtual ICollection<Obra> PkObras { get; set; } = new List<Obra>();
}
