using System.Security.Claims;
using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Models.Api;
using MemoLens.Models.Api.Images;
using MemoLens.Models.Api.Memories;
using MemoLens.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemoLens.Controllers.Api.V1;

[ApiController]
[Route("api/v1")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class MemoryImagesController : ControllerBase
{
    private const long UploadRequestSizeLimitBytes = 55L * 1024 * 1024;

    private readonly ApplicationDbContext _context;
    private readonly IImageStorageService _imageStorageService;
    private readonly ICoverResolutionService _coverResolutionService;
    private readonly ILogger<MemoryImagesController> _logger;

    public MemoryImagesController(
        ApplicationDbContext context,
        IImageStorageService imageStorageService,
        ICoverResolutionService coverResolutionService,
        ILogger<MemoryImagesController> logger)
    {
        _context = context;
        _imageStorageService = imageStorageService;
        _coverResolutionService = coverResolutionService;
        _logger = logger;
    }

    [HttpPost("memories/{memoryId:int}/images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UploadRequestSizeLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadRequestSizeLimitBytes)]
    [ProducesResponseType(typeof(ApiResponse<UploadMemoryImagesResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UploadMemoryImagesResponse>>> Upload(
        int memoryId,
        [FromForm] UploadMemoryImagesRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidBearerToken();
        }

        var memory = await _context.Memories
            .Include(item => item.Images)
            .FirstOrDefaultAsync(item =>
                item.Id == memoryId &&
                item.UserId == userId &&
                !item.IsDeleted);
        if (memory is null)
        {
            return MemoryNotFound();
        }

        var files = request.Files.Where(file => file.Length > 0).ToList();
        var validationErrors = ValidateUpload(memory, request.Files, files);
        if (validationErrors.Count > 0)
        {
            return ValidationFailure(validationErrors);
        }

        var savedFiles = new List<ImageUploadResult>();
        var newImages = new List<MemoryImage>();
        var existingImageCount = memory.Images.Count;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var uploadedAt = DateTime.UtcNow;
            for (var index = 0; index < files.Count; index++)
            {
                var result = await _imageStorageService.SaveImageAsync(files[index], userId, memoryId);
                savedFiles.Add(result);

                var image = new MemoryImage
                {
                    MemoryId = memoryId,
                    ImagePath = result.ImagePath,
                    OriginalFileName = result.OriginalFileName,
                    UploadedAt = uploadedAt.AddTicks(index)
                };
                newImages.Add(image);
                _context.MemoryImages.Add(image);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception exception)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch (Exception rollbackException)
            {
                _logger.LogWarning(rollbackException, "Could not roll back image upload transaction.");
            }

            foreach (var savedFile in savedFiles)
            {
                TryDeleteFile(savedFile.ImagePath);
            }

            _logger.LogError(exception, "Could not upload images for memory {MemoryId}.", memoryId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
            {
                Success = false,
                Message = "Không thể lưu ảnh lúc này. Vui lòng thử lại."
            });
        }

        var totalImageCount = existingImageCount + newImages.Count;
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<UploadMemoryImagesResponse>
        {
            Success = true,
            Message = "Tải ảnh lên thành công.",
            Data = new UploadMemoryImagesResponse
            {
                Images = newImages.Select(ToMetadataResponse).ToList(),
                TotalImageCount = totalImageCount,
                RemainingSlots = _imageStorageService.MaxImagesPerMemory - totalImageCount
            }
        });
    }

    [HttpGet("images/{imageId:int}/content")]
    [Produces("image/jpeg", "image/png", "image/webp", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Content(int imageId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidBearerToken();
        }

        var image = await _context.MemoryImages
            .AsNoTracking()
            .Include(item => item.Memory)
            .FirstOrDefaultAsync(item =>
                item.Id == imageId &&
                item.Memory.UserId == userId &&
                !item.Memory.IsDeleted);
        if (image is null)
        {
            return ImageNotFound();
        }

        var filePath = _imageStorageService.ResolveImagePath(image.ImagePath);
        if (filePath is null || !System.IO.File.Exists(filePath))
        {
            return ImageNotFound();
        }

        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.Pragma = "no-cache";
        return PhysicalFile(
            filePath,
            _imageStorageService.GetContentType(image.ImagePath),
            enableRangeProcessing: true);
    }

    [HttpDelete("memories/{memoryId:int}/images/{imageId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> Delete(int memoryId, int imageId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidBearerToken();
        }

        var image = await _context.MemoryImages
            .Include(item => item.Memory)
            .FirstOrDefaultAsync(item =>
                item.Id == imageId &&
                item.MemoryId == memoryId &&
                item.Memory.UserId == userId &&
                !item.Memory.IsDeleted);
        if (image is null)
        {
            return ImageNotFound();
        }

        try
        {
            await _coverResolutionService.ClearCoverReferencesForImageAsync(image.Id);
            _context.MemoryImages.Remove(image);
            await _context.SaveChangesAsync();
            _imageStorageService.DeleteImageFile(image.ImagePath);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not delete image {ImageId} from memory {MemoryId}.", imageId, memoryId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
            {
                Success = false,
                Message = "Không thể xóa ảnh lúc này. Vui lòng thử lại."
            });
        }

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Xóa ảnh thành công."
        });
    }

    private Dictionary<string, string[]> ValidateUpload(
        Memory memory,
        IReadOnlyCollection<IFormFile> submittedFiles,
        IReadOnlyCollection<IFormFile> nonEmptyFiles)
    {
        var errors = new Dictionary<string, string[]>();

        if (submittedFiles.Count == 0 || nonEmptyFiles.Count == 0)
        {
            errors["files"] = ["Vui lòng chọn ít nhất một ảnh."];
            return errors;
        }

        if (nonEmptyFiles.Count != submittedFiles.Count)
        {
            errors["files"] = ["Tệp ảnh không được để trống."];
        }

        if (memory.Images.Count + nonEmptyFiles.Count > _imageStorageService.MaxImagesPerMemory)
        {
            errors["files"] = [$"Mỗi kỷ niệm chỉ được có tối đa {_imageStorageService.MaxImagesPerMemory} ảnh."];
        }

        var storageErrors = _imageStorageService.ValidateImages(nonEmptyFiles);
        if (storageErrors.Count > 0)
        {
            errors["files"] = errors.TryGetValue("files", out var existingErrors)
                ? existingErrors.Concat(storageErrors).ToArray()
                : storageErrors.ToArray();
        }

        return errors;
    }

    private void TryDeleteFile(string imagePath)
    {
        try
        {
            _imageStorageService.DeleteImageFile(imagePath);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not remove rolled-back image file.");
        }
    }

    private static MemoryImageResponse ToMetadataResponse(MemoryImage image)
    {
        return new MemoryImageResponse
        {
            Id = image.Id,
            OriginalFileName = image.OriginalFileName,
            UploadedAt = image.UploadedAt,
            ContentUrl = MemoryImageApiRoutes.Content(image.Id)
        };
    }

    private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private BadRequestObjectResult ValidationFailure(Dictionary<string, string[]> errors)
    {
        return BadRequest(new ApiValidationErrorResponse
        {
            Success = false,
            Message = "Dữ liệu gửi lên chưa hợp lệ.",
            Errors = errors
        });
    }

    private UnauthorizedObjectResult InvalidBearerToken()
    {
        return Unauthorized(new ApiResponse
        {
            Success = false,
            Message = "Bearer token không hợp lệ."
        });
    }

    private NotFoundObjectResult MemoryNotFound()
    {
        return NotFound(new ApiResponse
        {
            Success = false,
            Message = "Không tìm thấy kỷ niệm."
        });
    }

    private NotFoundObjectResult ImageNotFound()
    {
        return NotFound(new ApiResponse
        {
            Success = false,
            Message = "Không tìm thấy ảnh."
        });
    }
}
