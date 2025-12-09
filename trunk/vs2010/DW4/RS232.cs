using System;

using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace AQ3
{
	/// <summary>
	/// Summary description for RS232.
	/// </summary>

	public class RS232 : IDisposable
	{
		/// <summary>
		///  kernel side constants
		/// </summary>
		public const int INVALID_HANDLE_VALUE = -1;
		public const uint GENERIC_READ = 0x80000000;
		public const uint GENERIC_WRITE = 0x40000000;

		public const int CREATE_NEW        =  1;
		public const int CREATE_ALWAYS     =  2;
		public const int OPEN_EXISTING     =  3;
		public const int OPEN_ALWAYS       =  4;
		public const int TRUNCATE_EXISTING =  5;		

		//from winnt.h
		public const uint FILE_ATTRIBUTE_READONLY             =0x00000001;  
		public const uint FILE_ATTRIBUTE_HIDDEN               =0x00000002;  
		public const uint FILE_ATTRIBUTE_SYSTEM               =0x00000004;  
		public const uint FILE_ATTRIBUTE_DIRECTORY            =0x00000010;  
		public const uint FILE_ATTRIBUTE_ARCHIVE              =0x00000020;   
		public const uint FILE_ATTRIBUTE_DEVICE               =0x00000040;   
		public const uint FILE_ATTRIBUTE_NORMAL               =0x00000080;   
		public const uint FILE_ATTRIBUTE_TEMPORARY            =0x00000100;   
		public const uint FILE_ATTRIBUTE_SPARSE_FILE          =0x00000200;   
		public const uint FILE_ATTRIBUTE_REPARSE_POINT        =0x00000400;   
		public const uint FILE_ATTRIBUTE_COMPRESSED           =0x00000800;   
		public const uint FILE_ATTRIBUTE_OFFLINE              =0x00001000;   
		public const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED  =0x00002000;  
		public const uint FILE_ATTRIBUTE_ENCRYPTED            =0x00004000;

		// from winbase.h
		public const uint EV_RXCHAR           =0x0001;  // Any Character received
		public const uint EV_RXFLAG           =0x0002;  // Received certain character
		public const uint EV_TXEMPTY          =0x0004;  // Transmitt Queue Empty
		public const uint EV_CTS              =0x0008;  // CTS changed state
		public const uint EV_DSR              =0x0010;  // DSR changed state
		public const uint EV_RLSD             =0x0020;  // RLSD changed state
		public const uint EV_BREAK            =0x0040;  // BREAK received
		public const uint EV_ERR              =0x0080;  // Line status error occurred
		public const uint EV_RING             =0x0100;  // Ring signal detected
		public const uint EV_PERR             =0x0200;  // Printer error occured
		public const uint EV_RX80FULL         =0x0400;  // Receive buffer is 80 percent full
		public const uint EV_EVENT1           =0x0800;  // Provider specific event 1
		public const uint EV_EVENT2           =0x1000;  // Provider specific event 2

		[StructLayout(LayoutKind.Sequential)]
			public struct DCB 
		{
			public int DCBlength;		// sizeof(DCB) 
			public uint BaudRate;		// current baud rate
			public uint flags;
			public ushort wReserved;	// not currently used 
			public ushort XonLim;		// transmit XON threshold 
			public ushort XoffLim;		// transmit XOFF threshold 
			public byte ByteSize;		// number of bits/byte, 4-8 
			public byte Parity;			// 0-4=no,odd,even,mark,space 
			public byte StopBits;		// 0,1,2 = 1, 1.5, 2 
			public char XonChar;		// Tx and Rx XON character 
			public char XoffChar;		// Tx and Rx XOFF character 
			public char ErrorChar;		// error replacement character 
			public char EofChar;		// end of input character 
			public char EvtChar;		// received event character 
			public ushort wReserved1;	// reserved; do not use 
		}

		[StructLayout(LayoutKind.Sequential)]
			private struct COMMTIMEOUTS 
		{  
			public int ReadIntervalTimeout; 
			public int ReadTotalTimeoutMultiplier; 
			public int ReadTotalTimeoutConstant; 
			public int WriteTotalTimeoutMultiplier; 
			public int WriteTotalTimeoutConstant; 
		}

		[StructLayout(LayoutKind.Sequential)]	
			public struct OVERLAPPED 
		{ 
			public int Internal; 
			public int InternalHigh; 
			public int Offset; 
			public int OffsetHigh; 
			public int hEvent; 
		} 

		[DllImport("KERNEL32.DLL", EntryPoint="GetCommTimeouts", SetLastError=true)]
		private static extern bool GetCommTimeouts(
			int hFile,						// handle to comm device
			ref COMMTIMEOUTS lpCommTimeouts // time-out values
			);
		
		[DllImport("KERNEL32.DLL", EntryPoint="SetCommTimeouts", SetLastError=true)]	
		private static extern bool SetCommTimeouts(
			int hFile,						// handle to comm device
			ref COMMTIMEOUTS lpCommTimeouts // time-out values
			);

		[DllImport("KERNEL32.DLL", EntryPoint="GetCommState", SetLastError=true)]
		protected static extern bool GetCommState(int hFile, ref DCB lpDCB);

		[DllImport("KERNEL32.DLL", EntryPoint="SetCommState", SetLastError=true)]	
		protected static extern bool SetCommState(int hFile, ref DCB lpDCB);

		[DllImport("KERNEL32.DLL", EntryPoint="SetCommMask", SetLastError=true)]	
		protected static extern bool SetCommMask(int hFile, uint dwEvtMask);

		[DllImport("KERNEL32.DLL", EntryPoint="WaitCommEvent", SetLastError=true)]	
		protected static extern bool WaitCommEvent(int hFile, ref uint lpEvtMask, ref OVERLAPPED lpOverlapped);

		[DllImport("KERNEL32.DLL", EntryPoint="CloseHandle", SetLastError=true)]	
		protected static extern bool CloseHandle(int hObject);

		[DllImport("KERNEL32.DLL", EntryPoint="SetupComm", SetLastError=true)]	
		protected static extern bool SetupComm(int hFile, uint dwInQueue, uint dwOutQueue);

		[DllImport("KERNEL32.DLL", EntryPoint="CreateFile", SetLastError=true)]	
		protected static extern int CreateFile(
			string lpFileName,				// file name
			uint dwDesiredAccess,			// access mode
			int dwShareMode,				// share mode
			int lpSecurityAttributes,		// SD
			uint dwCreationDisposition,		// how to create
			uint dwFlagsAndAttributes,		// file attributes
			int hTemplateFile				// handle to template file
			);

		[DllImport("kernel32.dll", EntryPoint="ReadFile", SetLastError=true)]
		protected static extern bool ReadFile(
			int hFile,						// handle to file
			byte[] lpBuffer,				// data buffer
			int nNumberOfBytesToRead,		// number of bytes to read
			ref int lpNumberOfBytesRead,	// number of bytes read
			ref OVERLAPPED lpOverlapped		// overlapped buffer
			);

		[DllImport("KERNEL32.DLL", EntryPoint="WriteFile", SetLastError=true)]	
		protected static extern bool WriteFile(
			int hFile,						// handle to file
			byte[] lpBuffer,				// data buffer
			int nNumberOfBytesToWrite,		// number of bytes to write
			ref int lpNumberOfBytesWritten,	// number of bytes written
			ref OVERLAPPED lpOverlapped		// overlapped buffer
			);

		private int m_nPort;
		int m_PortHandle;
		int m_nReadDelay = 100;
		int m_readErrorCount = 0;
		bool m_checkTimeout = false;

		public bool CheckTimeout
		{
			set{ m_checkTimeout = value; }
		}

		public RS232(int nPort)
		{
			m_nPort = nPort;

			// open COM port for read and write
			string strPort = "COM";
			strPort += m_nPort;
			m_PortHandle = CreateFile(strPort,
				GENERIC_READ|GENERIC_WRITE,
				0,
				0,
				OPEN_EXISTING,
				FILE_ATTRIBUTE_NORMAL,
				0);

			if (m_PortHandle == INVALID_HANDLE_VALUE) 
			{
				throw (new Exception("Could not open COM Port"));
			}

			// events to monitor
			SetCommMask(m_PortHandle, EV_TXEMPTY);

			// set queue size
			SetupComm(m_PortHandle, 4096, 4096);

			// get comm timeouts
			COMMTIMEOUTS ctoCommPort = new COMMTIMEOUTS();	
			GetCommTimeouts(m_PortHandle, ref ctoCommPort);

			// set comm timeouts
			ctoCommPort.ReadIntervalTimeout = 100;
			ctoCommPort.ReadTotalTimeoutConstant = 1000;
			ctoCommPort.ReadTotalTimeoutMultiplier = 20;
			ctoCommPort.WriteTotalTimeoutMultiplier = 20;
			ctoCommPort.WriteTotalTimeoutConstant = 100;  
			SetCommTimeouts(m_PortHandle, ref ctoCommPort);

			// get curent state
			DCB dcb = new DCB();
			GetCommState(m_PortHandle, ref dcb);

			// alter current state to suit the AQ3
			dcb.BaudRate = 9600;
			dcb.Parity = 0;		// 0 = none
			dcb.StopBits = 0;	// 0 = 1
			dcb.ByteSize = 8;

			// set new state
			SetCommState(m_PortHandle, ref dcb);
		}

		public void Dispose()
		{
			CloseHandle(m_PortHandle);
		}

		public byte[] Read(int NumBytes) 
		{
			byte[] BufBytes;
			byte[] OutBytes;
			BufBytes = new byte[NumBytes];
			if (m_PortHandle != INVALID_HANDLE_VALUE) 
			{
				OVERLAPPED ovlCommPort = new OVERLAPPED();
				int BytesRead = 0;
				ReadFile(m_PortHandle, BufBytes, NumBytes, ref BytesRead, ref ovlCommPort);
				if (BytesRead == 0)
				{
					m_readErrorCount++;
					if( m_readErrorCount > 3 || !m_checkTimeout )
					{
						throw (new Exception("Could not read data from COM port"));
					}
				}
				OutBytes = new byte[BytesRead];
				Array.Copy(BufBytes,OutBytes,BytesRead);
			} 
			else 
			{
				throw (new Exception("COM Port Not Open"));
			}
			return OutBytes;
		}

		public void Write(byte[] WriteBytes) 
		{
			if (m_PortHandle != INVALID_HANDLE_VALUE) 
			{
				OVERLAPPED ovlCommPort = new OVERLAPPED();
				int BytesWritten = 0;
				WriteFile(m_PortHandle, WriteBytes, WriteBytes.Length, ref BytesWritten, ref ovlCommPort);
				uint ComEvent = EV_TXEMPTY;
				OVERLAPPED ovlCommPortEvent = new OVERLAPPED();
				WaitCommEvent(m_PortHandle, ref ComEvent, ref ovlCommPortEvent);

				if (BytesWritten == 0)
				{
					throw (new Exception("Could not write data to COM port"));
				}
			}
			else 
			{
				throw (new Exception("COM Port Not Open"));
			}		
		}

		public bool GetDeviceCodes(out byte DeviceCode1, out byte DeviceCode2, out byte DeviceCode3)
		{
			DeviceCode1 = 0;
			DeviceCode2 = 0;
			DeviceCode3 = 0;

			byte[] escape = new byte[1];
			escape[0] = 27;
			byte[] command = new byte[3];
			command[0] = (byte)'/';
			command[1] = (byte)'C';
			command[2] = (byte)';';
			
			// write command
			Write(escape);
			Write(command);

			//Thread.Sleep(m_nReadDelay);
			
			// read response
			byte[] response = Read(3);
			DeviceCode1 = response[0];
			DeviceCode2 = response[1];
			DeviceCode3 = response[2];

			return true;
		}

		public string GetFirmwareText()
		{
			string strResponse = "";

			byte[] escape = new byte[1];
			escape[0] = 27;
			byte[] command = new byte[3];
			command[0] = (byte)'/';
			command[1] = (byte)'D';
			command[2] = (byte)';';
			
			// write command
			Write(escape);
			Write(command);

			//Thread.Sleep(m_nReadDelay);
			
			// read response
			byte[] response = Read(128);
			for (int i = 0; i < response.Length; i++)
			{
				strResponse += (char)response[i];
			}

			return strResponse;			
		}

		public string GetSerialNumber()
		{
			string strResponse = "";

			byte[] escape = new byte[1];
			escape[0] = 27;
			byte[] command = new byte[3];
			command[0] = (byte)'/';
			command[1] = (byte)'G';
			command[2] = (byte)';';
				
			// write command
			Write(escape);
			Write(command);

			//Thread.Sleep(m_nReadDelay);
				
			// read response
			byte[] response = Read(128);
			for (int i = 0; i < response.Length; i++)
			{
				strResponse += (char)response[i];
			}

			return strResponse;
		}

		public void PutAck()
		{
			byte[] ack = new byte[1];
			ack[0] = 6;				
			Write(ack);	
		}

		public byte[,] GetFile()
		{
			int nProgramCount = 99 + 1;
			int nBlockSize = 256;
			byte[,] file = new byte[nProgramCount, nBlockSize];

			int nBlock = 0;

			try
			{	
				byte[] escape = new byte[1];
				escape[0] = 27;
				byte[] command = new byte[3];
				command[0] = (byte)'/';
				command[1] = (byte)'U';
				command[2] = (byte)';';
				
				// write command
				Write(escape);
				Write(command);

				//Thread.Sleep(m_nReadDelay);
				
				// read response
				//int nBlock = 0;
				byte[] uploading = Read(4);
				while (uploading[2] != 'Z')
				{
					byte[] response = Read(256);
					int MyChecksum = 0;
					for (int i = 0; i < response.Length; i++)
					{
						file[nBlock, i] = response[i];
						MyChecksum += response[i];
					}
					MyChecksum = -MyChecksum;
					byte MyByteChecksum = (byte)MyChecksum;
					byte[] checksum = Read(1);

					PutAck();
					nBlock++;
					uploading = Read(4);
				}

				return file;
			}
			catch (Exception exception)
			{
				throw (exception);
			}
		}

		public bool PutFile(byte[,] file)
		{
			int nTimeOut = 3000;
			int nTotalBlocks = file.Length / 256;

			try
			{
				// reset all programs
				byte[] escape = new byte[1];
				escape[0] = 27;
				byte[] command = new byte[3];
				command[0] = (byte)'/';
				command[1] = (byte)'R';
				command[2] = (byte)';';
			
				// write reset command
				Write(escape);
				Write(command);

				// wait for reset response
				bool bResetOK = false;
				int nWaitedSecs = 0;
				while (nWaitedSecs < nTimeOut && !bResetOK)
				{
					//MessageBox.Show("Wait ack");
					byte[] ack = Read(1);
					if (ack.Length > 0 && ack[0] == 6)
					{
						//MessageBox.Show("Reset Done!");
						bResetOK = true;
					}
					else
					{
						Thread.Sleep(m_nReadDelay);
						nWaitedSecs += m_nReadDelay;
					}
				}

				// write new blocks
				for (int nBlock = 0; nBlock < nTotalBlocks; nBlock++)
				{
					byte[] block = new byte[256];
					byte[] checksum = new byte[1];
					int checksumTemp = 0;
					for (int i = 0; i < 256; i++)
					{
						block[i] = file[nBlock, i];
						checksumTemp += file[nBlock, i];
					}
					checksumTemp = -checksumTemp;
					checksum[0] = (byte)checksumTemp;
					// write block
					escape[0] = 27;
					command[0] = (byte)'/';
					command[1] = (byte)'A';
					command[2] = (byte)';';
					Write(escape);
					Write(command);
					Write(block);
					Write(checksum);

					// wait for block write response
					bool bBlockWriteOK = false;
					nWaitedSecs = 0;
					while (nWaitedSecs < nTimeOut && !bBlockWriteOK)
					{
						//MessageBox.Show("Wait ack");
						byte[] ack = Read(1);
						if (ack.Length > 0 && ack[0] == 6)
						{
							bBlockWriteOK = true;
						}
						else
						{
							Thread.Sleep(m_nReadDelay);
							nWaitedSecs += m_nReadDelay;
						}
					}
					if (bBlockWriteOK)
					{
						//MessageBox.Show("Write Block Done!");
					}
					else
					{
						//MessageBox.Show("Write Block Failed!");
					}
				}

				// end of transmission
				escape[0] = 27;
				command[0] = (byte)'/';
				command[1] = (byte)'z';
				command[2] = (byte)';';
				Write(escape);
				Write(command);

				return true;
			}
			catch(Exception exception)
			{
				throw (exception);
			}
		}
	}
}
