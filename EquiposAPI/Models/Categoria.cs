namespace EquiposAPI.Models;

// Grupo de equipos (Laptops, Proyectores...). Relación: Categoria 1-N Equipo.
public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;   // Único, máximo 100 caracteres.

    public List<Equipo> Equipos { get; set; } = [];
}
