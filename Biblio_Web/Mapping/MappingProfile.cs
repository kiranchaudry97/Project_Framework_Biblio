using AutoMapper;
using Biblio_Web.ApiModels;
using Biblio_Models.Entiteiten;

namespace Biblio_Web.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Boek, BoekDto>()
                .ForMember(dest => dest.CategorieNaam, opt => opt.MapFrom(src => src.categorie != null ? src.categorie.Naam : null));

            CreateMap<Lid, LidDto>();

            CreateMap<Categorie, CategorieDto>();

            CreateMap<Lenen, UitleningDto>()
                .ForMember(dest => dest.BoekTitel, opt => opt.MapFrom(src => src.Boek != null ? src.Boek.Titel : null))
                .ForMember(dest => dest.LidNaam, opt => opt.MapFrom(src => src.Lid != null ? src.Lid.Voornaam + " " + src.Lid.AchterNaam : null));

            // reverse maps for creating/updating from DTOs if needed
            CreateMap<BoekDto, Boek>().ForMember(dest => dest.categorie, opt => opt.Ignore());
            CreateMap<LidDto, Lid>();
            CreateMap<CategorieDto, Categorie>();
            CreateMap<UitleningDto, Lenen>().ForMember(dest => dest.Boek, opt => opt.Ignore()).ForMember(dest => dest.Lid, opt => opt.Ignore());
        }
    }
}