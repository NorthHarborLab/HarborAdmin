using System.Net.Sockets;

namespace HarborAdmin.ConfigCenter.Client.Protocol;

/// <summary>
/// ConfigCenter TCP 客户端:连接、发送帧、接收帧
/// </summary>
public sealed class ConfigTcpClient : IAsyncDisposable
{
    /// <summary>
    /// 底层 TCP 客户端
    /// </summary>
    private TcpClient? _tcpClient;

    /// <summary>
    /// 网络流
    /// </summary>
    private NetworkStream? _stream;

    /// <summary>
    /// 帧读取器
    /// </summary>
    private readonly ConfigFrameReader _frameReader = new();

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected => _tcpClient?.Connected == true;

    /// <summary>
    /// 连接到 ConfigCenter 服务。
    /// </summary>
    /// <param name="host">主机名或 IP</param>
    /// <param name="port">端口</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await DisposeAsync();
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(host, port, cancellationToken);
        _stream = _tcpClient.GetStream();
    }

    /// <summary>
    /// 发送一条协议消息(自动编码为帧)
    /// </summary>
    /// <param name="message">消息体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="InvalidOperationException">未连接</exception>
    public async Task SendAsync(ConfigMessage message, CancellationToken cancellationToken = default)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Not connected.");
        }

        var frame = message.ToFrameBytes();
        await _stream.WriteAsync(frame, cancellationToken);
        await _stream.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// 接收一条协议消息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息体;连接关闭时可能为 <see langword="null"/></returns>
    /// <exception cref="InvalidOperationException">未连接</exception>
    public Task<ConfigMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Not connected.");
        }

        return _frameReader.ReadFrameAsync(_stream, cancellationToken);
    }

    /// <summary>
    /// 释放网络流与 TCP 客户端资源
    /// </summary>
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_stream is not null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }

        _tcpClient?.Dispose();
        _tcpClient = null;
    }
}
