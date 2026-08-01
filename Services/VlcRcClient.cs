using System.Net.Sockets;
using System.Text;

namespace VideoLibrarySystemVlc.Services;

public sealed class VlcRcClient : IDisposable
{
	private readonly string host;
	private readonly int port;
	private TcpClient? client;
	private NetworkStream? stream;
	private bool disposed;

	public VlcRcClient(string host, int port)
	{
		this.host = host;
		this.port = port;
	}

	public bool IsConnected => client?.Connected == true;

	public async Task<bool> ConnectAsync(int timeoutMs = 5000, CancellationToken cancellationToken = default)
	{
		if (disposed)
		{
			return false;
		}

		try
		{
			client = new TcpClient();
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(timeoutMs);

			await client.ConnectAsync(host, port, cts.Token);

			if (client.Connected)
			{
				stream = client.GetStream();
				// Clear welcome message
				await ReadResponseAsync(cancellationToken);
				return true;
			}
		}
		catch (Exception)
		{
			client?.Dispose();
			client = null;
			stream = null;
		}

		return false;
	}

	public async Task<int?> GetCurrentTimeSecondsAsync(CancellationToken cancellationToken = default)
	{
		if (!IsConnected || stream is null)
		{
			return null;
		}

		try
		{
			var response = await SendCommandAsync("get_time", cancellationToken);
			if (string.IsNullOrWhiteSpace(response))
			{
				return null;
			}

			// VLC returns time in seconds as a number
			if (int.TryParse(response.Trim(), out var seconds))
			{
				return seconds;
			}
		}
		catch (Exception)
		{
			// Connection might be closed
		}

		return null;
	}

	public async Task<int?> GetLengthSecondsAsync(CancellationToken cancellationToken = default)
	{
		if (!IsConnected || stream is null)
		{
			return null;
		}

		try
		{
			var response = await SendCommandAsync("get_length", cancellationToken);
			if (string.IsNullOrWhiteSpace(response))
			{
				return null;
			}

			if (int.TryParse(response.Trim(), out var seconds))
			{
				return seconds;
			}
		}
		catch (Exception)
		{
			// Connection might be closed
		}

		return null;
	}

	private async Task<string?> SendCommandAsync(string command, CancellationToken cancellationToken)
	{
		if (!IsConnected || stream is null)
		{
			return null;
		}

		try
		{
			var commandBytes = Encoding.UTF8.GetBytes($"{command}\n");
			await stream.WriteAsync(commandBytes, cancellationToken);
			await stream.FlushAsync(cancellationToken);

			return await ReadResponseAsync(cancellationToken);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private async Task<string?> ReadResponseAsync(CancellationToken cancellationToken)
	{
		if (stream is null)
		{
			return null;
		}

		try
		{
			var buffer = new byte[1024];
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(2000); // 2 second timeout for reading

			var bytesRead = await stream.ReadAsync(buffer, cts.Token);
			if (bytesRead > 0)
			{
				var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
				// Remove VLC prompt (> ) from response
				response = response.Replace(">", "").Trim();
				return response;
			}
		}
		catch (Exception)
		{
			// Timeout or connection closed
		}

		return null;
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		disposed = true;
		stream?.Dispose();
		client?.Dispose();
	}
}
