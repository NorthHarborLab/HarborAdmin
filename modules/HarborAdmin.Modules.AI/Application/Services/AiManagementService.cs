using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.EventBus;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Application.Services;

/// <summary>
/// AI 管理服务。
/// </summary>
public sealed partial class AiManagementService
{
    private readonly IAiRepository repository;
    private readonly IAiDbContext dbContext;
    private readonly DbEntityRegistry entityRegistry;
    private readonly UnitOfWorkManagerCloud unitOfWorkManager;
    private readonly ISecretStore secretStore;
    private readonly IEventPublisher eventPublisher;
    private readonly IHarborMapper mapper;

    /// <summary>
    /// 初始化 AI 管理服务。
    /// </summary>
    public AiManagementService(
        IAiRepository repository,
        IAiDbContext dbContext,
        DbEntityRegistry entityRegistry,
        UnitOfWorkManagerCloud unitOfWorkManager,
        ISecretStore secretStore,
        IEventPublisher eventPublisher,
        IHarborMapper mapper)
    {
        this.repository = repository;
        this.dbContext = dbContext;
        this.entityRegistry = entityRegistry;
        this.unitOfWorkManager = unitOfWorkManager;
        this.secretStore = secretStore;
        this.eventPublisher = eventPublisher;
        this.mapper = mapper;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };
}
