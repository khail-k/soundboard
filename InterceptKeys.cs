using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using NAudio.Wave;
using System.Collections.Generic;
using System.IO;

namespace soundBoardTorb
{
    class InterceptKeys {
		private const int WH_KEYBOARD_LL = 13;
		private const int WM_KEYDOWN = 0x0100;
		private static LowLevelKeyboardProc _proc = HookCallback;
		private static IntPtr _hookID = IntPtr.Zero;

		private static List<int> outputDeviceList = new List<int>();
		private static IDictionary<int, string> keyMaps = new Dictionary<int, string>(); // should be <Keys, string>
		private static int exitButton;

		public static void Main() {

			print_devices();
			ParseConfig();

			_hookID = SetHook(_proc);
			Application.Run();
			UnhookWindowsHookEx(_hookID);

		}

		private static void print_devices() {
			Console.WriteLine("---- Devices ----");
			Console.WriteLine("\t");
			Console.WriteLine("WaveOut capabilities:");
			for (int n = -1; n < WaveOut.DeviceCount; n++)
			{
				var caps = WaveOut.GetCapabilities(n);
				Console.WriteLine($"{n}: {caps.ProductName}");
			}
			Console.WriteLine("\t");
			Console.WriteLine("WaveIn capabilities:");
			for (int n = -1; n < WaveIn.DeviceCount; n++)
			{
				var caps = WaveIn.GetCapabilities(n);
				Console.WriteLine($"{n}: {caps.ProductName}");
			}
			Console.WriteLine("\t");
			Console.WriteLine("Direct sound out devices:");
			foreach (var dev in DirectSoundOut.Devices)
			{
				Console.WriteLine($"{dev.Guid} {dev.ModuleName} {dev.Description}");
			}
			Console.WriteLine("\t");
			Console.WriteLine("Asio driver names:");
			foreach (var asio in AsioOut.GetDriverNames())
			{
				Console.WriteLine(asio);
			}
			Console.WriteLine("\t");
			Console.WriteLine("-----------------");
			Console.WriteLine("\t");
		}

		private static void ParseConfig() {
			string[] lines = System.IO.File.ReadAllLines(Directory.GetCurrentDirectory() + @"\config.txt");

			foreach (string line in lines) {

				if (line.Contains("#")) {
					continue;
				}

				if (line.Contains("outputKey:")) {
					string[] parsed = line.Split(' ');
					outputDeviceList.Add(Convert.ToInt32(parsed[1]));

				}

				if (line.Contains("Linkkey:")) {
					string[] parsed = line.Split(' ');
					keyMaps.Add((int)(Keys)Enum.Parse(typeof(Keys), parsed[1], true), parsed[2]);

				}

				if (line.Contains("stopKey:")) {
					string[] parsed = line.Split(' ');
					exitButton = (int)(Keys)Enum.Parse(typeof(Keys), parsed[1], true);
				}
			}

			Console.WriteLine("---- Config read ----");
			Console.WriteLine("\t");
			Console.WriteLine("Outpint device read:");
			foreach (int item in outputDeviceList)
			{
				var caps = WaveOut.GetCapabilities(item);
				Console.WriteLine(item + " " + caps.ProductName);
			}

			Console.WriteLine("\t");
			Console.WriteLine("key mappings read:");
			foreach (KeyValuePair<int, string> item in keyMaps)
			{
				Console.WriteLine((Keys)item.Key + " -> " + item.Value);
			}

			Console.WriteLine("\t");
			Console.WriteLine("Stop key set:");
			Console.WriteLine((Keys)exitButton);

			Console.WriteLine("\t");
			Console.WriteLine("----------------------");
			Console.WriteLine("\t");
		}

		private static IntPtr SetHook(LowLevelKeyboardProc proc) {
			using (Process curProcess = Process.GetCurrentProcess())
			using (ProcessModule curModule = curProcess.MainModule) {
				return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
					GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		private delegate IntPtr LowLevelKeyboardProc(	
			int nCode, IntPtr wParam, IntPtr lParam);

		private static IntPtr HookCallback(
			int nCode, IntPtr wParam, IntPtr lParam) {
			string currentDir = Directory.GetCurrentDirectory();
			if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) {
				int vkCode = Marshal.ReadInt32(lParam);
				Console.WriteLine((Keys)vkCode); // -- here to see button ID

				if (keyMaps.ContainsKey(vkCode)) {
					foreach (int device in outputDeviceList) {
						play(device, keyMaps[vkCode]);
                    }
				}

				if (vkCode == exitButton)
				{
					stop();
				}

			}
			return CallNextHookEx(_hookID, nCode, wParam, lParam);
		}

		private static WaveOut waveOut;
		private static List<WaveOut> waveOutList = new List<WaveOut>();
		private static AudioFileReader audioFileReader;
		private static List<AudioFileReader> audioFileReaderList = new List<AudioFileReader>();
		//https://github.com/naudio/NAudio/issues/156
		public static void play(int audioDeviceNumber, string audioUrl) {
			waveOut = new WaveOut();
			waveOutList.Add(waveOut);
			audioFileReader = new AudioFileReader(audioUrl);
			audioFileReaderList.Add(audioFileReader);
			waveOutList[(waveOutList.Count - 1)].DeviceNumber = audioDeviceNumber;
			waveOutList[(waveOutList.Count - 1)].Init(audioFileReader);
			waveOutList[(waveOutList.Count - 1)].Play();
		}

		private static void stop() {
			foreach (var i in audioFileReaderList) {
				if (i != null) i.Dispose();
			}
			audioFileReaderList.Clear();

			foreach (var i in waveOutList) {
				if (i != null) {
					i.Stop();
					i.Dispose();
				}
			}
			waveOutList.Clear();
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook,
			LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
			IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
	}
}
