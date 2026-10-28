using AutoMapper;
using EquiposAPI.Dtos;
using EquiposAPI.Models;

namespace EquiposAPI.Mapping;

// Reglas de AutoMapper: cómo convertir entidades <-> DTOs.
// Las propiedades con el mismo nombre se copian solas; aquí solo se
// configuran las que tienen un nombre o tipo distinto.
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Categorías
        CreateMap<Categoria, CategoriaListaDto>();
        CreateMap<CategoriaCrearDto, Categoria>();
        CreateMap<CategoriaActualizarDto, Categoria>();

        // Equipos
        CreateMap<Equipo, EquipoListaDto>()
            .ForMember(d => d.CategoriaNombre, o => o.MapFrom(s => s.Categoria.Nombre))
            // byte[] -> texto Base64 para que viaje en JSON.
            .ForMember(d => d.RowVersion, o => o.MapFrom(s => Convert.ToBase64String(s.RowVersion)));
        CreateMap<EquipoCrearDto, Equipo>();
        // La RowVersion no se copia: la asigna el controlador como "valor original".
        CreateMap<EquipoActualizarDto, Equipo>()
            .ForMember(d => d.RowVersion, o => o.Ignore());

        // Usuarios
        CreateMap<Usuario, UsuarioListaDto>();

        // Préstamos
        CreateMap<Prestamo, PrestamoListaDto>()
            .ForMember(d => d.CodigoInventario, o => o.MapFrom(s => s.Equipo.CodigoInventario))
            .ForMember(d => d.EquipoNombre, o => o.MapFrom(s => s.Equipo.Nombre))
            .ForMember(d => d.Username, o => o.MapFrom(s => s.Usuario.Username));
    }
}
