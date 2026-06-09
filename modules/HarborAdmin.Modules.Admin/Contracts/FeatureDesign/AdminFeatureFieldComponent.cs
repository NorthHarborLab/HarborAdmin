namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign;

/// <summary>
/// 功能字段组件类型。
/// </summary>
public enum AdminFeatureFieldComponent : short
{
    /// <summary>
    /// 文本输入。
    /// </summary>
    Input,

    /// <summary>
    /// 数字输入。
    /// </summary>
    InputNumber,

    /// <summary>
    /// 下拉选择。
    /// </summary>
    Select,

    /// <summary>
    /// 开关。
    /// </summary>
    Switch,

    /// <summary>
    /// 日期选择。
    /// </summary>
    DatePicker,

    /// <summary>
    /// 多行文本。
    /// </summary>
    Textarea,

    /// <summary>
    /// 密码输入。
    /// </summary>
    InputPassword,

    /// <summary>
    /// API 下拉选择。
    /// </summary>
    ApiSelect,

    /// <summary>
    /// API 树形选择。
    /// </summary>
    ApiTreeSelect,
}
