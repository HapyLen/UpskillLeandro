using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LibEF.Models;

public partial class Obra
{
    public int PkObra { get; set; }

    public string NomeObra { get; set; } = null!;

    public string? Isbn { get; set; }

    public string? Editora { get; set; }

    public int Ano { get; set; }
    [JsonIgnore]

    public int? FkImagem { get; set; }
    [JsonIgnore]

    public virtual ImageReference? FkImagemNavigation { get; set; }

    [JsonIgnore]
    public virtual ICollection<NucleoObra> NucleoObras { get; set; } = new List<NucleoObra>();
    [JsonIgnore]

    public virtual ICollection<Requisicao> Requisicaos { get; set; } = new List<Requisicao>();
    [JsonIgnore]

    public virtual ICollection<Autor> PkAutors { get; set; } = new List<Autor>();
    [JsonIgnore]

    public virtual ICollection<Genero> PkGeneros { get; set; } = new List<Genero>();
}
