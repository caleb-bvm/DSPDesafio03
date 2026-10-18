using AutoMapper;
using EquiposAPI.Dtos;
using EquiposAPI.Models;

namespace EquiposAPI.Mapping;

public class EquipoProfile : Profile
{
    public EquipoProfile()
    {
        CreateMap<Categoria, CategoriaListaDto>();
        CreateMap<CategoriaCrearDto, Categoria>();
        CreateMap<CategoriaActualizarDto, Categoria>();

        CreateMap<Equipo, EquipoListaDto>()
            .ForMember(destino => destino.CategoriaNombre,
                opcion => opcion.MapFrom(origen => origen.Categoria.Nombre))
            .ForMember(destino => destino.RowVersion,
                opcion => opcion.MapFrom(origen => Convert.ToBase64String(origen.RowVersion)));
        CreateMap<EquipoCrearDto, Equipo>();
        CreateMap<EquipoActualizarDto, Equipo>()
            .ForMember(destino => destino.RowVersion, opcion => opcion.Ignore());

        CreateMap<Usuario, UsuarioListaDto>();
        CreateMap<Prestamo, PrestamoListaDto>()
            .ForMember(destino => destino.CodigoInventario,
                opcion => opcion.MapFrom(origen => origen.Equipo.CodigoInventario))
            .ForMember(destino => destino.EquipoNombre,
                opcion => opcion.MapFrom(origen => origen.Equipo.Nombre))
            .ForMember(destino => destino.Username,
                opcion => opcion.MapFrom(origen => origen.Usuario.Username));
    }
}
