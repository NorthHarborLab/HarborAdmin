using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.AI.Controllers;

/// <summary>
/// AI 密钥管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/secrets")]
public sealed class AiSecretsController(AiManagementService service) : ControllerBase
{
    /// <summary>
    /// 列出密钥。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AiSecretDto>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListSecretsAsync(cancellationToken));

    /// <summary>
    /// 保存或轮换密钥。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AiSecretDto>> Save([FromBody] SaveAiSecretRequest request, CancellationToken cancellationToken) =>
        Ok(await service.SaveSecretAsync(request, cancellationToken));
}
