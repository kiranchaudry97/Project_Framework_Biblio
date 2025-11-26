using AutoMapper;
using Biblio_Models.Entiteiten;

namespace Biblio_Web.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // No ApiModels in use anymore. Keep minimal mappings if needed.
            // Example: map between entities if desired (here we ignore navigation properties by default)
            CreateMap<Boek, Boek>().ForMember(dest => dest.categorie, opt => opt.Ignore());
            CreateMap<Lid, Lid>();
            CreateMap<Categorie, Categorie>();
            CreateMap<Lenen, Lenen>().ForMember(dest => dest.Boek, opt => opt.Ignore()).ForMember(dest => dest.Lid, opt => opt.Ignore());
        }
    }
}