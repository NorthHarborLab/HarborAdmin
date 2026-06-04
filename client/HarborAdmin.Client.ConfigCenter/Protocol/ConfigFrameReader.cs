using System.Buffers.Binary;
using System.Net.Sockets;

namespace HarborAdmin.Client.ConfigCenter.Protocol;

/// <summary>
/// 从 <see cref="NetworkStream"/> 按长度前缀协议读取完整 JSON 帧
/// </summary>
public sealed class ConfigFrameReader
{
    /// <summary>
    /// 长度前缀缓冲区（4 字节）
    /// </summary>
    private readonly byte[] _lengthBuffer = new byte[4];

    /// <summary>
    /// 已读入的长度前缀字节数
    /// </summary>
    private int _lengthBytesRead;

    /// <summary>
    /// 当前帧 payload 缓冲区
    /// </summary>
    private byte[]? _payloadBuffer;

    /// <summary>
    /// 已读入的 payload 字节数
    /// </summary>
    private int _payloadBytesRead;

    /// <summary>
        /// 异步读取下一帧;连接关闭且未收到完整帧时返回 <see langword="null"/>
    /// </summary>
    /// <param name="stream">TCP 网络流</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="InvalidOperationException">帧长度非法(0 或超过 16MB)</exception>
    public async Task<ConfigMessage?> ReadFrameAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        while (true)
        {
            if (_payloadBuffer is null)
            {
                var read = await stream.ReadAsync(
                    _lengthBuffer.AsMemory(_lengthBytesRead, 4 - _lengthBytesRead),
                    cancellationToken);
                if (read == 0)
                {
                    return null;
                }

                _lengthBytesRead += read;
                if (_lengthBytesRead < 4)
                {
                    continue;
                }

                var length = BinaryPrimitives.ReadUInt32BigEndian(_lengthBuffer);
                if (length is 0 or > 16 * 1024 * 1024)
                {
                    throw new InvalidOperationException($"Invalid frame length: {length}");
                }

                _payloadBuffer = new byte[length];
                _payloadBytesRead = 0;
                _lengthBytesRead = 0;
            }

            var payloadRead = await stream.ReadAsync(
                _payloadBuffer.AsMemory(_payloadBytesRead, _payloadBuffer.Length - _payloadBytesRead),
                cancellationToken);
            if (payloadRead == 0)
            {
                return null;
            }

            _payloadBytesRead += payloadRead;
            if (_payloadBytesRead < _payloadBuffer.Length)
            {
                continue;
            }

            var message = ConfigMessage.FromPayload(_payloadBuffer);
            _payloadBuffer = null;
            _payloadBytesRead = 0;
            return message;
        }
    }
}
