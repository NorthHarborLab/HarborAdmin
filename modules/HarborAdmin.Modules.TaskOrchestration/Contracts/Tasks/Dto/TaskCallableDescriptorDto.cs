namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;

/// <summary>
/// 可调用接口方法 DTO
/// </summary>
/// <param name="FullClassName">Callable 实现完整类名</param>
/// <param name="ServiceKey">服务键</param>
/// <param name="MethodKey">方法键</param>
/// <param name="DisplayName">显示名称</param>
/// <param name="RequestType">请求类型名称</param>
/// <param name="ResponseType">响应类型名称</param>
public sealed record TaskCallableDescriptorDto(string FullClassName, string ServiceKey, string MethodKey, string DisplayName, string RequestType, string ResponseType);
