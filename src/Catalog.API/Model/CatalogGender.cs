using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.API.Model;

public class CatalogGender
{
    public int Id { get; set; }

    [Required]
    public string Gender { get; set; }
    public DateTime CreatedTimestamp { get; set; }
}
