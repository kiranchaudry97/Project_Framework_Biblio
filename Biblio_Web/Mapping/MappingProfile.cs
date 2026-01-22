using AutoMapper;
using Biblio_Models.Entiteiten;

namespace Biblio_Web.Mapping
{
    // AutoMapper profiel
    // Bevat mappingconfiguratie voor de applicatie
    // Momenteel minimaal omdat de API rechtstreeks entities gebruikt
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Self-to-self mappings
            // Worden gebruikt om entiteiten te kopiëren (clonen)
            // of om later eenvoudig DTO-mappings toe te voegen

            CreateMap<Boek, Boek>();
            CreateMap<Lid, Lid>();
            CreateMap<Categorie, Categorie>();
            CreateMap<Lenen, Lenen>();
        }
    }
}
