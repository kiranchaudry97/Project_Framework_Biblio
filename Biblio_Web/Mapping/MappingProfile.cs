using AutoMapper;
using Biblio_Models.Entiteiten;

namespace Biblio_Web.Mapping
{
    // AutoMapper profile kept minimal since API uses entities directly.
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Keep simple mappings if needed elsewhere. No DTOs used for API.
            CreateMap<Boek, Boek>();
            CreateMap<Lid, Lid>();
            CreateMap<Categorie, Categorie>();
            CreateMap<Lenen, Lenen>();
        }
    }
}