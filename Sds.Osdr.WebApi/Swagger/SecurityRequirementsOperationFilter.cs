using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.OpenApi.Models;

namespace Sds.Osdr.WebApi.Swagger
{
    public class SecurityRequirementsOperationFilter : IOperationFilter
    {
        private readonly AuthorizationOptions authorizationOptions;

        public SecurityRequirementsOperationFilter(IOptions<AuthorizationOptions> authorizationOptions)
        {
            this.authorizationOptions = authorizationOptions?.Value ?? throw new ArgumentNullException(nameof(authorizationOptions));
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var requiredPolicies = context.MethodInfo
                .GetCustomAttributes(true)
                .Concat(context.MethodInfo.DeclaringType.GetCustomAttributes(true))
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.Policy)
                .Distinct();

            var requiredScopes = requiredPolicies.Select(p => authorizationOptions.GetPolicy(p))
                .SelectMany(r => r.Requirements.OfType<ClaimsAuthorizationRequirement>())
                .Where(cr => cr.ClaimType == "scope")
                .SelectMany(r => r.AllowedValues)
                .Distinct()
                .ToList();

            if (requiredScopes.Any())
            {
                 operation.Responses.Add("401", new OpenApiResponse  { Description = "Unauthorized - correct token needed" });

                 var bearerScheme = new OpenApiSecurityScheme
            {
                     Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                 };

                 operation.Security = new List<OpenApiSecurityRequirement>
                 {
                     new OpenApiSecurityRequirement
                     {
                         [ bearerScheme ] = requiredScopes
                     }
                 };
            }
        }
    }
}
