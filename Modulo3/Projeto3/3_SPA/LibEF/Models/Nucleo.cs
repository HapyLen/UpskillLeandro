using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LibEF.Models;

public partial class Nucleo
{
    public int PkNucleo { get; set; }

    public string NomeNucleo { get; set; } = null!;

    public string? Morada { get; set; }

    public string? Telefone { get; set; }

    [JsonIgnore]
    public virtual ICollection<NucleoObra> NucleoObras { get; set; } = new List<NucleoObra>();
}
