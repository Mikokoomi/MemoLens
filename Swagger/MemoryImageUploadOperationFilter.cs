using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MemoLens.Swagger;

public sealed class MemoryImageUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var routeTemplate = context.ApiDescription.RelativePath;
        if (!string.Equals(context.ApiDescription.HttpMethod, HttpMethods.Post, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(routeTemplate, "api/v1/memories/{memoryId}/images", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Summary = "Tải ảnh riêng tư cho một kỷ niệm";
        operation.Description =
            "Gửi multipart/form-data với field `files`. Hỗ trợ JPG/JPEG/PNG/WEBP, " +
            "tối đa 5 MB mỗi file và tối đa 10 ảnh cho một kỷ niệm. Yêu cầu JWT Bearer.";

        if (operation.RequestBody?.Content.TryGetValue("multipart/form-data", out var multipart) == true)
        {
            multipart.Schema.Description = "Một hoặc nhiều file ảnh trong field `files`.";
        }
    }
}
