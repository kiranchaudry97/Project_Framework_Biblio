using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Biblio_Web.Swagger
{
    public class AddLoginRequestExampleOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation == null || context == null)
                return;

            var path = context.ApiDescription.RelativePath ?? string.Empty;
            // target the token endpoint
            if (!path.TrimEnd('/').Equals("api/auth/token", System.StringComparison.OrdinalIgnoreCase))
                return;

            if (operation.RequestBody?.Content.ContainsKey("application/json") == true)
            {
                var example = new OpenApiObject
                {
                    ["email"] = new OpenApiString("admin@biblio.local"),
                    ["password"] = new OpenApiString("Admin123!")
                };

                operation.RequestBody.Content["application/json"].Example = example;
            }
        }
    }
}
