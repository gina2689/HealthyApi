﻿using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HealthyApi.Helpers
{
    public class FileUploadOperation : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParams = context.ApiDescription.ParameterDescriptions
                .Where(p => p.Type == typeof(IFormFile))
                .ToList();

            if (fileParams.Count > 0)
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content =
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = fileParams.ToDictionary(
                                    p => p.Name,
                                    p => new OpenApiSchema { Type = "string", Format = "binary" }
                                ),
                                Required = fileParams.Select(p => p.Name).ToHashSet()
                            }
                        }
                    }
                };
            }
        }
    }
}
