using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace LibEF.Models;

public partial class ImageReference
{
    public int PkImage { get; set; }

    [JsonIgnore]
    public string? ImageName { get; set; }

    [JsonIgnore]
    public string? ImagePath { get; set; }

    public byte[]? ImageData { get; set; }
    [JsonIgnore]
    public virtual ICollection<Obra> Obras { get; set; } = new List<Obra>();
}
