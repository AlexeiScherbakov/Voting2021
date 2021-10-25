using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Voting2021.FilesUtils
{


	public class TransactionFileReader
		: IDisposable
	{
		private MemoryMappedFile _memoryMappedFile;
		private MemoryMappedViewStream _stream;
		public unsafe TransactionFileReader(string fileName)
		{
			_memoryMappedFile = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(fileName);
			_stream = _memoryMappedFile.CreateViewStream();
			byte* pointer = null;
			_stream.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				// Эта штука может вызывать BSOD на некоторых Windows 10
				//return;
				try
				{
					WIN32_MEMORY_RANGE_ENTRY range = new();
					range.VirtualAddress = new IntPtr(pointer);
					range.NumberOfBytes = new IntPtr(_stream.Length);
					int ok = PrefetchVirtualMemory(GetCurrentProcess(), 1, ref range, 0);
				}
				catch (Exception e)
				{

				}
			}
		}

		~TransactionFileReader()
		{
			Close();
		}

		public void Dispose()
		{
			Close();
			GC.SuppressFinalize(this);
		}

		private void Close()
		{
			_stream.SafeMemoryMappedViewHandle.ReleasePointer();
			_stream?.Dispose();
			_memoryMappedFile?.Dispose();
		}

		public bool Eof
		{
			get { return _stream.Position >= _stream.Length; }
		}



		public (string, byte[], Dictionary<string, string>) ReadRecord()
		{
			if (!SearchStartSequence())
			{
				return (null, null, null);
			}

			var bytic = _stream.ReadByte();
			switch (bytic)
			{
				case 0xBE:
					{
						using var binaryReader = new BinaryReader(_stream, Encoding.UTF8, true);
						int size = binaryReader.Read7BitEncodedInt();
						var value = binaryReader.ReadBytes(size);
						var dictionary = ReadDictionary();
						return (null, value, dictionary);
					}
					break;
				default:
					break;
			}
			return (null, null, null);
		}

		private bool SearchStartSequence()
		{
			int bytic;
			do
			{
				bytic = _stream.ReadByte();
				if (bytic != 0xCA)
				{
					continue;
				}
				bytic = _stream.ReadByte();
				if (bytic != 0xFE)
				{
					continue;
				}
				bytic = _stream.ReadByte();
				if (bytic != 0xBA)
				{
					continue;
				}
				return true;
			} while (bytic >= 0);
			return false;
		}


		private Dictionary<string, string> ReadDictionary()
		{
			int counter = 0;
			int pagePointer = 0;

			bool insideString = false;
			bool lastWasEscape = false;

			MemoryStream m = new MemoryStream(1024);
			do
			{
				var ch = _stream.ReadByte();
				m.WriteByte((byte) ch);
				switch (ch)
				{
					case '{':
						if (!insideString)
						{
							counter++;
						}
						break;
					case '}':
						if (!insideString)
						{
							counter--;
						}
						break;
					case '"':
						if (insideString)
						{
							if ((pagePointer > 0) && lastWasEscape)
							{
								//ignore
							}
							else
							{
								insideString = false;
							}
						}
						else
						{
							insideString = true;
						}
						break;
				}
				pagePointer++;
			} while ((counter > 0) && (_stream.Position < _stream.Length));
			m.Position = 0;
			Memory<byte> buffer = m.GetBuffer();
			var slicedBuffer = buffer.Slice(0, (int) m.Length);
			return JsonSerializer.Deserialize<Dictionary<string, string>>(slicedBuffer.Span);
		}


		[DllImport("kernel32.dll", EntryPoint = "PrefetchVirtualMemory")]
		private static extern int PrefetchVirtualMemory(IntPtr hProcess,
		  int NumberOfEntries,
		  ref WIN32_MEMORY_RANGE_ENTRY VirtualAddresses,
		  int Flags);

		[DllImport("kernel32.dll", EntryPoint = "GetCurrentProcess")]
		private static extern IntPtr GetCurrentProcess();

		private ref struct WIN32_MEMORY_RANGE_ENTRY
		{
			public IntPtr VirtualAddress;
			public IntPtr NumberOfBytes;
		}
	}
}
