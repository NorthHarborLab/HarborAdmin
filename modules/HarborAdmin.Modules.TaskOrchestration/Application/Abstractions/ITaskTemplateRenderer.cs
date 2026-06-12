using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;

/// <summary>
/// 任务模板渲染器
/// </summary>
public interface ITaskTemplateRenderer
{
    /// <summary>
    /// 按任务执行上下文渲染模板
    /// </summary>
    /// <param name="template">模板文本</param>
    /// <param name="context">任务执行上下文</param>
    /// <returns>渲染后的文本</returns>
    string Render(string template, TaskExecutionContext context);
}
