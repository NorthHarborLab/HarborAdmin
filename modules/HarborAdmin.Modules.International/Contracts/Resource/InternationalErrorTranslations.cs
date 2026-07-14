namespace HarborAdmin.Modules.International.Contracts.Resource;

/// <summary>
/// 全局业务错误码翻译。
/// </summary>
public static class InternationalErrorTranslations
{
    /// <summary>
    /// 错误语言包在运行时资源清单中的虚拟路径。
    /// </summary>
    public const string BundlePath = "__errors";

    /// <summary>
    /// 错误语言包版本；翻译内容变化时必须递增。
    /// </summary>
    public const int BundleVersion = 2;

    /// <summary>
    /// 按语言组织的错误码翻译目录。
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Catalog { get; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            ["zh-CN"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["PLATFORM.REQUEST.VALIDATION_FAILED"] = "请求校验失败。",
                ["PLATFORM.RESOURCE.NOT_FOUND"] = "资源不存在。",
                ["PLATFORM.RESOURCE.CONFLICT"] = "资源状态冲突。",
                ["PLATFORM.AUTH.UNAUTHORIZED"] = "未登录或登录已过期。",
                ["PLATFORM.AUTH.FORBIDDEN"] = "无权执行此操作。",
                ["PLATFORM.BUSINESS.FAILED"] = "业务处理失败。",
                ["PLATFORM.SYSTEM.INTERNAL_ERROR"] = "服务器内部错误，请稍后重试。",
                ["AI.BUSINESS.NOT_FOUND"] = "AI 业务 {id} 不存在。",
                ["AI.BUSINESS.INVALID_INPUT"] = "AI 业务输入无效：{reason}",
                ["AI.BUSINESS.DUPLICATE_KEY"] = "AI 业务 Key {businessKey} 已存在。",
                ["AI.KNOWLEDGE_BASE.NOT_FOUND"] = "AI 知识库 {id} 不存在。",
                ["AI.KNOWLEDGE_BASE.INVALID_INPUT"] = "AI 知识库输入无效：{reason}",
                ["AI.KNOWLEDGE_BASE.DUPLICATE_KEY"] = "AI 知识库 Key {knowledgeKey} 已存在。",
                ["AI.PROMPT.NOT_FOUND"] = "AI Prompt {id} 不存在。",
                ["AI.PROMPT.INVALID_INPUT"] = "AI Prompt 输入无效：{reason}",
                ["AI.PROMPT.DUPLICATE_VERSION"] = "AI Prompt {promptKey} 的版本 {version} 已存在。",
                ["AI.PROVIDER.NOT_FOUND"] = "AI 供应商 {id} 不存在。",
                ["AI.PROVIDER.INVALID_INPUT"] = "AI 供应商输入无效：{reason}",
                ["AI.PROVIDER.SECRET_UNAVAILABLE"] = "供应商密钥引用 {secretRef} 不存在或未启用。",
                ["AI.PROVIDER.MODEL_REQUIRED"] = "至少需要配置一个供应商模型。",
                ["AI.PROVIDER.DUPLICATE_KEY"] = "AI 供应商 Key {providerKey} 已存在。",
                ["AI.MODEL_QUOTA.NOT_FOUND"] = "AI 模型限额 {id} 不存在。",
                ["AI.MODEL_QUOTA.INVALID_INPUT"] = "AI 模型限额输入无效：{reason}",
                ["AI.MODEL_QUOTA.DUPLICATE_SCOPE"] = "AI 模型限额作用域已存在：{providerKey}/{modelName}/{businessKey}/{producerKey}",
                ["ADMIN.DEPT.NOT_FOUND"] = "部门 {id} 不存在。",
                ["ADMIN.DEPT.PARENT_NOT_FOUND"] = "上级部门 {parentId} 不存在。",
                ["ADMIN.DEPT.INVALID_PARENT"] = "上级部门不能是当前部门或其下级部门。",
                ["ADMIN.DEPT.HAS_CHILDREN"] = "部门 {id} 存在下级部门，请先删除下级部门。",
                ["ADMIN.DEPT.HAS_USERS"] = "部门 {id} 下存在用户，不能删除。",
                ["ADMIN.DEPT.DUPLICATE_CODE"] = "部门编码 {deptCode} 已存在。",
                ["ADMIN.ROLE.NOT_FOUND"] = "角色 {id} 不存在。",
                ["ADMIN.ROLE.MENU_NOT_FOUND"] = "角色绑定的菜单不存在：{ids}",
                ["ADMIN.ROLE.PERMISSION_NOT_FOUND"] = "角色绑定的权限不存在：{permissionCode}",
                ["ADMIN.ROLE.DUPLICATE_CODE"] = "角色编码 {roleCode} 已存在。",
            },
            ["en-US"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["PLATFORM.REQUEST.VALIDATION_FAILED"] = "Request validation failed.",
                ["PLATFORM.RESOURCE.NOT_FOUND"] = "The requested resource was not found.",
                ["PLATFORM.RESOURCE.CONFLICT"] = "The resource state conflicts with this operation.",
                ["PLATFORM.AUTH.UNAUTHORIZED"] = "Your session is missing or has expired.",
                ["PLATFORM.AUTH.FORBIDDEN"] = "You are not allowed to perform this operation.",
                ["PLATFORM.BUSINESS.FAILED"] = "The operation could not be completed.",
                ["PLATFORM.SYSTEM.INTERNAL_ERROR"] = "An internal server error occurred. Please try again later.",
                ["AI.BUSINESS.NOT_FOUND"] = "AI business {id} was not found.",
                ["AI.BUSINESS.INVALID_INPUT"] = "AI business input is invalid: {reason}",
                ["AI.BUSINESS.DUPLICATE_KEY"] = "AI business key {businessKey} already exists.",
                ["AI.KNOWLEDGE_BASE.NOT_FOUND"] = "AI knowledge base {id} was not found.",
                ["AI.KNOWLEDGE_BASE.INVALID_INPUT"] = "AI knowledge base input is invalid: {reason}",
                ["AI.KNOWLEDGE_BASE.DUPLICATE_KEY"] = "AI knowledge base key {knowledgeKey} already exists.",
                ["AI.PROMPT.NOT_FOUND"] = "AI Prompt {id} was not found.",
                ["AI.PROMPT.INVALID_INPUT"] = "AI Prompt input is invalid: {reason}",
                ["AI.PROMPT.DUPLICATE_VERSION"] = "AI Prompt {promptKey} version {version} already exists.",
                ["AI.PROVIDER.NOT_FOUND"] = "AI provider {id} was not found.",
                ["AI.PROVIDER.INVALID_INPUT"] = "AI provider input is invalid: {reason}",
                ["AI.PROVIDER.SECRET_UNAVAILABLE"] = "Provider secret reference {secretRef} does not exist or is disabled.",
                ["AI.PROVIDER.MODEL_REQUIRED"] = "At least one provider model is required.",
                ["AI.PROVIDER.DUPLICATE_KEY"] = "AI provider key {providerKey} already exists.",
                ["AI.MODEL_QUOTA.NOT_FOUND"] = "AI model quota {id} was not found.",
                ["AI.MODEL_QUOTA.INVALID_INPUT"] = "AI model quota input is invalid: {reason}",
                ["AI.MODEL_QUOTA.DUPLICATE_SCOPE"] = "AI model quota scope already exists: {providerKey}/{modelName}/{businessKey}/{producerKey}",
                ["ADMIN.DEPT.NOT_FOUND"] = "Department {id} was not found.",
                ["ADMIN.DEPT.PARENT_NOT_FOUND"] = "Parent department {parentId} was not found.",
                ["ADMIN.DEPT.INVALID_PARENT"] = "The parent department cannot be the current department or one of its descendants.",
                ["ADMIN.DEPT.HAS_CHILDREN"] = "Department {id} has child departments and cannot be deleted.",
                ["ADMIN.DEPT.HAS_USERS"] = "Department {id} contains users and cannot be deleted.",
                ["ADMIN.DEPT.DUPLICATE_CODE"] = "Department code {deptCode} already exists.",
                ["ADMIN.ROLE.NOT_FOUND"] = "Role {id} was not found.",
                ["ADMIN.ROLE.MENU_NOT_FOUND"] = "One or more role menus were not found: {ids}",
                ["ADMIN.ROLE.PERMISSION_NOT_FOUND"] = "Role permission was not found: {permissionCode}",
                ["ADMIN.ROLE.DUPLICATE_CODE"] = "Role code {roleCode} already exists.",
            },
        };
}
