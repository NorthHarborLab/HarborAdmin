using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;
using HarborAdmin.Modules.TaskOrchestration.Domain.Entities;
using Mapster;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Mappings;

/// <summary>
/// 任务编排模块 Mapster 映射配置
/// </summary>
public sealed class TaskOrchestrationMappingRegister : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<OrchestrationTask, OrchestrationTaskListItemDto>()
            .Map(destination => destination.TriggerCount, source => source.Triggers.Count)
            .Map(destination => destination.NodeCount, source => source.Nodes.Count);

        config.NewConfig<OrchestrationTask, OrchestrationTaskDto>();
        config.NewConfig<OrchestrationTaskTrigger, OrchestrationTaskTriggerDto>();
        config.NewConfig<OrchestrationTaskNode, OrchestrationTaskNodeDto>();
        config.NewConfig<OrchestrationTaskEdge, OrchestrationTaskEdgeDto>();
        config.NewConfig<OrchestrationNodeRun, OrchestrationNodeRunDto>();
        config.NewConfig<OrchestrationTaskRun, OrchestrationTaskRunDto>()
            .Map(destination => destination.Nodes, _ => Array.Empty<OrchestrationNodeRunDto>());

        config.NewConfig<TaskCallableDescriptor, TaskCallableDescriptorDto>()
            .Map(destination => destination.RequestType, source => source.RequestType.FullName ?? source.RequestType.Name)
            .Map(destination => destination.ResponseType, source => source.ResponseType.FullName ?? source.ResponseType.Name);
    }
}
