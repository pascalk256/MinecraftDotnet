using Minecraft;
using Minecraft.Data.Generated;
using Minecraft.Packets.Play.ClientBound;
using Minecraft.Registry;
using Minecraft.Schemas.Chunks;
using System.Globalization;

namespace Sniffcraft.PaketLog.Deserializer;

public class Program
{
	public static void Main(string[] args)
	{
		// DEBUG
		// Parser.ParseLine("0x00 0x86 0x06 0x09 0x31 0x32 0x37 0x2E 0x30 0x2E 0x30 0x2E 0x31 0x63 0xDD 0x02");
		// <DEBUG>

		string path;
		if (args.Length != 0)
		{
			path = args[0];
		}
		else
		{
			Console.WriteLine($"Enter file PathName:");
			path = Console.ReadLine() ?? string.Empty;
		}

		Console.WriteLine($"Reading File '{path}'...");
		if (!File.Exists(path))
		{
			Console.WriteLine($"File does not exist");
			return;
		}

		string[] lines = File.ReadAllLines(path);
		Console.WriteLine($"Found {lines.Length} lines");

		// Ignore non hex lines
		IEnumerable<string> packetLines = lines.Where(d => d.StartsWith("0x"));

		MinecraftRegistry reg = VanillaRegistry.Data;

		foreach (string line in packetLines) 
		{
			byte[] bytes = ParseLine(line);
			byte id = bytes[0];

			// Decode your Packet here.
			// Example: ClientBoundChunkDataAndUpdateLightPacket
			if (id == 0x2C)
			{
				using DataReader reader = new(bytes[1..]);
				ClientBoundChunkDataAndUpdateLightPacket packet = (ClientBoundChunkDataAndUpdateLightPacket)ClientBoundChunkDataAndUpdateLightPacket
					.Deserialiser
					.Invoke(reader, reg);

				if (packet.ChunkZ > 25 && packet.ChunkX > 25)
				{
					int count = 0;
					foreach (ChunkSection section in packet.Data.Sections)
					{
						foreach (uint item in section.Blocks)
						{
							if (item != 0)
								count++;
						}
					}

					Console.WriteLine($"Chunk: {packet.ChunkX}|{packet.ChunkZ}: {count} blocks that are not air ({bytes.Length / 1024} KiB)");
				}
			}
		}
	}

	private static byte[] ParseLine(string line)
	{
		// Calculate size
		// 0x00 0x86 0x06 0x09 0x31 0x32 0x37 0x2E 0x30 0x2E 0x30 0x2E 0x31 0x63 0xDD 0x02
		// 5 char per entry (-1 because the last one has no space)
		int expectedBytes = (line.Length + 1) / 5;

		Span<byte> buffer = stackalloc byte[expectedBytes];

		int bufferIndex = 0;
		for (int i = 0; i < line.Length; i += 5)
		{
			if (line[i] != '0' || line[i + 1] != 'x')
				throw new FormatException($"Invalid hex entry in: {line}");

			buffer[bufferIndex++] = byte.Parse(
				line.AsSpan(i + 2, 2),
				NumberStyles.HexNumber
			);
		}

		return buffer[..bufferIndex].ToArray();
	}
}
