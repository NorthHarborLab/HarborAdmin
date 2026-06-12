namespace HarborAdmin.Modules.TaskOrchestration.Contracts.TaskContext;

/// <summary>
/// 可调用接口方法描述
/// </summary>
/// <param name="ServiceKey">服务键</param>
/// <param name="MethodKey">方法键</param>
/// <param name="DisplayName">显示名称</param>
/// <param name="RequestType">请求类型</param>
/// <param name="ResponseType">响应类型</param>
public sealed record TaskCallableDescriptor(string ServiceKey, string MethodKey, string DisplayName, Type RequestType, Type ResponseType);
