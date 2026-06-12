using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;

namespace HarborAdmin.TaskWorker.Callables;

/// <summary>
/// Callable 插件入口
/// </summary>
/// <param name="ImplementationType">实现类</param>
/// <param name="Descriptor">接口描述</param>
internal sealed record CallablePluginEntry(Type ImplementationType, TaskCallableDescriptor Descriptor);
